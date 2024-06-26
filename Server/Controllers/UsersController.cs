using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Common.DataStructures.Dtos;
using Common.DataStructures.Http.Requests;
using Common.DataStructures.Http.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Contexts;
using Server.Models;
using Server.Utility;
using LoginRequest = Microsoft.AspNetCore.Identity.Data.LoginRequest;

namespace Server.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UsersController(
    ApplicationDbContext context,
    IConfiguration configuration,
    ILogger<UsersController> logger,
    UserManager<User> userManager) : ApplicationControllerBase(context, logger, userManager)
{
    private const int RefreshTokenLifetime = 7; // Days
    private const int GuestRefreshTokenLifetime = 1; // Days
    private const int AccessTokenLifetime = 60; // Minutes
    private const int MaxFailedAttempts = 5;
    private const int TimeoutDuration = 5; // Minutes
    
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<User> _userManager = userManager;

    private IPasswordHasher<User> PasswordHasher => _userManager.PasswordHasher;

    [Authorize(Roles = "SuperAdmin,Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _context.Users.ToListAsync();
        var userDtos = users.Select(u => u.ToDto());
        return Ok(userDtos);
    }
    
    // DELETE: api/User/34e5705e-f901-45a5-9c88-e645984d2931
    [HttpDelete("{guid:guid}")]
    public async Task<IActionResult> DeleteUser(Guid guid)
    {
        var managerUserResult = await GetUserFromToken();
        if (managerUserResult.Result is not null)
        {
            return managerUserResult.Result;
        }
        
        var managerUser = managerUserResult.Value!;
        
        var user = await GetUserByGuid(guid);
        if (user is null)
        {
            logger.LogInformation("User {guid} not found", guid);
            return NotFound();
        }

        if (user.Guid != managerUser.Guid)
        {
            logger.LogInformation("User {user} cannot be managed by requesting user {requestingUser}", user.Guid, managerUser.Guid);
            return Forbid();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        logger.LogInformation("User {guid} deleted", guid);

        return Ok();
    }

    // POST: api/User/register
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = await GetUserByEmail(request.Email);
        if (user is not null)
        {
            logger.LogInformation("Email is already in use: {email}", request.Email);
            return BadRequest(new { Message = "Email is already in use." });
        }

        user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email,
            Guid = Guid.NewGuid()
        };
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            logger.LogInformation("Failed to create user {email}", request.Email);
            var errors = result.Errors.Select(e => e.Description);
            logger.LogInformation("Errors: {errors}", errors);
            return BadRequest(new { errors });
        }

        logger.LogInformation("Created user {email}", user.Email);
        
        return Created();
    }

    // POST: api/User/login
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await GetUserByEmail(request.Email);
        if (user is null)
        {
            logger.LogInformation("User {email} not found", request.Email);
            return NotFound();
        }

        if (user.IsLockedOut)
        {
            logger.LogInformation("User {email} is locked out", request.Email);
            return Unauthorized();
        }
        
        if (!PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                           .Equals(PasswordVerificationResult.Success))
        {
            logger.LogInformation("Invalid password for user {email}", request.Email);
            if (!user.LockoutEnabled)
            {
                return Unauthorized();
            }

            user.AccessFailedCount++;
            if (user.AccessFailedCount >= MaxFailedAttempts)
            {
                logger.LogInformation("User {email} has been locked out", request.Email);
                user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(TimeoutDuration);
            }
            await _userManager.UpdateAsync(user);
            return Unauthorized();
        }

        var accessToken = await GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();
        await AddRefreshToken(user, refreshToken);

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            logger.LogInformation("User {email} has role {role}", user.Email, role);
        }

        if (user.LockoutEnabled)
        {
            user.AccessFailedCount = 0;
            user.LockoutEnd = null;
            await _userManager.UpdateAsync(user);
        }

        logger.LogInformation("User {email} logged in", request.Email);
        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        });
    }
    
    // POST: api/User/guest-login
    [AllowAnonymous]
    [HttpPost("guest-login")]
    public async Task<ActionResult<GuestAuthResponse>> GuestLogin()
    {
        var guid = Guid.NewGuid();
        var guestId = $"Guest-{guid}";
        var guest = new User
        {
            FirstName = guestId,
            LastName = guestId,
            Email = guestId,
            UserName = guestId,
            Guid = guid
        };
        
        var result = await _userManager.CreateAsync(guest, guid.ToString());
        if (!result.Succeeded)
        {
            logger.LogInformation("Failed to create user {email}", guestId);
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { errors });
        }
        
        // Add guest role
        await _userManager.AddToRoleAsync(guest, "Guest");
        
        logger.LogInformation("Created guest {email}", guest.Email);
        
        var accessToken = await GenerateAccessToken(guest);
        var refreshToken = GenerateRefreshToken();
        await AddRefreshToken(guest, refreshToken, lifetime: GuestRefreshTokenLifetime);

        logger.LogInformation("Guest {email} logged in", guest.Email);
        return Ok(new GuestAuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            GuestId = guest.Email
        });
    }

    // POST: api/User/logout?all=<bool?>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromQuery] bool all = false, [FromBody] TokenRequest? tokenRequest = null)
    {
        var userResult = await GetUserFromToken();
        if (userResult.Result is not null)
        {
            return userResult.Result;
        }

        var user = userResult.Value!;
        if (all)
        {
            return await LogoutAllSessions(user);
        }

        if (tokenRequest is not null)
        {
            return await LogoutSession(user, tokenRequest.AccessToken);
        }

        return await LogoutAllSessions(user); // Default to logging out of all sessions
    }
    
    private async Task<IActionResult> LogoutSession(User user, string token)
    {
        try{
            var refreshToken = await GetRefreshTokenByToken(token);
            if (refreshToken is null)
            {
                logger.LogInformation("Refresh token not found");
                return NoContent();
            }

            var valid = ValidateRefreshToken(refreshToken);
            if (!valid)
            {
                logger.LogInformation("Refresh token is invalid");
                // Token is invalid, so user is essentially logged out from this session
                return NoContent();
            }

            if (refreshToken.UserId != user.Id)
            {
                logger.LogWarning("User {email} does not own refresh token", user.Email);
                // User does not own the token, so log the incident and invalidate the token
            }

            refreshToken.Valid = false;
            await _context.SaveChangesAsync();
            logger.LogInformation("User {email} logged out of session", user.Email);
            
            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while logging out the session for user {Email}", user.Email);
            return Problem();
        }
    }
    
    private async Task<IActionResult> LogoutAllSessions(User user)
    {
        var refreshTokens = _context.RefreshTokens.Where(t => t.UserId == user.Id);
        foreach (var token in refreshTokens)
        {
            token.Valid = false;
        }

        await _context.SaveChangesAsync();
        logger.LogInformation("User {email} logged out of all devices", user.Email);
        
        return Ok();
    }
    
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userResult = await GetUserFromToken();
        if (userResult.Result is not null)
        {
            return userResult.Result;
        }

        var user = userResult.Value!;
        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            logger.LogInformation("Failed to change password for user {email}", user.Email);
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { errors });
        }

        logger.LogInformation("Changed password for user {email}", user.Email);
        return Ok();
    }
    
    [HttpGet("role")]
    public async Task<ActionResult<string>> GetHighestRole()
    {
        var userResult = await GetUserFromToken();
        if (userResult.Result is not null)
        {
            return userResult.Result;
        }
        
        var user = userResult.Value!;
        var roles = await _userManager.GetRolesAsync(user);
        var highestRole = GetHighestRole(roles);
        return Ok(highestRole);
    }
    
    // POST: api/User/refresh
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] TokenRequest request)
    {
        var refreshToken = await GetRefreshTokenByToken(request.AccessToken);
        if (refreshToken is null)
        {
            logger.LogInformation("Refresh token not found: {token}", request.AccessToken);
            return Unauthorized();
        }
            
        var valid = ValidateRefreshToken(refreshToken);
        if (!valid)
        {
            logger.LogInformation("Refresh token {token} is invalid", request.AccessToken);
            return Unauthorized();
        }

        var user = await GetUserFromRefreshToken(refreshToken);
        if (user is null)
        {
            logger.LogInformation("User not found for refresh token {token}", request.AccessToken);
            return Unauthorized();
        }
            
        var newAccessToken = await GenerateAccessToken(user);
        var newRefreshToken = GenerateRefreshToken();

        try
        {
            await UpdateRefreshToken(user, refreshToken, newRefreshToken); // Replace the old refresh token with a new one
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update refresh token for user {userId}", user.Id);
            return Problem();
        }
        
        logger.LogInformation("Refreshed token for user {email}", user.Email);

        return Ok(new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        });
    }
    
    /// <summary>
    ///     Invalidates the old refresh token and adds a new one to the database. Note: This method updates the database.
    /// </summary>
    /// <param name="user">User to update the refresh token for</param>
    /// <param name="oldRefreshToken">Old refresh token to invalidate</param>
    /// <param name="newRefreshToken">New refresh token to add</param>
    private async Task UpdateRefreshToken(User user, RefreshToken oldRefreshToken, string newRefreshToken)
    {
        logger.LogInformation("Invalidating old refresh token for user {userId}", user.Id);
        oldRefreshToken.Valid = false;

        await AddRefreshToken(user, newRefreshToken); // Context.SaveChangesAsync() is called in AddRefreshToken
    }
    
    /// <summary>
    ///     Adds a refresh token to the database for the user. Note: This method updates the database.
    /// </summary>
    /// <param name="user">User to add the refresh token to</param>
    /// <param name="refreshToken">Refresh token to add</param>
    /// <param name="lifetime">Lifetime of the refresh token in days (Default: <see cref="RefreshTokenLifetime"/>)</param>
    private async Task AddRefreshToken(User user, string refreshToken, int lifetime = RefreshTokenLifetime)
    {
        var token = new RefreshToken
        {
            Token = refreshToken,
            ExpiryTime = DateTimeOffset.UtcNow.AddDays(lifetime),
            Valid = true,
            UserId = user.Id,
            User = user
        };
        
        logger.LogInformation("Updating refresh token for user {userId}", user.Id);
        await _context.RefreshTokens.AddAsync(token);
        await _context.SaveChangesAsync();
    }
        
    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32]; // For example, 256 bits
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
    
    private async Task<string> GenerateAccessToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, user.Email),
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(AccessTokenLifetime),
            SigningCredentials = creds
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);
        return tokenString;
    }
    
    private static bool ValidateRefreshToken(RefreshToken token)
    {
        return token.Valid && token.ExpiryTime > DateTimeOffset.UtcNow;
    }
        
    private ValueTask<User?> GetUserFromRefreshToken(RefreshToken token)
    {
        return _context.Users.FindAsync(token.UserId);
    }
    
    private Task<RefreshToken?> GetRefreshTokenByToken(string token)
    {
        return _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);
    }
}
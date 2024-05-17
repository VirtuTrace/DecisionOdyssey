using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
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
using LoginRequest = Microsoft.AspNetCore.Identity.Data.LoginRequest;

namespace Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController(
    ApplicationDbContext context,
    IConfiguration configuration,
    ILogger<UsersController> logger,
    UserManager<User> userManager,
    IMapper mapper) : ApplicationControllerBase(context, logger, userManager)
{
    private const int RefreshTokenLifetime = 7; // Days
    private const int GuestRefreshTokenLifetime = 1; // Days
    private const int AccessTokenLifetime = 60; // Minutes
    
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<User> _userManager = userManager;

    private IPasswordHasher<User> PasswordHasher => _userManager.PasswordHasher;

    [Authorize(Roles = "SuperAdmin,Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(mapper.Map<IEnumerable<UserDto>>(users));
    }
    
    // DELETE: api/User/34e5705e-f901-45a5-9c88-e645984d2931
    [Authorize(Roles = "SuperAdmin,Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await GetUserByGuid(id);
        if (user is null)
        {
            logger.LogInformation("User {id} not found", id);
            return NotFound();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        logger.LogInformation("User {id} deleted", id);

        return Ok();
    }

    // POST: api/User/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(UserRegisterRequest request)
    {
        var user = await GetUserByEmail(request.Email);
        if (user is not null)
        {
            logger.LogInformation("Email is already in use: {email}", request.Email);
            return BadRequest("Email is already in use.");
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
            return BadRequest(new { errors });
        }

        logger.LogInformation("Created user {email}", user.Email);
        
        return Created();
    }

    // POST: api/User/login
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await GetUserByEmail(request.Email);
        if (user is null)
        {
            logger.LogInformation("User {email} not found", request.Email);
            return NotFound();
        }

        if (!PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                           .Equals(PasswordVerificationResult.Success))
        {
            logger.LogInformation("Invalid password for user {email}", request.Email);
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

        logger.LogInformation("User {email} logged in", request.Email);
        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        });
    }
    
    // POST: api/User/guest-login
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
            return await LogoutSession(user, tokenRequest.Token);
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
    
    // POST: api/User/refresh
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] TokenRequest request)
    {
        var refreshToken = await GetRefreshTokenByToken(request.Token);
        if (refreshToken is null)
        {
            logger.LogInformation("Refresh token not found: {token}", request.Token);
            return Unauthorized();
        }
            
        var valid = ValidateRefreshToken(refreshToken);
        if (!valid)
        {
            logger.LogInformation("Refresh token {token} is invalid", request.Token);
            return Unauthorized();
        }

        var user = await GetUserFromRefreshToken(refreshToken);
        if (user is null)
        {
            logger.LogInformation("User not found for refresh token {token}", request.Token);
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
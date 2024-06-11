using Common.DataStructures.Dtos;
using Common.DataStructures.Dtos.DecisionElements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Contexts;
using Server.Models;
using Server.Utility;

namespace Server.Controllers;

[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin, Admin")]
[ApiController]
public class AdminController(ApplicationDbContext context, ILogger logger, UserManager<User> userManager) : ApplicationControllerBase(context, logger, userManager)
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<User> _userManager = userManager;
    private readonly ILogger _logger = logger;

    #region User Control

    [HttpGet("user")]
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        var users = await _context.Users.Select(u => u.ToDto()).ToListAsync();
        return Ok(users);
    }
    
    [HttpDelete("user/{guid:guid}")]
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

        if (!await UserCanBeManaged(managerUser, user))
        {
            logger.LogInformation("User {user} cannot be managed by requesting user {requestingUser}", user.Guid, managerUser.Guid);
            return Forbid();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        logger.LogInformation("User {guid} deleted", guid);

        return Ok();
    }
    
    [HttpPost("user/{guid:guid}/role")]
    public async Task<IActionResult> AddRoleToUser(Guid guid, [FromBody] string role)
    {
        if (!ApplicationRole.IsValidRole(role))
        {
            _logger.LogInformation("Role does not exist: {role}", role);
            return BadRequest(new { Message = "Role does not exist" });
        }
        
        var requestingUserResult = await GetUserFromToken();
        if (requestingUserResult.Result is not null)
        {
            return requestingUserResult.Result;
        }
        
        var requestingUser = requestingUserResult.Value!;
        
        var user = await GetUserByGuid(guid);
        if (user is null)
        {
            _logger.LogInformation("User not found: {guid}", guid);
            return NotFound();
        }

        if (!await UserCanBeManaged(requestingUser, user))
        {
            _logger.LogInformation("User {user} cannot be managed by requesting user {requestingUser}", user.Guid, requestingUser.Guid);
            return Forbid();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var result = await _userManager.RemoveFromRolesAsync(user, roles);
        if (!result.Succeeded)
        {
            _logger.LogInformation("Failed to remove roles from user {user}: {errors}", user.Guid, result.Errors);
            return BadRequest(new { Message = "Failed to remove old roles from user"});
        }
        
        result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            _logger.LogInformation("Failed to add role to user {user}: {errors}", user.Guid, result.Errors);
            return BadRequest(new { Message = "Failed to add role to user" });
        }

        return Ok();
    }
    
    [HttpPost("user/{guid:guid}/lock")]
    public async Task<IActionResult> LockUser(Guid guid)
    {
        var requestingUserResult = await GetUserFromToken();
        if (requestingUserResult.Result is not null)
        {
            return requestingUserResult.Result;
        }
        
        var requestingUser = requestingUserResult.Value!;
        
        var user = await GetUserByGuid(guid);
        if (user is null)
        {
            _logger.LogInformation("User not found: {guid}", guid);
            return NotFound();
        }

        if (!await UserCanBeManaged(requestingUser, user))
        {
            _logger.LogInformation("User {user} cannot be managed by requesting user {requestingUser}", user.Guid, requestingUser.Guid);
            return Forbid();
        }

        var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        if (!result.Succeeded)
        {
            _logger.LogInformation("Failed to lock user {user}: {errors}", user.Guid, result.Errors);
            return BadRequest(new { Message = "Failed to lock user" });
        }

        return Ok();
    }
    
    [HttpPost("user/{guid:guid}/unlock")]
    public async Task<IActionResult> UnlockUser(Guid guid)
    {
        var requestingUserResult = await GetUserFromToken();
        if (requestingUserResult.Result is not null)
        {
            return requestingUserResult.Result;
        }
        
        var requestingUser = requestingUserResult.Value!;
        
        var user = await GetUserByGuid(guid);
        if (user is null)
        {
            _logger.LogInformation("User not found: {guid}", guid);
            return NotFound();
        }

        if (!await UserCanBeManaged(requestingUser, user))
        {
            _logger.LogInformation("User {user} cannot be managed by requesting user {requestingUser}", user.Guid, requestingUser.Guid);
            return Forbid();
        }

        var result = await _userManager.SetLockoutEndDateAsync(user, null);
        if (!result.Succeeded)
        {
            _logger.LogInformation("Failed to unlock user {user}: {errors}", user.Guid, result.Errors);
            return BadRequest(new { Message = "Failed to unlock user" });
        }

        return Ok();
    }
    
    #endregion
    
    #region Decision Element Control
    
    [HttpGet("matrix")]
    public async Task<ActionResult<List<DecisionMatrixDto>>> GetDecisionMatrices()
    {
        var matrices = await _context.DecisionMatrices.Select(m => m.ToDto()).ToListAsync();
        _logger.LogInformation("Returning {count} matrices", matrices.Count);
        return Ok(matrices);
    }
    
    #endregion
}
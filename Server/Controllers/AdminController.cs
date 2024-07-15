using Common.DataStructures.Dtos;
using Common.DataStructures.Dtos.DecisionElements;
using Common.DataStructures.Http.Responses;
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
public class AdminController(
    ApplicationDbContext context,
    ILogger<AdminController> logger,
    UserManager<User> userManager) : ApplicationControllerBase(context, logger, userManager)
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<User> _userManager = userManager;

    #region User Control

    [HttpGet("users")]
    public async Task<ActionResult<List<AdvanceUserDto>>> GetUsers(string? email = null, string? firstname = null,
                                                            string? lastname = null, string? name = null)
    {
        var userQuery = _context.Users.AsQueryable();
        if (email is not null)
        {
            userQuery = userQuery.Where(u => u.Email.Contains(email));
        }

        if (firstname is not null)
        {
            userQuery = userQuery.Where(u => u.FirstName.Contains(firstname));
        }

        if (lastname is not null)
        {
            userQuery = userQuery.Where(u => u.LastName.Contains(lastname));
        }

        if (name is not null)
        {
            userQuery = userQuery.Where(u => u.FirstName.Contains(name) || u.LastName.Contains(name));
        }

        var queriedUsers = await userQuery.ToListAsync();
        var users = new List<AdvanceUserDto>();
        foreach (var user in queriedUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = GetHighestRole(roles);
            users.Add(new AdvanceUserDto(user.ToDto(), role));
        }
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
            logger.LogInformation("User {user} cannot be managed by requesting user {requestingUser}", user.Guid,
                managerUser.Guid);
            return Forbid();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        logger.LogInformation("User {guid} deleted", guid);

        return Ok();
    }

    [HttpGet("user/{guid:guid}/status")]
    public async Task<ActionResult<UserStatusResponse>> GetUserStatus(Guid guid)
    {
        var user = await GetUserByGuid(guid);
        if (user is null)
        {
            logger.LogInformation("User {guid} not found", guid);
            return NotFound();
        }

        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "User";
        var locked = await _userManager.IsLockedOutAsync(user);
        return new UserStatusResponse
        {
            Role = role,
            Locked = locked
        };
    }

    [HttpPost("user/{guid:guid}/role")]
    public async Task<IActionResult> AddRoleToUser(Guid guid, [FromBody] string role)
    {
        if (!ApplicationRole.IsValidRole(role))
        {
            logger.LogInformation("Role does not exist: {role}", role);
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
            logger.LogInformation("User not found: {guid}", guid);
            return NotFound();
        }

        if (!await UserCanBeManaged(requestingUser, user))
        {
            logger.LogInformation("User {user} cannot be managed by requesting user {requestingUser}", user.Guid,
                requestingUser.Guid);
            return Forbid();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var result = await _userManager.RemoveFromRolesAsync(user, roles);
        if (!result.Succeeded)
        {
            logger.LogInformation("Failed to remove roles from user {user}: {errors}", user.Guid, result.Errors);
            return BadRequest(new { Message = "Failed to remove old roles from user" });
        }

        result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            logger.LogInformation("Failed to add role to user {user}: {errors}", user.Guid, result.Errors);
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
            logger.LogInformation("User not found: {guid}", guid);
            return NotFound();
        }

        if (!await UserCanBeManaged(requestingUser, user))
        {
            logger.LogInformation("User {user} cannot be managed by requesting user {requestingUser}", user.Guid,
                requestingUser.Guid);
            return Forbid();
        }

        var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        if (!result.Succeeded)
        {
            logger.LogInformation("Failed to lock user {user}: {errors}", user.Guid, result.Errors);
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
            logger.LogInformation("User not found: {guid}", guid);
            return NotFound();
        }

        if (!await UserCanBeManaged(requestingUser, user))
        {
            logger.LogInformation("User {user} cannot be managed by requesting user {requestingUser}", user.Guid,
                requestingUser.Guid);
            return Forbid();
        }

        var result = await _userManager.SetLockoutEndDateAsync(user, null);
        if (!result.Succeeded)
        {
            logger.LogInformation("Failed to unlock user {user}: {errors}", user.Guid, result.Errors);
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
        logger.LogInformation("Returning {count} matrices", matrices.Count);
        return Ok(matrices);
    }

    #endregion
}
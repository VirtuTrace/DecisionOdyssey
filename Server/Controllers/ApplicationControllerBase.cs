using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Contexts;
using Server.Models;

namespace Server.Controllers;

public abstract class ApplicationControllerBase(
    ApplicationDbContext context,
    ILogger logger,
    UserManager<User> userManager) : ControllerBase
{
    private const string GuestRole = "Guest";

    protected static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected Task<User?> GetUserByGuid(Guid guid)
    {
        return context.Users.FirstOrDefaultAsync(u => u.Guid == guid);
    }

    protected Task<User?> GetUserByEmail(string email)
    {
        return context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    protected async Task<ActionResult<User>> GetUserFromToken()
    {
        if (User.Identity is not ClaimsIdentity identity)
        {
            logger.LogInformation("No identity found");
            return Unauthorized();
        }

        var email = identity.FindFirst(ClaimTypes.Email)?.Value;
        if (email is null)
        {
            logger.LogInformation("No email found in claims");
            return Unauthorized();
        }

        var user = await GetUserByEmail(email);
        if (user is not null)
        {
            return user;
        }

        logger.LogInformation("User not found: {email}", email);
        return Unauthorized();
    }

    /// <summary>
    ///     Returns a 405 Method Not Allowed response with a message. Message is converted to a JSON object.
    /// </summary>
    /// <param name="message">Error message</param>
    /// <returns>405 Method Not Allowed response</returns>
    protected ObjectResult MethodNotAllowed(string message)
    {
        return StatusCode(405, new { Message = message });
    }

    /// <summary>
    ///     Checks if the user has a lower role than the user. Manager must have a higher role than the user.
    /// </summary>
    /// <param name="manager">User to check if they can manage the user</param>
    /// <param name="user">User being managed</param>
    /// <returns>Boolean based on whether the manager can manage the user</returns>
    protected async Task<bool> UserCanBeManaged(User manager, User user)
    {
        var managerRoles = await userManager.GetRolesAsync(manager);
        var userRoles = await userManager.GetRolesAsync(user);
        var managerHighestRole = GetHighestRole(managerRoles);
        var userHighestRole = GetHighestRole(userRoles);

        return RolePriority(userHighestRole) > RolePriority(managerHighestRole); // Lower number means higher priority
    }

    protected static string GetHighestRole(IEnumerable<string> roles)
    {
        var highestRole = GuestRole;
        var highestPriority = RolePriority(highestRole);
        foreach (var role in roles)
        {
            var priority = RolePriority(role);
            if (priority < highestPriority) // Lower number means higher priority
            {
                highestRole = role;
                highestPriority = priority;
            }
        }

        return highestRole;
    }

    private static int RolePriority(string role)
    {
        return ApplicationRole.RolePriority.GetValueOrDefault(role, int.MaxValue);
    }
}
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Contexts;
using Server.Models;

namespace Server.Utility;

public static class Initialization
{
    public static async Task Initialize(IServiceProvider services)
    {
        await InitializeDatabaseAsync(services);
        await SeedRolesAsync(services);
        await CleanTokensAsync(services);
    }

    private static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
        // Ensure the database is created and migrated
        await context.Database.MigrateAsync();
    }

    private static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var roleNames = ApplicationRole.RolePriority.Keys;
        foreach (var roleName in roleNames)
        {
            var roleExists = await roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName));
            }
        }
    }
    
    private static async Task CleanTokensAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var expiredTokens = context.RefreshTokens
                                         .Where(rt => rt.ExpiryTime < DateTime.UtcNow || !rt.Valid);
        
        context.RefreshTokens.RemoveRange(expiredTokens);
        await context.SaveChangesAsync();
    }
}
using Microsoft.AspNetCore.Identity;

namespace Server.Models;

public class ApplicationRole : IdentityRole<long>
{
    public static Dictionary<string, int> RolePriority { get; } = new()
    {
        ["SuperAdmin"] = 0,
        ["Admin"] = 1,
        ["Researcher"] = 2, // Decision Project Manager
        ["User"] = 3,
        ["Guest"] = 4
    };
    
    
    public ApplicationRole()
    {
        
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }
    
    
    public static bool IsValidRole(string role)
    {
        return RolePriority.ContainsKey(role);
    }
}
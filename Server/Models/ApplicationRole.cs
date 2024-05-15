using Microsoft.AspNetCore.Identity;

namespace Server.Models;

public class ApplicationRole : IdentityRole<long>
{
    public static Dictionary<string, int> RolePriority { get; } = new()
    {
        ["SuperAdmin"] = 0,
        ["Admin"] = 1,
        ["Researcher"] = 2,
        ["User"] = 3,
        ["Guest"] = 4
    };
    
    
    public ApplicationRole()
    {
        
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }
}
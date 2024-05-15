using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Server.Models;

public class User : IdentityUser<long>
{
    public Guid Guid { get; set; }
    
    [StringLength(255)]
    public override required string Email { get; set; }

    [StringLength(255)]
    public string? SecondaryEmail { get; set; }
    
    [StringLength(127)]
    public override string PasswordHash { get; set; } = null!;
    
    [StringLength(255)]
    public required string FirstName { get; set; }
    
    [StringLength(255)]
    public required string LastName { get; set; }
    
    public ICollection<RefreshToken> RefreshTokens { get; set; } = null!;
}
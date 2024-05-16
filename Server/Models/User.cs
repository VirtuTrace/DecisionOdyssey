using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Server.Models.DecisionElements;
using Server.Models.DecisionElements.Stats;

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
    
    public ICollection<DecisionMatrix> CreatedDecisionMatrices { get; set; } = null!;
    
    public ICollection<DecisionMatrixStats> CompletedDecisionMatrixStats { get; set; } = null!;
    
    public ICollection<DecisionMatrix> AccessibleDecisionMatrices { get; set; } = null!;
}
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Server.Models;
using Server.Models.DecisionElements;
using Server.Models.DecisionElements.Stats;

namespace Server.Contexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, ApplicationRole, long>(options)
{
    public required DbSet<RefreshToken> RefreshTokens { get; set; }
    public required DbSet<DecisionMatrix> DecisionMatrices { get; set; }
    public required DbSet<DecisionMatrixStats> DecisionMatrixStats { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<User>()
               .ToTable("Users");
        
        // One-to-many relationship between User and RefreshToken
        builder.Entity<RefreshToken>()
               .ToTable("RefreshTokens")
               .HasOne(r => r.User)
               .WithMany(u => u.RefreshTokens);
        
        builder.Entity<DecisionMatrix>()
               .ToTable("DecisionMatrices")
               .HasOne(d => d.User)
               .WithMany(u => u.CreatedDecisionMatrices)
               .HasForeignKey(d => d.UserId)
               .IsRequired();
        
        builder.Entity<DecisionMatrix>()
               .HasMany(d => d.Participants)
               .WithMany(u => u.AccessibleDecisionMatrices)
               .UsingEntity(j => j.ToTable("DecisionMatrixAccess"));
        
        builder.Entity<DecisionMatrixStats>()
               .HasOne(dms => dms.Matrix)
               .WithMany(dm => dm.DecisionMatrixStats)
               .HasForeignKey(dms => dms.MatrixId)
               .IsRequired();
        
        builder.Entity<DecisionMatrixStats>()
               .HasOne(dms => dms.Participant)
               .WithMany(u => u.CompletedDecisionMatrixStats)
               .HasForeignKey(dms => dms.ParticipantId)
               .IsRequired();
    }
}
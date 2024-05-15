using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Contexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, ApplicationRole, long>(options)
{
    public required DbSet<RefreshToken> RefreshTokens { get; set; }

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
    }
}
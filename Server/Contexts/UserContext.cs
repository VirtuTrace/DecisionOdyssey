using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Contexts;

public class UserContext(DbContextOptions<UserContext> options) : IdentityDbContext<User, IdentityRole<long>, long>(options);

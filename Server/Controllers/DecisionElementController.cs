using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Server.Contexts;
using Server.Models;

namespace Server.Controllers;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public class DecisionElementController(ApplicationDbContext context, ILogger logger, UserManager<User> userManager) : ApplicationControllerBase(context, logger, userManager)
{
    
}
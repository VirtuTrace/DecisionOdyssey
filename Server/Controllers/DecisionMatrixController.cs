using AutoMapper;
using Common.Models.Dtos.DecisionElements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Contexts;
using Server.Models;

namespace Server.Controllers;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public class DecisionMatrixController(ApplicationDbContext context, ILogger logger, UserManager<User> userManager,
                                      IMapper mapper) 
    : ApplicationControllerBase(context, logger, userManager)
{
    private readonly ApplicationDbContext _context = context;
    
    private string DecisionElementDirectoryName => "Matrices";
    
    // GET: api/DecisionMatrix
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DecisionMatrixDto>>> GetDecisionMatrices()
    {
        var userResult = await GetUserFromToken();
        if (userResult.Result != null)
        {
            // If there's a result, it's an error response.
            return userResult.Result;
        }

        var user = userResult.Value!;
        
        return await GetDecisionMatrices(user);
    }
    
    private async Task<List<DecisionMatrixDto>> GetDecisionMatrices(User user)
    {
        var decisionElements = await _context.DecisionElements
            .Where(de => de.UserId == user.Id)
            .ToListAsync();
        
        var decisionMatrixDtos = mapper.Map<List<DecisionMatrixDto>>(decisionElements);
        
        return decisionMatrixDtos;
    }
}
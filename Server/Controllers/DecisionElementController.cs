using System.Runtime.CompilerServices;
using AutoMapper;
using Common.DataStructures;
using Common.DataStructures.Dtos.DecisionElements;
using Common.DataStructures.Dtos.DecisionElements.Stats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Server.Contexts;
using Server.Models;
using Server.Models.DecisionElements.Stats;
using Server.Utility;

namespace Server.Controllers;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public abstract class DecisionElementController<TDto>(
    ApplicationDbContext context,
    ILogger logger,
    UserManager<User> userManager,
    IMapper mapper)
    : ApplicationControllerBase(context, logger, userManager)
    where TDto : DecisionElementDto
{
    private readonly ILogger _logger = logger;

    protected abstract string DecisionElementDirectoryName { get; }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TDto>>> GetDecisionElements()
    {
        var userResult = await GetUserFromToken();
        if (userResult.Result != null)
        {
            // If there's a result, it's an error response.
            return userResult.Result;
        }

        var user = userResult.Value!;
        
        return await GetDecisionElements(user);
    }
    
    protected abstract Task<List<TDto>> GetDecisionElements(User user);
    
    // GET: api/<DecisionElement>/created
    [HttpGet("created")]
    public async Task<ActionResult<IEnumerable<TDto>>> GetCreatedDecisionElements()
    {
        var userResult = await GetUserFromToken();
        if (userResult.Result != null)
        {
            // If there's a result, it's an error response.
            return userResult.Result;
        }

        var user = userResult.Value!;
        
        return await GetCreatedDecisionElements(user);
    }

    protected abstract Task<List<TDto>> GetCreatedDecisionElements(User user);
    
    // GET: api/<DecisionElement>/accessible
    [HttpGet("accessible")]
    public async Task<ActionResult<IEnumerable<TDto>>> GetAccessibleDecisionElements()
    {
        var userResult = await GetUserFromToken();
        if (userResult.Result != null)
        {
            // If there's a result, it's an error response.
            return userResult.Result;
        }

        var user = userResult.Value!;
        
        return await GetAccessibleDecisionElements(user);
    }

    protected abstract Task<List<TDto>> GetAccessibleDecisionElements(User user);
    
    // GET: api/<DecisionElement>/{guid}
    [HttpGet("{guid:guid}")]
    public async Task<ActionResult<TDto>> GetDecisionElement(Guid guid)
    {
        var userResult = await GetUserFromToken();
        if (userResult.Result != null)
        {
            // If there's a result, it's an error response.
            return userResult.Result;
        }
        
        var user = userResult.Value!;

        return await GetDecisionElement(guid, user);
    }
    
    protected abstract Task<ActionResult<TDto>> GetDecisionElement(Guid guid, User user);
    
    // GET: api/<DecisionElement>/{guid}/data
    [HttpGet("{guid:guid}/data")]
    public async Task<IActionResult> GetDecisionElementData(Guid guid)
    {
        var userResult = await GetUserFromToken();
        if (userResult.Result != null)
        {
            // If there's a result, it's an error response.
            return userResult.Result;
        }
        
        var user = userResult.Value!;

        return await GetDecisionElementData(guid, user);
    }
    
    protected abstract Task<IActionResult> GetDecisionElementData(Guid guid, User user);
    
    // POST: api/<DecisionElement>
    [HttpPost]
    [RequestSizeLimit(ControllerConfig.MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = ControllerConfig.MaxFileSize)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<TDto>> PostDecisionElement([FromForm] string metadata, IFormFile file)
    {
        if(file.Length == 0)
        {
            _logger.LogInformation("File is empty");
            return BadRequest(new { Message = "File is empty" });
        }
        
        var userResult = await GetUserFromToken();
        if (userResult.Result != null)
        {
            // If there's a result, it's an error response.
            return userResult.Result;
        }
        
        var user = userResult.Value!;
        
        return await PostDecisionElement(metadata, file, user);
    }
    
    protected abstract Task<ActionResult<TDto>> PostDecisionElement(string metadata, IFormFile file, User user);
    
    // PUT: api/<DecisionElement>/{guid}
    [HttpPut("{guid:guid}")]
    [RequestSizeLimit(ControllerConfig.MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = ControllerConfig.MaxFileSize)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<TDto>> PutDecisionElement(Guid guid, [FromForm] string metadata, IFormFile file)
    {
        if(file.Length == 0)
        {
            _logger.LogInformation("File is empty");
            return BadRequest(new { Message = "File is empty" });
        }
        
        var userResult = await GetUserFromToken();
        if (userResult.Result != null)
        {
            // If there's a result, it's an error response.
            return userResult.Result;
        }
        
        var user = userResult.Value!;
        
        return await PutDecisionElement(guid, metadata, file, user);
    }
    
    protected abstract Task<ActionResult<TDto>> PutDecisionElement(Guid guid, string metadata, IFormFile file, User user);
    
    // DELETE: api/<DecisionElement>/{guid}
    [HttpDelete("{guid:guid}")]
    public async Task<IActionResult> DeleteDecisionElement(Guid guid)
    {
        var userResult = await GetUserFromToken();
        if (userResult.Result != null)
        {
            // If there's a result, it's an error response.
            return userResult.Result;
        }
        
        var user = userResult.Value!;
        
        return await DeleteDecisionElement(guid, user);
    }
    
    protected abstract Task<IActionResult> DeleteDecisionElement(Guid guid, User user);
    
    [Authorize(Roles = "SuperAdmin,Admin,Researcher")]
    [HttpGet("{guid:guid}/stats")]
    public async Task<ActionResult<List<DecisionElementStatsDto>>> GetDecisionElementStats(Guid guid)
    {
        var userResult = await GetUserFromToken();
        if (userResult.Result != null)
        {
            // If there's a result, it's an error response.
            return userResult.Result;
        }
        
        var user = userResult.Value!;
        
        return await GetDecisionElementStats(guid, user);
    }
    
    protected abstract Task<ActionResult<List<DecisionElementStatsDto>>> GetDecisionElementStats(Guid guid, User user);
    
    [Authorize(Roles = "SuperAdmin,Admin,Researcher")]
    [HttpGet("{guid:guid}/stats/data")]
    public async Task<ActionResult<List<DecisionMatrixStatsData>>> GetDecisionElementStatsData(Guid guid)
    {
        var userResult = await GetUserFromToken();
        if (userResult.Result != null)
        {
            // If there's a result, it's an error response.
            return userResult.Result;
        }
        
        var user = userResult.Value!;
        
        return await GetDecisionElementStatsData(guid, user);
    }
    
    protected abstract Task<ActionResult<List<DecisionMatrixStatsData>>> GetDecisionElementStatsData(Guid guid, User user);
    
    [HttpPost("stats")]
    [RequestSizeLimit(ControllerConfig.MaxFileSize)]
    public async Task<ActionResult> PostDecisionElementStats(string serializedStats)
    {
        var userResult = await GetUserFromToken();
        if (userResult.Result != null)
        {
            // If there's a result, it's an error response.
            _logger.LogInformation("User result is null");
            return userResult.Result;
        }

        var user = userResult.Value!;
        return await PostDecisionElementStats(serializedStats, user);
    }
    
    protected abstract Task<ActionResult> PostDecisionElementStats(string serializedStats, User user);
    
    protected static FileStreamResult CreateFileStream(string filePath)
    {
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return new FileStreamResult(stream, "application/zip")
        {
            FileDownloadName = Path.GetFileName(filePath),
            EnableRangeProcessing = true
        };
        //return File(stream, "application/zip", Path.GetFileName(filePath));
    }
    
    protected static PhysicalFileResult CreatePhysicalFile(string filePath)
    {
        return new PhysicalFileResult(filePath, "application/zip")
        {
            FileDownloadName = Path.GetFileName(filePath),
            EnableRangeProcessing = true
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void CreateParentDirectories(string filepath) => Directory.CreateDirectory(Directory.GetParent(filepath)!.FullName);
}
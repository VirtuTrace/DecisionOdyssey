using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Common.DataStructures;
using Common.DataStructures.Dtos.DecisionElements;
using Common.DataStructures.Dtos.DecisionElements.Stats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Contexts;
using Server.Models;
using Server.Models.DecisionElements;
using Server.Models.DecisionElements.Stats;
using Server.Utility;

namespace Server.Controllers;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public class DecisionMatrixController(
    ApplicationDbContext context,
    ILogger<DecisionMatrixController> logger,
    UserManager<User> userManager)
    : DecisionElementController<DecisionMatrixDto>(context, logger, userManager)
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger _logger = logger;

    protected override string DecisionElementDirectoryName => "Matrices";

    protected override async Task<List<DecisionMatrixDto>> GetDecisionElements(User user)
    {
        await _context.Entry(user).Collection(u => u.CreatedDecisionMatrices).LoadAsync();
        await _context.Entry(user).Collection(u => u.AccessibleDecisionMatrices).LoadAsync();
        var decisionMatrices = user.CreatedDecisionMatrices.Concat(user.AccessibleDecisionMatrices);
        //var decisionMatrixDtos = _mapper.Map<List<DecisionMatrixDto>>(decisionMatrices);
        var decisionMatrixDtos = ToDtos(decisionMatrices);

        _logger.LogInformation("Returning {numberDecisionMatrices} decision matrices for user {userId}",
            decisionMatrixDtos.Count, user.Id);
        return decisionMatrixDtos;
    }

    protected override async Task<List<DecisionMatrixDto>> GetCreatedDecisionElements(User user)
    {
        await _context.Entry(user).Collection(u => u.CreatedDecisionMatrices).LoadAsync();
        var decisionMatrices = user.CreatedDecisionMatrices;
        //var decisionMatrixDtos = _mapper.Map<List<DecisionMatrixDto>>(decisionMatrices);
        var decisionMatrixDtos = ToDtos(decisionMatrices);

        _logger.LogInformation("Returning {numberDecisionMatrices} decision matrices created by user {userId}",
            decisionMatrixDtos.Count, user.Id);
        return decisionMatrixDtos;
    }

    protected override async Task<List<DecisionMatrixDto>> GetAccessibleDecisionElements(User user)
    {
        await _context.Entry(user).Collection(u => u.AccessibleDecisionMatrices).LoadAsync();
        var decisionMatrices = user.AccessibleDecisionMatrices;
        //var decisionMatrixDtos = _mapper.Map<List<DecisionMatrixDto>>(decisionMatrices);
        var decisionMatrixDtos = ToDtos(decisionMatrices);

        _logger.LogInformation("Returning {numberDecisionMatrices} decision matrices accessible by user {userId}",
            decisionMatrixDtos.Count, user.Id);
        return decisionMatrixDtos;
    }

    protected override async Task<ActionResult<DecisionMatrixDto>> GetDecisionElement(Guid guid, User user)
    {
        var decisionMatrix = await GetDecisionMatrix(guid);
        if (decisionMatrix is null)
        {
            _logger.LogInformation("Decision matrix with GUID {guid} not found", guid);
            return NotFound();
        }

        if (decisionMatrix.UserId != user.Id && !user.AccessibleDecisionMatrices.Contains(decisionMatrix))
        {
            _logger.LogInformation("User {userId} does not have access to decision matrix with GUID {guid}", user.Id,
                guid);
            return Forbid();
        }

        var decisionMatrixDto = decisionMatrix.ToDto();
        _logger.LogInformation("Returning decision matrix with GUID {guid}", guid);
        return decisionMatrixDto;
    }

    protected override async Task<IActionResult> GetDecisionElementData(Guid guid, User user)
    {
        var decisionMatrix = await GetDecisionMatrix(guid);

        if (decisionMatrix is null)
        {
            _logger.LogInformation("Decision matrix with GUID {guid} not found", guid);
            return NotFound();
        }

        if (decisionMatrix.UserId != user.Id && !user.AccessibleDecisionMatrices.Contains(decisionMatrix))
        {
            _logger.LogInformation("User {userId} does not have access to decision matrix with GUID {guid}", user.Id,
                guid);
            return Forbid();
        }

        var filepath = decisionMatrix.Filepath;
        if (!System.IO.File.Exists(filepath))
        {
            _logger.LogInformation("Decision matrix with GUID {guid} has no data file", guid);
            return NotFound();
        }

        _logger.LogInformation("Returning data for decision matrix with GUID {guid}", guid);
        return CreatePhysicalFile(filepath);
    }

    protected override async Task<ActionResult<DecisionMatrixDto>> PostDecisionElement(
        string metadata, IFormFile file, User user)
    {
        var decisionMatrixDto = JsonSerializer.Deserialize<DecisionMatrixMetadata>(metadata);
        if (decisionMatrixDto is null)
        {
            _logger.LogInformation("Failed to deserialize decision matrix metadata");
            return BadRequest();
        }

        var decisionMatrix = await GetDecisionMatrix(decisionMatrixDto.Guid);

        if (decisionMatrix is not null)
        {
            _logger.LogInformation("Decision matrix with GUID {guid} already exists", decisionMatrixDto.Guid);
            return MethodNotAllowed("Update existing decision matrix instead using PUT");
        }

        var filepath = GetElementFilePath(user, decisionMatrixDto.Name);

        // File exists on disk, but not in database // Could overwrite file on disk
        if (System.IO.File.Exists(filepath))
        {
            _logger.LogWarning("Decision matrix with name {name} already exists", decisionMatrixDto.Name);
            //return MethodNotAllowed("Update existing decision matrix instead using PUT");
        }

        Directory.CreateDirectory(Directory.GetParent(filepath)!.FullName);
        decisionMatrix = decisionMatrixDto.ToModel();
        decisionMatrix.UserId = user.Id;
        decisionMatrix.Filepath = filepath;

        _context.DecisionMatrices.Add(decisionMatrix);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Decision matrix with GUID {guid} created", decisionMatrix.Guid);

        await using (var stream = new FileStream(filepath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
            _logger.LogInformation("Decision matrix with GUID {guid} data saved", decisionMatrix.Guid);
        }

        return Ok(new { Message = "Decision matrix uploaded successfully" });
    }

    protected override async Task<ActionResult<DecisionMatrixDto>> PutDecisionElement(
        Guid guid, string metadata, IFormFile file, User user)
    {
        var decisionMatrixDto = JsonSerializer.Deserialize<DecisionMatrixMetadata>(metadata);
        if (decisionMatrixDto is null)
        {
            _logger.LogInformation("Failed to deserialize decision matrix metadata");
            return BadRequest();
        }

        var decisionMatrix = await GetDecisionMatrix(guid);
        if (decisionMatrix is null)
        {
            _logger.LogInformation("Decision matrix with GUID {guid} not found", guid);
            return NotFound(new { Message = "Create new decision matrix instead using POST" });
        }

        if (decisionMatrix.UserId != user.Id)
        {
            _logger.LogInformation("User {userId} does not have access to decision matrix with GUID {guid}", user.Id,
                guid);
            return Forbid();
        }

        var filepath = decisionMatrix.Filepath;
        if (!System.IO.File.Exists(filepath))
        {
            _logger.LogWarning("Decision matrix with GUID {guid} has no data file", guid);
        }

        Directory.CreateDirectory(Directory.GetParent(filepath)!.FullName); // Should already exist

        decisionMatrix.Name = decisionMatrixDto.Name;
        decisionMatrix.Features = decisionMatrixDto.Features;
        decisionMatrix.NumRows = decisionMatrixDto.RowCount;
        decisionMatrix.NumColumns = decisionMatrixDto.ColumnCount;
        decisionMatrix.LastUpdated = DateTime.Now;

        _context.DecisionMatrices.Update(decisionMatrix);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Decision matrix with GUID {guid} updated", guid);

        await using (var stream = new FileStream(filepath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
            _logger.LogInformation("Decision matrix with GUID {guid} data updated", guid);
        }

        return Ok(new { Message = "Decision matrix updated successfully" });
    }

    protected override async Task<IActionResult> DeleteDecisionElement(Guid guid, User user)
    {
        var decisionMatrix = await GetDecisionMatrix(guid);
        if (decisionMatrix is null)
        {
            _logger.LogInformation("Decision matrix with GUID {guid} not found", guid);
            return NotFound();
        }

        if (decisionMatrix.UserId != user.Id)
        {
            _logger.LogInformation("User {userId} does not have access to decision matrix with GUID {guid}", user.Id,
                guid);
            return Forbid();
        }

        _context.DecisionMatrices.Remove(decisionMatrix);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Decision matrix with GUID {guid} deleted", guid);

        var filepath = decisionMatrix.Filepath;
        if (System.IO.File.Exists(filepath))
        {
            System.IO.File.Delete(filepath);
            _logger.LogInformation("Decision matrix with GUID {guid} data deleted", guid);
        }
        else
        {
            _logger.LogWarning("Decision matrix with GUID {guid} has no data file", guid);
        }

        return Ok(new { Message = "Decision matrix deleted successfully" });
    }

    protected override async Task<ActionResult<List<DecisionElementStatsDto>>> GetDecisionElementStats(
        Guid guid, User user, DateTime? start = null, DateTime? end = null)
    {
        var decisionMatrix = await GetDecisionMatrix(guid);
        if (decisionMatrix is null)
        {
            _logger.LogInformation("Decision matrix with GUID {guid} not found", guid);
            return NotFound();
        }

        if (decisionMatrix.UserId != user.Id)
        {
            _logger.LogInformation("User {userId} does not own decision matrix with GUID {guid}", user.Id, guid);
            return Forbid();
        }

        await _context.Entry(decisionMatrix).Collection(dm => dm.DecisionMatrixStats).LoadAsync();
        var decisionMatrixStatsDtos = decisionMatrix.DecisionMatrixStats
                                                    .Where(dms => DecisionElementWithinTimeRange(dms, start, end))
                                                    .Select(dms => dms.ToDto())
                                                    .Cast<DecisionElementStatsDto>()
                                                    .ToList();
        _logger.LogInformation(
            "Returning {numberDecisionMatrixStats} decision matrix stats for decision matrix with GUID {guid}",
            decisionMatrix.DecisionMatrixStats.Count, guid);
        return decisionMatrixStatsDtos;
    }

    protected override async Task<ActionResult<List<DecisionMatrixStatsData>>> GetDecisionElementStatsData(
        Guid guid, User user, DateTime? start = null, DateTime? end = null)
    {
        var decisionMatrix = await GetDecisionMatrix(guid);
        if (decisionMatrix is null)
        {
            _logger.LogInformation("Decision matrix with GUID {guid} not found", guid);
            return NotFound();
        }
        
        if (decisionMatrix.UserId != user.Id)
        {
            _logger.LogInformation("User {userId} does not own decision matrix with GUID {guid}", user.Id, guid);
            return Forbid();
        }
        
        await _context.Entry(decisionMatrix).Collection(dm => dm.DecisionMatrixStats).LoadAsync();
        var filepaths = decisionMatrix.DecisionMatrixStats
                                      .Where(dms => DecisionElementWithinTimeRange(dms, start, end))
                                      .Select(dms => dms.Filepath);
        var decisionMatrixStatsData = new List<DecisionMatrixStatsData>();
        foreach (var filepath in filepaths)
        {
            var serializedData = await System.IO.File.ReadAllTextAsync(filepath);
            var statsData = JsonSerializer.Deserialize<DecisionMatrixStatsData>(serializedData, JsonOptions);
            if (statsData is null)
            {
                _logger.LogWarning("Failed to deserialize decision matrix stats data: {filepath}", filepath);
                continue;
            }
            
            decisionMatrixStatsData.Add(statsData);
        }
        
        _logger.LogInformation(
            "Returning {numberDecisionMatrixStatsData} decision matrix stats data for decision matrix with GUID {guid}",
            decisionMatrixStatsData.Count, guid);
        return decisionMatrixStatsData;
    }

    protected override async Task<ActionResult> PostDecisionElementStats(JsonNode serializedStats, User user)
    {
        var decisionMatrixStatsData = serializedStats.Deserialize<DecisionMatrixStatsData>(JsonOptions);
        if (decisionMatrixStatsData is null)
        {
            _logger.LogInformation("Failed to deserialize decision matrix stats data");
            return BadRequest();
        }

        var decisionMatrix = await GetDecisionMatrix(decisionMatrixStatsData.ElementGuid);
        if (decisionMatrix is null)
        {
            _logger.LogInformation("Decision matrix with GUID {guid} not found", decisionMatrixStatsData.ElementGuid);
            return NotFound();
        }

        var filepath = GetStatsFilePath(user, decisionMatrixStatsData);
        CreateParentDirectories(filepath);
        var serializedStatsString = serializedStats.ToJsonString();
        if(System.IO.File.Exists(filepath))
        {
            _logger.LogWarning("Decision matrix stats data for decision matrix with GUID {guid} already exists",
                decisionMatrixStatsData.Guid);
            return BadRequest();
        }

        var decisionMatrixStats = await GetDecisionMatrixStats(decisionMatrixStatsData.Guid);
        if (decisionMatrixStats is not null)
        {
            _logger.LogWarning("Decision matrix stats for decision matrix with GUID {guid} already exists",
                decisionMatrixStatsData.Guid);
            return BadRequest();
        }
        
        decisionMatrixStats = decisionMatrixStatsData.ToModel();
        decisionMatrixStats.Filepath = filepath;
        decisionMatrixStats.Matrix = decisionMatrix;
        decisionMatrixStats.Participant = user;
        
        _context.DecisionMatrixStats.Add(decisionMatrixStats);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Decision matrix stats data for decision matrix with GUID {guid} created",
            decisionMatrixStatsData.Guid);
        
        await System.IO.File.WriteAllTextAsync(filepath, serializedStatsString);
        _logger.LogInformation("Decision matrix stats data for decision matrix with GUID {guid} saved",
            decisionMatrixStatsData.Guid);

        return Ok(new { Message = "Decision matrix stats data uploaded successfully" });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Task<DecisionMatrix?> GetDecisionMatrix(Guid guid)
    {
        return _context.DecisionMatrices
                       .SingleOrDefaultAsync(de => de.Guid == guid);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Task<DecisionMatrixStats?> GetDecisionMatrixStats(Guid guid)
    {
        return _context.DecisionMatrixStats
                       .SingleOrDefaultAsync(dms => dms.Guid == guid);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static List<DecisionMatrixDto> ToDtos(IEnumerable<DecisionMatrix> decisionMatrices)
    {
        return decisionMatrices.Select(dm => dm.ToDto()).ToList();
    }
}
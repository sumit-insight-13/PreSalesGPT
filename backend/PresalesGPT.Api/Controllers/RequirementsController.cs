using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PresalesGPT.Api.Data;
using PresalesGPT.Api.Models;
using PresalesGPT.Api.Services;

namespace PresalesGPT.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RequirementsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AiRequirementService _ai;

    public RequirementsController(
        AppDbContext db,
        AiRequirementService ai)
    {
        _db = db;
        _ai = ai;
    }

    [HttpGet("by-opportunity/{opportunityId:int}")]
    public async Task<IActionResult> GetByOpportunity(
        int opportunityId)
    {
        var requirements = await _db.Requirements
            .Where(r => r.OpportunityId == opportunityId)
            .OrderBy(r => r.Code)
            .ToListAsync();

        return Ok(requirements);
    }

    [HttpPost("extract/{documentId:int}")]
    public async Task<IActionResult> Extract(
        int documentId)
    {
        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document is null)
        {
            return NotFound(new
            {
                message = $"Document {documentId} not found."
            });
        }

        if (string.IsNullOrWhiteSpace(document.ExtractedText))
        {
            return BadRequest(new
            {
                message =
                    "Is document me koi text nahi hai. " +
                    "Pehle extraction successful hona chahiye."
            });
        }

        List<ExtractedRequirement> extracted;

        try
        {
            extracted = await _ai.ExtractAsync(
                document.ExtractedText);
        }
        catch (Exception exception)
        {
            return StatusCode(500, new
            {
                message = exception.Message
            });
        }

        var existing = await _db.Requirements
            .Where(r => r.DocumentId == documentId)
            .ToListAsync();

        _db.Requirements.RemoveRange(existing);

        var saved = new List<Requirement>();

        foreach (var item in extracted)
        {
            var requirement = new Requirement
            {
                OpportunityId = document.OpportunityId,
                DocumentId = document.Id,
                Code = item.Code,
                Description = item.Description,
                Category = item.Category,
                Priority = item.Priority,
                IsMandatory = item.IsMandatory,
                SourceReference = item.SourceReference,
                IsApproved = false,
                CreatedOn = DateTime.UtcNow
            };

            saved.Add(requirement);
        }

        _db.Requirements.AddRange(saved);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            count = saved.Count,
            requirements = saved
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] Requirement input)
    {
        var requirement = await _db.Requirements
            .FirstOrDefaultAsync(r => r.Id == id);

        if (requirement is null)
        {
            return NotFound(new
            {
                message = $"Requirement {id} not found."
            });
        }

        requirement.Description = input.Description;
        requirement.Category = input.Category;
        requirement.Priority = input.Priority;
        requirement.IsMandatory = input.IsMandatory;

        await _db.SaveChangesAsync();

        return Ok(requirement);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var requirement = await _db.Requirements
            .FirstOrDefaultAsync(r => r.Id == id);

        if (requirement is null)
        {
            return NotFound(new
            {
                message = $"Requirement {id} not found."
            });
        }

        requirement.IsApproved = true;

        await _db.SaveChangesAsync();

        return Ok(requirement);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var requirement = await _db.Requirements
            .FirstOrDefaultAsync(r => r.Id == id);

        if (requirement is null)
        {
            return NotFound(new
            {
                message = $"Requirement {id} not found."
            });
        }

        _db.Requirements.Remove(requirement);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Requirement deleted."
        });
    }
}

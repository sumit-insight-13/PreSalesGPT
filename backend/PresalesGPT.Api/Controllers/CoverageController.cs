using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PresalesGPT.Api.Data;
using PresalesGPT.Api.Services;

namespace PresalesGPT.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoverageController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CoverageService _coverage;

    public CoverageController(
        AppDbContext db,
        CoverageService coverage)
    {
        _db = db;
        _coverage = coverage;
    }

    [HttpGet("{opportunityId:int}")]
    public async Task<IActionResult> GetReport(
        int opportunityId)
    {
        var opportunity = await _db.Opportunities
            .FirstOrDefaultAsync(o => o.Id == opportunityId);

        if (opportunity is null)
        {
            return NotFound(new
            {
                message = $"Opportunity {opportunityId} not found."
            });
        }

        var requirements = await _db.Requirements
            .Where(r => r.OpportunityId == opportunityId
                        && r.IsApproved)
            .OrderBy(r => r.Code)
            .ToListAsync();

        var sections = await _db.ProposalSections
            .Where(s => s.OpportunityId == opportunityId)
            .OrderBy(s => s.SectionOrder)
            .ToListAsync();

        if (sections.Count == 0)
        {
            return BadRequest(new
            {
                message =
                    "Koi proposal section nahi mila. " +
                    "Pehle proposal generate karo."
            });
        }

        var documentTexts = await _db.Documents
            .Where(d => d.OpportunityId == opportunityId
                        && d.Status == "Extracted")
            .Select(d => d.ExtractedText)
            .ToListAsync();

        var sourceText = string.Join("\n", documentTexts);

        var report = _coverage.Build(
            requirements,
            sections,
            sourceText,
            opportunity.CustomerName);

        return Ok(report);
    }
}

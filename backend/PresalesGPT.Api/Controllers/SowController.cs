using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PresalesGPT.Api.Data;
using PresalesGPT.Api.Models;
using PresalesGPT.Api.Services;

namespace PresalesGPT.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SowController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SowService _sow;

    public SowController(AppDbContext db, SowService sow)
    {
        _db = db;
        _sow = sow;
    }

    [HttpGet("by-opportunity/{opportunityId:int}")]
    public async Task<IActionResult> GetByOpportunity(
        int opportunityId)
    {
        var sections = await _db.SowSections
            .Where(s => s.OpportunityId == opportunityId)
            .OrderBy(s => s.SectionOrder)
            .ToListAsync();

        return Ok(sections);
    }

    [HttpPost("generate/{opportunityId:int}")]
    public async Task<IActionResult> Generate(int opportunityId)
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

        var proposal = await _db.ProposalSections
            .Where(s => s.OpportunityId == opportunityId)
            .OrderBy(s => s.SectionOrder)
            .ToListAsync();

        if (proposal.Count == 0)
        {
            return BadRequest(new
            {
                message =
                    "There is no proposal yet. Generate and approve " +
                    "the proposal before drafting a Statement of Work."
            });
        }

        // A Statement of Work is contractual. It is drafted only from
        // proposal content a human has signed off, never from an
        // unreviewed draft.
        var unapproved = proposal.Count(s => !s.IsApproved);

        if (unapproved > 0)
        {
            return BadRequest(new
            {
                message =
                    $"{unapproved} proposal section(s) are still " +
                    $"awaiting review. A Statement of Work can only be " +
                    $"drafted from a fully approved proposal."
            });
        }

        var requirements = await _db.Requirements
            .Where(r => r.OpportunityId == opportunityId
                        && r.IsApproved)
            .OrderBy(r => r.Code)
            .ToListAsync();

        var existing = await _db.SowSections
            .Where(s => s.OpportunityId == opportunityId)
            .ToListAsync();

        _db.SowSections.RemoveRange(existing);
        await _db.SaveChangesAsync();

        var generated = new List<SowSection>();
        var failures = new List<string>();

        foreach (var definition in SowService.Sections)
        {
            try
            {
                var result = await _sow.GenerateSectionAsync(
                    definition,
                    opportunity,
                    requirements,
                    proposal);

                generated.Add(new SowSection
                {
                    OpportunityId = opportunityId,
                    SectionOrder = definition.Order,
                    SectionName = definition.Name,
                    Content = result.Content,
                    CoveredRequirements =
                        result.CoveredRequirements,
                    IsApproved = false,
                    IsEdited = false,
                    GeneratedOn = DateTime.UtcNow
                });
            }
            catch (Exception exception)
            {
                failures.Add(
                    $"{definition.Name}: {exception.Message}");
            }
        }

        _db.SowSections.AddRange(generated);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            sectionsGenerated = generated.Count,
            requirementsUsed = requirements.Count,
            failures,
            sections = generated
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SowSection input)
    {
        var section = await _db.SowSections
            .FirstOrDefaultAsync(s => s.Id == id);

        if (section is null)
        {
            return NotFound(new
            {
                message = $"Section {id} not found."
            });
        }

        section.Content = input.Content;
        section.IsEdited = true;
        section.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(section);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var section = await _db.SowSections
            .FirstOrDefaultAsync(s => s.Id == id);

        if (section is null)
        {
            return NotFound(new
            {
                message = $"Section {id} not found."
            });
        }

        section.IsApproved = true;
        section.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(section);
    }

    [HttpGet("export/{opportunityId:int}")]
    public async Task<IActionResult> Export(int opportunityId)
    {
        var opportunity = await _db.Opportunities
            .FirstOrDefaultAsync(o => o.Id == opportunityId);

        if (opportunity is null)
        {
            return NotFound(new
            {
                message = "Opportunity not found."
            });
        }

        var sections = await _db.SowSections
            .Where(s => s.OpportunityId == opportunityId)
            .OrderBy(s => s.SectionOrder)
            .ToListAsync();

        if (sections.Count == 0)
        {
            return BadRequest(new
            {
                message = "No Statement of Work has been drafted."
            });
        }

        var pending = sections.Count(s => !s.IsApproved);

        var builder = new StringBuilder();

        builder.AppendLine("# Statement of Work");
        builder.AppendLine();
        builder.AppendLine($"**Engagement:** {opportunity.Name}");
        builder.AppendLine(
            $"**Drafted:** {DateTime.UtcNow:dd MMM yyyy}");
        builder.AppendLine();

        builder.AppendLine(
            "> This draft has not been reviewed by Legal. It is not a " +
            "contractual offer and must not be issued to a customer " +
            "without legal review.");
        builder.AppendLine();

        if (pending > 0)
        {
            builder.AppendLine(
                $"> DRAFT - {pending} section(s) still awaiting " +
                $"review.");
            builder.AppendLine();
        }

        builder.AppendLine("---");
        builder.AppendLine();

        foreach (var section in sections)
        {
            builder.AppendLine(
                $"## {section.SectionOrder}. {section.SectionName}");
            builder.AppendLine();
            builder.AppendLine(section.Content);
            builder.AppendLine();
        }

        var fileName =
            $"SOW-{opportunity.Name}-" +
            $"{DateTime.UtcNow:yyyyMMdd}.md";

        return File(
            Encoding.UTF8.GetBytes(builder.ToString()),
            "text/markdown",
            fileName);
    }
}

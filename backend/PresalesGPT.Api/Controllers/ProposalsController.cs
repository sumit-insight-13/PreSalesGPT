using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PresalesGPT.Api.Data;
using PresalesGPT.Api.Models;
using PresalesGPT.Api.Services;

namespace PresalesGPT.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProposalsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ProposalService _proposal;

    public ProposalsController(
        AppDbContext db,
        ProposalService proposal)
    {
        _db = db;
        _proposal = proposal;
    }

    [HttpGet("by-opportunity/{opportunityId:int}")]
    public async Task<IActionResult> GetByOpportunity(
        int opportunityId)
    {
        var sections = await _db.ProposalSections
            .Where(s => s.OpportunityId == opportunityId)
            .OrderBy(s => s.SectionOrder)
            .ToListAsync();

        return Ok(sections);
    }

    [HttpPost("generate/{opportunityId:int}")]
    public async Task<IActionResult> Generate(
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

        if (requirements.Count == 0)
        {
            return BadRequest(new
            {
                message =
                    "Koi approved requirement nahi mili. " +
                    "Proposal generate karne se pehle " +
                    "requirements approve karo."
            });
        }

        var existing = await _db.ProposalSections
            .Where(s => s.OpportunityId == opportunityId)
            .ToListAsync();

        _db.ProposalSections.RemoveRange(existing);

        await _db.SaveChangesAsync();

        var generated = new List<ProposalSection>();
        var failures = new List<string>();

        foreach (var definition in ProposalService.Sections)
        {
            try
            {
                var result =
                    await _proposal.GenerateSectionAsync(
                        definition,
                        opportunity,
                        requirements);

                generated.Add(new ProposalSection
                {
                    OpportunityId = opportunityId,
                    SectionOrder = definition.Order,
                    SectionName = definition.Name,
                    Content = result.Content,
                    CoveredRequirements =
                        result.CoveredRequirements,
                    Assumptions = result.Assumptions,
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

        _db.ProposalSections.AddRange(generated);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            requirementsUsed = requirements.Count,
            sectionsGenerated = generated.Count,
            failures,
            sections = generated
        });
    }

    [HttpPost("regenerate-section/{sectionId:int}")]
    public async Task<IActionResult> RegenerateSection(
        int sectionId)
    {
        var section = await _db.ProposalSections
            .FirstOrDefaultAsync(s => s.Id == sectionId);

        if (section is null)
        {
            return NotFound(new
            {
                message = $"Section {sectionId} not found."
            });
        }

        var opportunity = await _db.Opportunities
            .FirstOrDefaultAsync(
                o => o.Id == section.OpportunityId);

        if (opportunity is null)
        {
            return NotFound(new
            {
                message = "Opportunity not found."
            });
        }

        var requirements = await _db.Requirements
            .Where(r => r.OpportunityId == section.OpportunityId
                        && r.IsApproved)
            .OrderBy(r => r.Code)
            .ToListAsync();

        var definition = ProposalService.Sections
            .FirstOrDefault(
                d => d.Order == section.SectionOrder);

        if (definition is null)
        {
            return BadRequest(new
            {
                message = "Section definition not found."
            });
        }

        try
        {
            var result =
                await _proposal.GenerateSectionAsync(
                    definition,
                    opportunity,
                    requirements);

            section.Content = result.Content;
            section.CoveredRequirements =
                result.CoveredRequirements;
            section.Assumptions = result.Assumptions;
            section.IsApproved = false;
            section.IsEdited = false;
            section.UpdatedOn = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(section);
        }
        catch (Exception exception)
        {
            return StatusCode(500, new
            {
                message = exception.Message
            });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ProposalSection input)
    {
        var section = await _db.ProposalSections
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
        var section = await _db.ProposalSections
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
    public async Task<IActionResult> Export(
        int opportunityId)
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

        var sections = await _db.ProposalSections
            .Where(s => s.OpportunityId == opportunityId)
            .OrderBy(s => s.SectionOrder)
            .ToListAsync();

        if (sections.Count == 0)
        {
            return BadRequest(new
            {
                message = "Koi proposal section nahi mila."
            });
        }

        var pending = sections.Count(s => !s.IsApproved);

        var builder = new StringBuilder();

        builder.AppendLine($"# Proposal: {opportunity.Name}");
        builder.AppendLine();
        builder.AppendLine(
            $"**Customer:** {opportunity.CustomerName}");
        builder.AppendLine(
            $"**Industry:** {opportunity.Industry}");
        builder.AppendLine(
            $"**Generated:** {DateTime.UtcNow:dd MMM yyyy}");
        builder.AppendLine();

        if (pending > 0)
        {
            builder.AppendLine(
                $"> DRAFT - {pending} section(s) still awaiting " +
                $"review. Not for customer release.");
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

            if (!string.IsNullOrWhiteSpace(
                    section.Assumptions))
            {
                builder.AppendLine("**Assumptions**");
                builder.AppendLine();
                builder.AppendLine(section.Assumptions);
                builder.AppendLine();
            }

            builder.AppendLine();
        }

        var fileName =
            $"Proposal-{opportunity.CustomerName}-" +
            $"{DateTime.UtcNow:yyyyMMdd}.md";

        var bytes = Encoding.UTF8.GetBytes(
            builder.ToString());

        return File(bytes, "text/markdown", fileName);
    }
}

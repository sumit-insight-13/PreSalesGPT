using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PresalesGPT.Api.Data;
using PresalesGPT.Api.Models;
using PresalesGPT.Api.Services;

namespace PresalesGPT.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClarificationsController : ControllerBase
{
    private static readonly string[] ImpactOrder =
    {
        "Blocking",
        "Important",
        "Minor"
    };

    private readonly AppDbContext _db;
    private readonly ClarificationService _clarifications;

    public ClarificationsController(
        AppDbContext db,
        ClarificationService clarifications)
    {
        _db = db;
        _clarifications = clarifications;
    }

    [HttpGet("by-opportunity/{opportunityId:int}")]
    public async Task<IActionResult> GetByOpportunity(
        int opportunityId)
    {
        var items = await _db.Clarifications
            .Where(c => c.OpportunityId == opportunityId)
            .ToListAsync();

        return Ok(Sorted(items));
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

        var documentTexts = await _db.Documents
            .Where(d => d.OpportunityId == opportunityId
                        && d.Status == "Extracted")
            .Select(d => d.ExtractedText)
            .ToListAsync();

        var sourceText = string.Join("\n", documentTexts);

        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return BadRequest(new
            {
                message =
                    "No extracted document text found. Upload the " +
                    "tender document first."
            });
        }

        // Clarifications are prepared from everything extracted, not
        // only the approved set. A requirement a reviewer rejected as
        // unclear is exactly the kind of thing worth asking about.
        var requirements = await _db.Requirements
            .Where(r => r.OpportunityId == opportunityId)
            .OrderBy(r => r.Code)
            .ToListAsync();

        List<GeneratedClarification> generated;

        try
        {
            generated = await _clarifications.GenerateAsync(
                opportunity,
                requirements,
                sourceText);
        }
        catch (Exception exception)
        {
            return StatusCode(500, new
            {
                message = exception.Message
            });
        }

        var existing = await _db.Clarifications
            .Where(c => c.OpportunityId == opportunityId)
            .ToListAsync();

        _db.Clarifications.RemoveRange(existing);

        var saved = new List<Clarification>();
        var number = 1;

        foreach (var item in generated)
        {
            var reference =
                string.IsNullOrWhiteSpace(item.Reference)
                    ? $"Q{number:00}"
                    : item.Reference;

            saved.Add(new Clarification
            {
                OpportunityId = opportunityId,
                Reference = reference,
                Question = item.Question,
                Rationale = item.Rationale,
                Category = item.Category,
                Impact = NormaliseImpact(item.Impact),
                RelatedRequirements = item.RelatedRequirements,
                SourceReference = item.SourceReference,
                IsApproved = false,
                IsEdited = false,
                CreatedOn = DateTime.UtcNow
            });

            number++;
        }

        _db.Clarifications.AddRange(saved);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            questionsGenerated = saved.Count,
            blockingCount = saved.Count(
                c => c.Impact == "Blocking"),
            requirementsConsidered = requirements.Count,
            clarifications = Sorted(saved)
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] Clarification input)
    {
        var item = await _db.Clarifications
            .FirstOrDefaultAsync(c => c.Id == id);

        if (item is null)
        {
            return NotFound(new
            {
                message = $"Clarification {id} not found."
            });
        }

        item.Question = input.Question;
        item.Rationale = input.Rationale;
        item.Impact = NormaliseImpact(input.Impact);
        item.IsEdited = true;
        item.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(item);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var item = await _db.Clarifications
            .FirstOrDefaultAsync(c => c.Id == id);

        if (item is null)
        {
            return NotFound(new
            {
                message = $"Clarification {id} not found."
            });
        }

        item.IsApproved = true;
        item.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Clarifications
            .FirstOrDefaultAsync(c => c.Id == id);

        if (item is null)
        {
            return NotFound(new
            {
                message = $"Clarification {id} not found."
            });
        }

        _db.Clarifications.Remove(item);

        await _db.SaveChangesAsync();

        return Ok(new { message = "Clarification deleted." });
    }

    /// <summary>
    /// Exports only approved questions, and without the internal
    /// rationale. The rationale is written to be blunt about commercial
    /// and delivery consequences, so it must never reach the customer.
    /// </summary>
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

        var approved = await _db.Clarifications
            .Where(c => c.OpportunityId == opportunityId
                        && c.IsApproved)
            .ToListAsync();

        if (approved.Count == 0)
        {
            return BadRequest(new
            {
                message =
                    "No approved questions. Review and approve the " +
                    "questions you intend to submit before exporting."
            });
        }

        var builder = new StringBuilder();

        builder.AppendLine("# Clarification Questions");
        builder.AppendLine();
        builder.AppendLine($"**Tender:** {opportunity.Name}");
        builder.AppendLine(
            $"**Submitted:** {DateTime.UtcNow:dd MMM yyyy}");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();

        var number = 1;

        foreach (var item in Sorted(approved))
        {
            builder.AppendLine($"**{number}.** {item.Question}");

            if (!string.IsNullOrWhiteSpace(item.SourceReference) ||
                !string.IsNullOrWhiteSpace(item.RelatedRequirements))
            {
                var parts = new List<string>();

                if (!string.IsNullOrWhiteSpace(
                        item.RelatedRequirements))
                {
                    parts.Add($"Ref: {item.RelatedRequirements}");
                }

                if (!string.IsNullOrWhiteSpace(
                        item.SourceReference))
                {
                    parts.Add(item.SourceReference);
                }

                builder.AppendLine();
                builder.AppendLine(
                    $"*{string.Join(" | ", parts)}*");
            }

            builder.AppendLine();
            number++;
        }

        var fileName =
            $"Clarifications-{opportunity.Name}-" +
            $"{DateTime.UtcNow:yyyyMMdd}.md";

        return File(
            Encoding.UTF8.GetBytes(builder.ToString()),
            "text/markdown",
            fileName);
    }

    private static List<Clarification> Sorted(
        List<Clarification> items)
    {
        return items
            .OrderBy(c =>
            {
                var index = Array.IndexOf(ImpactOrder, c.Impact);
                return index < 0 ? ImpactOrder.Length : index;
            })
            .ThenBy(c => c.Reference)
            .ToList();
    }

    private static string NormaliseImpact(string? impact)
    {
        if (string.IsNullOrWhiteSpace(impact))
        {
            return "Important";
        }

        var match = ImpactOrder.FirstOrDefault(value =>
            string.Equals(
                value,
                impact.Trim(),
                StringComparison.OrdinalIgnoreCase));

        return match ?? "Important";
    }
}

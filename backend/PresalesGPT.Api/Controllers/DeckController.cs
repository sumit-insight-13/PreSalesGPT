using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PresalesGPT.Api.Data;
using PresalesGPT.Api.Models;
using PresalesGPT.Api.Services;

namespace PresalesGPT.Api.Controllers;

/// <summary>
/// Shape of the JSON held in DeckSlide.LayoutData.
/// </summary>
internal class DeckLayoutData
{
    public List<SlideStat> Stats { get; set; } = new();

    public List<SlideColumn> Columns { get; set; } = new();

    public string Callout { get; set; } = "";
}

[ApiController]
[Route("api/[controller]")]
public class DeckController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

    private readonly AppDbContext _db;
    private readonly DeckService _deck;
    private readonly PowerPointBuilder _builder;

    public DeckController(
        AppDbContext db,
        DeckService deck,
        PowerPointBuilder builder)
    {
        _db = db;
        _deck = deck;
        _builder = builder;
    }

    [HttpGet("by-opportunity/{opportunityId:int}")]
    public async Task<IActionResult> GetByOpportunity(
        int opportunityId)
    {
        var slides = await _db.DeckSlides
            .Where(s => s.OpportunityId == opportunityId)
            .OrderBy(s => s.SlideOrder)
            .ToListAsync();

        return Ok(slides);
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

        var approved = await _db.ProposalSections
            .Where(s => s.OpportunityId == opportunityId
                        && s.IsApproved)
            .OrderBy(s => s.SectionOrder)
            .ToListAsync();

        if (approved.Count == 0)
        {
            return BadRequest(new
            {
                message =
                    "No approved proposal sections found. Approve at " +
                    "least one section before building the deck."
            });
        }

        List<GeneratedSlide> slides;

        try
        {
            slides = await _deck.GenerateAsync(
                opportunity,
                approved);
        }
        catch (Exception exception)
        {
            return StatusCode(500, new
            {
                message = exception.Message
            });
        }

        var existing = await _db.DeckSlides
            .Where(s => s.OpportunityId == opportunityId)
            .ToListAsync();

        _db.DeckSlides.RemoveRange(existing);

        var order = 1;
        var saved = new List<DeckSlide>();

        foreach (var slide in slides)
        {
            var layoutData = new DeckLayoutData
            {
                Stats = slide.Stats,
                Columns = slide.Columns,
                Callout = slide.Callout
            };

            saved.Add(new DeckSlide
            {
                OpportunityId = opportunityId,
                SlideOrder = order++,
                Layout = slide.Layout,
                Title = slide.Title,
                Bullets = string.Join("\n", slide.Bullets),
                LayoutData = JsonSerializer.Serialize(
                    layoutData,
                    JsonOptions),
                SpeakerNotes = slide.SpeakerNotes,
                SourceSection = slide.SourceSection,
                GeneratedOn = DateTime.UtcNow
            });
        }

        _db.DeckSlides.AddRange(saved);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            slidesGenerated = saved.Count,
            sectionsUsed = approved.Count,
            layoutMix = saved
                .GroupBy(s => s.Layout)
                .ToDictionary(g => g.Key, g => g.Count()),
            slides = saved
        });
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

        var slides = await _db.DeckSlides
            .Where(s => s.OpportunityId == opportunityId)
            .OrderBy(s => s.SlideOrder)
            .ToListAsync();

        if (slides.Count == 0)
        {
            return BadRequest(new
            {
                message = "No deck has been generated."
            });
        }

        var pendingSections = await _db.ProposalSections
            .CountAsync(s => s.OpportunityId == opportunityId
                             && !s.IsApproved);

        var subtitle = pendingSections > 0
            ? "DRAFT - not for customer release"
            : $"Prepared {DateTime.UtcNow:dd MMMM yyyy}";

        var content = slides
            .Select(ToSlideContent)
            .ToList();

        byte[] bytes;

        try
        {
            bytes = _builder.Build(
                opportunity.Name,
                subtitle,
                content);
        }
        catch (Exception exception)
        {
            return StatusCode(500, new
            {
                message =
                    $"Could not build the presentation: " +
                    $"{exception.Message}"
            });
        }

        var fileName =
            $"Deck-{opportunity.Name}-" +
            $"{DateTime.UtcNow:yyyyMMdd}.pptx";

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument." +
            "presentationml.presentation",
            fileName);
    }

    private static SlideContent ToSlideContent(DeckSlide slide)
    {
        var content = new SlideContent
        {
            Layout = string.IsNullOrWhiteSpace(slide.Layout)
                ? "Bullets"
                : slide.Layout,
            Title = slide.Title,
            SpeakerNotes = slide.SpeakerNotes,
            SourceSection = slide.SourceSection,
            Bullets = (slide.Bullets ?? "")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(bullet => bullet.Trim())
                .Where(bullet => bullet.Length > 0)
                .ToList()
        };

        if (string.IsNullOrWhiteSpace(slide.LayoutData))
        {
            return content;
        }

        try
        {
            var data = JsonSerializer.Deserialize<DeckLayoutData>(
                slide.LayoutData,
                JsonOptions);

            if (data is null)
            {
                return content;
            }

            content.Callout = data.Callout;

            content.Stats = data.Stats
                .Select(stat => new SlideStatContent
                {
                    Value = stat.Value,
                    Label = stat.Label
                })
                .ToList();

            content.Columns = data.Columns
                .Select(column => new SlideColumnContent
                {
                    Heading = column.Heading,
                    Items = column.Items
                })
                .ToList();
        }
        catch (JsonException)
        {
            // Stored layout data that will not parse should not stop the
            // export. The slide falls back to its bullets.
        }

        return content;
    }
}

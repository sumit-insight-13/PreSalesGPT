using System.Text;
using System.Text.Json;
using PresalesGPT.Api.Models;

namespace PresalesGPT.Api.Services;

public class SlideStat
{
    public string Value { get; set; } = "";

    public string Label { get; set; } = "";
}

public class SlideColumn
{
    public string Heading { get; set; } = "";

    public List<string> Items { get; set; } = new();
}

public class GeneratedSlide
{
    public string Layout { get; set; } = "Bullets";

    public string Title { get; set; } = "";

    public List<string> Bullets { get; set; } = new();

    public List<SlideStat> Stats { get; set; } = new();

    public List<SlideColumn> Columns { get; set; } = new();

    public string Callout { get; set; } = "";

    public string SpeakerNotes { get; set; } = "";

    public string SourceSection { get; set; } = "";
}

public class DeckService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public DeckService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    private const string SystemPrompt = """
        You are a presales consultant building a customer presentation from
        an approved proposal.

        WHAT THIS DECK IS
        A deck supports a conversation. It is not the proposal reformatted.
        Each slide should give the presenter something to talk around, not
        something to read aloud.

        SOURCE OF TRUTH
        The approved proposal text is supplied to you. Every claim on every
        slide must come from it. You must not add a capability, a number, a
        client name, a credential or a commitment that the proposal does not
        contain. If the proposal carries a placeholder such as
        [Commercial team to confirm], keep that placeholder on the slide
        rather than filling it in.

        PRODUCT AND VENDOR NAMES
        Where the source names a specific product, vendor or system, carry
        that name through exactly. "11 Oracle databases" must not become
        "11 databases".
        The rule against inventing product names applies only to names
        absent from the source. It is not a reason to remove names that are
        present.

        CUSTOMER IDENTITY
        The customer name supplied to you is a label and may be unreliable.
        Do not name any organisation on any slide. Write "your organisation"
        where a name would otherwise appear.

        CHOOSING A LAYOUT
        Every slide declares a layout. A deck where every slide is bullets is a
        deck nobody remembers.

        You MUST meet this quota across the deck:
        - At least 2 slides with layout "Stats"
        - At least 2 slides with layout "TwoColumn"
        - At least 1 slide with layout "Callout"
        - The rest may be "Bullets"

        Before you write the slides, scan the proposal and decide which content
        fits which layout. Do not write the deck first and then label it.

        "Stats" - the slide's point rests on two to four figures drawn from the
        proposal. This proposal contains many: counts of servers, databases and
        stores, percentage targets, downtime limits, recovery objectives,
        retention periods, concurrency figures. Any of these can carry a Stats
        slide. Each stat is a short value and a short label.

        "TwoColumn" - the content is a genuine pairing. This proposal contains
        several: what is in scope against what remains to be confirmed, supplier
        responsibilities against customer responsibilities, a risk against its
        mitigation, operational retention against audit retention.

        "Callout" - a single sentence carries the slide. A binding constraint, a
        deadline, a commitment. At most 25 words.

        "Bullets" - the default, three to five bullets.

        If you find yourself writing a fourth consecutive Bullets slide, stop and
        ask whether the content would land better as Stats or TwoColumn.

        SLIDE CONSTRUCTION
        Produce between 12 and 16 slides.
        Every slide must have a title that states a point, not a label.
        Prefer "Migration waves protect trading continuity" over "Approach".
        Each bullet is at most 14 words. A bullet is a prompt, not a
        sentence to be read out.
        Never put a paragraph on a slide.
        Stat values are short: "25%", "412", "31 Jan 2028", "4 hours".
        Stat labels are at most 8 words.

        SPEAKER NOTES
        Every slide carries speaker notes of 40 to 80 words giving the
        presenter the detail behind the slide, drawn from the proposal.
        Notes may be full sentences.

        STRUCTURE
        Open with a slide framing the customer's situation in their terms.
        Close with a slide on next steps that proposes discussion, not
        agreement. Do not close on a commercial ask.
        In between, follow the logic of the approved proposal.
        Spread coverage across the proposal. No single proposal section should supply more than three slides. Delivery
        approach and risks each warrant their own slides.
        Set sourceSection to "Next steps" for the closing slide rather than
        attributing it to a content section.

        STYLE
        Professional British English. No marketing superlatives such as
        world-class, cutting-edge, best-in-class or industry leading. No
        exclamation marks.

        OUTPUT
        Return ONLY valid JSON, no markdown fences. Include only the fields
        the chosen layout uses:
        {"slides":[
          {"layout":"Bullets","title":"...","bullets":["...","..."],
           "speakerNotes":"...","sourceSection":"Proposed Solution"},
          {"layout":"Stats","title":"...",
           "stats":[{"value":"25%","label":"Annual cost reduction"}],
           "speakerNotes":"...","sourceSection":"Executive Summary"},
          {"layout":"TwoColumn","title":"...",
           "columns":[{"heading":"In scope","items":["...","..."]},
                      {"heading":"To confirm","items":["...","..."]}],
           "speakerNotes":"...","sourceSection":"Scope of Work"},
          {"layout":"Callout","title":"...",
           "callout":"One sentence that carries the slide.",
           "speakerNotes":"...","sourceSection":"Delivery Approach"}
        ]}
        """;

    public async Task<List<GeneratedSlide>> GenerateAsync(
        Opportunity opportunity,
        List<ProposalSection> approvedProposal)
    {
        var endpoint = _configuration["AzureOpenAI:Endpoint"];
        var apiKey = _configuration["AzureOpenAI:ApiKey"];
        var deployment = _configuration["AzureOpenAI:Deployment"];
        var apiVersion = _configuration["AzureOpenAI:ApiVersion"];

        if (string.IsNullOrWhiteSpace(endpoint) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(deployment))
        {
            throw new InvalidOperationException(
                "Azure OpenAI settings are missing in appsettings.json.");
        }

        var proposalText = new StringBuilder();

        foreach (var section in approvedProposal
                     .OrderBy(s => s.SectionOrder))
        {
            proposalText.AppendLine($"### {section.SectionName}");
            proposalText.AppendLine(section.Content);
            proposalText.AppendLine();
        }

        var userPrompt =
            $"OPPORTUNITY: {opportunity.Name}\n" +
            $"INDUSTRY: {opportunity.Industry}\n\n" +
            $"APPROVED PROPOSAL:\n{proposalText}\n\n" +
            $"Build the customer presentation.";

        var requestBody = new Dictionary<string, object>
        {
            ["messages"] = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = userPrompt }
            },
            ["max_completion_tokens"] = 14000,
            ["response_format"] = new { type = "json_object" }
        };

        var url =
            $"{endpoint.TrimEnd('/')}/openai/deployments/" +
            $"{deployment}/chat/completions?api-version={apiVersion}";

        var request = new HttpRequestMessage(HttpMethod.Post, url);

        request.Headers.Add("api-key", apiKey);

        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Azure OpenAI call failed " +
                $"({(int)response.StatusCode}): {responseBody}");
        }

        using var parsed = JsonDocument.Parse(responseBody);

        var choices = parsed.RootElement.GetProperty("choices");

        if (choices.GetArrayLength() == 0)
        {
            throw new InvalidOperationException(
                "The model returned no choices.");
        }

        var message = choices[0].GetProperty("message");

        if (!message.TryGetProperty("content", out var element))
        {
            throw new InvalidOperationException(
                "The model response contained no content.");
        }

        var content = element.GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                "The deck response was empty. Increase " +
                "max_completion_tokens or shorten the proposal.");
        }

        return ParseSlides(content);
    }

    private List<GeneratedSlide> ParseSlides(string content)
    {
        var cleaned = content.Trim();

        if (cleaned.StartsWith("```"))
        {
            var firstNewLine = cleaned.IndexOf('\n');

            if (firstNewLine > -1)
            {
                cleaned = cleaned[(firstNewLine + 1)..];
            }

            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned[..^3];
            }

            cleaned = cleaned.Trim();
        }

        using var parsed = JsonDocument.Parse(cleaned);

        if (!parsed.RootElement.TryGetProperty(
                "slides",
                out var array))
        {
            return new List<GeneratedSlide>();
        }

        var slides = new List<GeneratedSlide>();

        foreach (var item in array.EnumerateArray())
        {
            var title = ReadString(item, "title");

            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var slide = new GeneratedSlide
            {
                Layout = NormaliseLayout(ReadString(item, "layout")),
                Title = title,
                Callout = ReadString(item, "callout"),
                SpeakerNotes = ReadString(item, "speakerNotes"),
                SourceSection = ReadString(item, "sourceSection")
            };

            slide.Bullets = ReadStringArray(item, "bullets");

            if (item.TryGetProperty("stats", out var stats) &&
                stats.ValueKind == JsonValueKind.Array)
            {
                foreach (var stat in stats.EnumerateArray())
                {
                    var value = ReadString(stat, "value");

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    slide.Stats.Add(new SlideStat
                    {
                        Value = value,
                        Label = ReadString(stat, "label")
                    });
                }
            }

            if (item.TryGetProperty("columns", out var columns) &&
                columns.ValueKind == JsonValueKind.Array)
            {
                foreach (var column in columns.EnumerateArray())
                {
                    slide.Columns.Add(new SlideColumn
                    {
                        Heading = ReadString(column, "heading"),
                        Items = ReadStringArray(column, "items")
                    });
                }
            }

            // A layout the model chose but did not populate would render
            // as an empty slide. Fall back rather than ship a blank.
            slide.Layout = ResolveLayout(slide);

            slides.Add(slide);
        }

        return slides;
    }

    private static string ResolveLayout(GeneratedSlide slide)
    {
        return slide.Layout switch
        {
            "Stats" when slide.Stats.Count == 0 => "Bullets",
            "TwoColumn" when slide.Columns.Count < 2 => "Bullets",
            "Callout" when string.IsNullOrWhiteSpace(slide.Callout)
                => "Bullets",
            _ => slide.Layout
        };
    }

    private static string NormaliseLayout(string? layout)
    {
        if (string.IsNullOrWhiteSpace(layout))
        {
            return "Bullets";
        }

        var trimmed = layout.Trim();

        string[] known = { "Bullets", "Stats", "TwoColumn", "Callout" };

        var match = known.FirstOrDefault(value =>
            string.Equals(
                value,
                trimmed,
                StringComparison.OrdinalIgnoreCase));

        return match ?? "Bullets";
    }

    private List<string> ReadStringArray(
        JsonElement element,
        string propertyName)
    {
        var results = new List<string>();

        if (!element.TryGetProperty(propertyName, out var array) ||
            array.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var entry in array.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var text = entry.GetString();

            if (!string.IsNullOrWhiteSpace(text))
            {
                results.Add(text.Trim());
            }
        }

        return results;
    }

    private string ReadString(
        JsonElement element,
        string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var value) &&
            value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? "";
        }

        return "";
    }
}

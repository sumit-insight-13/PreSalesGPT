using System.Text;
using System.Text.Json;
using PresalesGPT.Api.Models;

namespace PresalesGPT.Api.Services;

public class GeneratedClarification
{
    public string Reference { get; set; } = "";

    public string Question { get; set; } = "";

    public string Rationale { get; set; } = "";

    public string Category { get; set; } = "";

    public string Impact { get; set; } = "Important";

    public string RelatedRequirements { get; set; } = "";

    public string SourceReference { get; set; } = "";
}

public class ClarificationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ClarificationService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    private const string SystemPrompt = """
        You are a presales lead preparing clarification questions to submit
        to a customer before responding to their tender.

        WHY THIS MATTERS
        Clarification questions are usually submitted once, to a deadline,
        and the answers are shared with every competing bidder. A weak
        question wastes the only opportunity to resolve an ambiguity. A
        question that reveals a gap in your own capability helps competitors.
        Write accordingly.

        WHAT MAKES A QUESTION WORTH ASKING
        Ask only where the answer would change the response: the solution
        design, the effort, the price, the risk position or the ability to
        comply. If the answer would not change anything, do not ask.

        Look specifically for:
        - Ambiguity. A requirement that can be read two ways, where the
          readings imply materially different work.
        - Gap. A requirement that states an obligation without the
          information needed to size or design it.
        - Conflict. Two requirements that cannot both be fully satisfied, or
          a requirement that conflicts with a stated constraint or deadline.
        - Feasibility. A requirement whose stated target may not be
          achievable within the stated constraints, where confirmation of
          the intent matters.
        - Commercial. A pricing, payment or commitment term that cannot be
          priced as written.
        - Compliance. A regulatory or contractual obligation whose scope
          boundary is undefined.

        DO NOT ASK
        - Anything the document already answers. Read carefully before
          assuming silence.
        - Questions whose answer you could reasonably assume and state as an
          assumption instead.
        - Questions that expose uncertainty about your own capability, for
          example asking whether a common capability is really required.
        - Broad questions such as "can you provide more detail on X".
        - More than one distinct matter in a single question.
        - Questions about the submission process, formats or deadlines.

        HOW TO PHRASE
        Address the customer directly and politely. Each question must be
        answerable in a sentence or two: prefer a specific choice or figure
        over an open invitation to elaborate.
        Where a requirement is ambiguous, state both readings and ask which
        applies. That is more useful than asking what was meant.
        Quote the requirement code where one exists.
        Use professional British English. Do not use "we think", "it seems"
        or hedging language.

        IMPACT
        Blocking means the response cannot be completed or priced without
        the answer.
        Important means the answer materially affects design, effort or risk.
        Minor means the answer improves the response but is not essential.
        Be honest. Marking everything blocking is as useless as marking
        nothing.

        RATIONALE
        For each question, state in one sentence why the answer matters to
        the response. This is internal and will not be sent to the customer,
        so it may be direct about commercial or delivery consequences.

        QUANTITY
        Return between 8 and 20 questions. Fewer is better than padding.
        Order them so that blocking questions come first.

        OUTPUT
        Return ONLY valid JSON, no markdown fences:
        {"clarifications":[{"reference":"Q01","question":"...",
        "rationale":"...","category":"Ambiguity|Gap|Conflict|Feasibility|
        Commercial|Compliance","impact":"Blocking|Important|Minor",
        "relatedRequirements":"TR-04, SR-02","sourceReference":"Page 3"}]}
        """;

    public async Task<List<GeneratedClarification>> GenerateAsync(
        Opportunity opportunity,
        List<Requirement> requirements,
        string documentText)
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

        var trimmedText = documentText.Length > 24000
            ? documentText[..24000]
            : documentText;

        var requirementText = new StringBuilder();

        foreach (var requirement in requirements)
        {
            requirementText.AppendLine(
                $"[{requirement.Code}] " +
                $"({requirement.Category}, " +
                $"mandatory {(requirement.IsMandatory ? "yes" : "no")}) " +
                requirement.Description);
        }

        var userPrompt =
            $"OPPORTUNITY: {opportunity.Name}\n" +
            $"INDUSTRY: {opportunity.Industry}\n\n" +
            $"EXTRACTED REQUIREMENTS:\n{requirementText}\n\n" +
            $"SOURCE DOCUMENT:\n{trimmedText}\n\n" +
            $"Prepare the clarification questions.";

        var requestBody = new Dictionary<string, object>
        {
            ["messages"] = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = userPrompt }
            },
            ["max_completion_tokens"] = 10000,
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
                "The clarification response was empty. Increase " +
                "max_completion_tokens or shorten the document.");
        }

        return ParseClarifications(content);
    }

    private List<GeneratedClarification> ParseClarifications(
        string content)
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
                "clarifications",
                out var array))
        {
            return new List<GeneratedClarification>();
        }

        var results = new List<GeneratedClarification>();

        foreach (var item in array.EnumerateArray())
        {
            var question = ReadString(item, "question");

            if (string.IsNullOrWhiteSpace(question))
            {
                continue;
            }

            results.Add(new GeneratedClarification
            {
                Reference = ReadString(item, "reference"),
                Question = question,
                Rationale = ReadString(item, "rationale"),
                Category = ReadString(item, "category"),
                Impact = ReadString(item, "impact"),
                RelatedRequirements =
                    ReadString(item, "relatedRequirements"),
                SourceReference =
                    ReadString(item, "sourceReference")
            });
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

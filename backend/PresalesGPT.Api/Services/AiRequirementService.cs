using System.Text;
using System.Text.Json;

namespace PresalesGPT.Api.Services;

public class ExtractedRequirement
{
    public string Code { get; set; } = "";

    public string Description { get; set; } = "";

    public string Category { get; set; } = "";

    public string Priority { get; set; } = "Medium";

    public bool IsMandatory { get; set; }

    public string SourceReference { get; set; } = "";
}

public class AiRequirementService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public AiRequirementService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    private const string SystemPrompt = """
        You are a presales requirement analyst. Extract requirements from the
        customer document.

        WHAT COUNTS AS A REQUIREMENT
        A requirement is a statement that obliges the supplier to deliver,
        build, provide or comply with something in the proposed solution or
        the delivery of that solution.

        DO NOT EXTRACT the following, even though they appear in the document:
        - Project timeline tables, milestone schedules or target completion weeks
        - Evaluation criteria and their weightings
        - Bid submission logistics, for example submission deadlines, file
          formats, sealed envelopes, number of references to attach, or CVs to
          include
        - Background, introduction and context narrative
        - Contact details, document references and revision history

        If a statement only describes WHEN the bid is due or HOW the bid must be
        packaged, it is not a requirement. Leave it out.

        MANDATORY FLAG
        Set isMandatory to true ONLY when the document explicitly marks that
        specific requirement as mandatory, for example by stating "this is a
        mandatory requirement", "mandatory", "must comply", "shall", or by
        listing it under a heading of mandatory requirements.
        If the document uses softer wording such as "should", "may", "is
        desirable", "preferred", or gives no indication at all, set isMandatory
        to false.
        Do not infer mandatory status from how important the requirement sounds.
        When in doubt, set it to false.

        PRIORITY
        High means the document states it is mandatory or critical.
        Medium is the default.
        Low means the document describes it as optional, desirable or nice to
        have.

        CODES
        If the document already uses requirement identifiers such as BR-01,
        TR-04 or SR-02, reuse that exact identifier as the code. Only generate
        REQ-001 style codes when the document has no identifiers of its own.

        GROUNDING
        Every requirement must come from text actually present in the document.
        Never invent a requirement. Never merge two unrelated requirements into
        one. Keep the description close to the original wording.
        Set sourceReference to the page marker shown in the text, for example
        "Page 3".

        OUTPUT
        Return ONLY valid JSON. No markdown, no commentary.
        {"requirements":[{"code":"TR-01","description":"...",
        "category":"Business|Technical|Security|Compliance|Commercial",
        "priority":"High|Medium|Low","isMandatory":true,
        "sourceReference":"Page 2"}]}
        If no requirements are found, return {"requirements":[]}.
        """;

    public async Task<List<ExtractedRequirement>> ExtractAsync(
        string documentText)
    {
        var endpoint =
            _configuration["AzureOpenAI:Endpoint"];

        var apiKey =
            _configuration["AzureOpenAI:ApiKey"];

        var deployment =
            _configuration["AzureOpenAI:Deployment"];

        var apiVersion =
            _configuration["AzureOpenAI:ApiVersion"];

        if (string.IsNullOrWhiteSpace(endpoint) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(deployment))
        {
            throw new InvalidOperationException(
                "Azure OpenAI settings are missing in appsettings.json.");
        }

        var trimmedText = documentText.Length > 30000
            ? documentText[..30000]
            : documentText;

        var requestBody = new Dictionary<string, object>
        {
            ["messages"] = new object[]
            {
                new
                {
                    role = "system",
                    content = SystemPrompt
                },
                new
                {
                    role = "user",
                    content =
                        "Extract requirements from this document:\n\n" +
                        trimmedText
                }
            },
            ["max_completion_tokens"] = 8000,
            ["response_format"] = new { type = "json_object" }
        };

        var url =
            $"{endpoint.TrimEnd('/')}/openai/deployments/" +
            $"{deployment}/chat/completions?api-version={apiVersion}";

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            url);

        request.Headers.Add("api-key", apiKey);

        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response =
            await _httpClient.SendAsync(request);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Azure OpenAI call failed " +
                $"({(int)response.StatusCode}): {responseBody}");
        }

        using var document =
            JsonDocument.Parse(responseBody);

        var choices = document
            .RootElement
            .GetProperty("choices");

        if (choices.GetArrayLength() == 0)
        {
            return new List<ExtractedRequirement>();
        }

        var message = choices[0].GetProperty("message");

        if (!message.TryGetProperty("content", out var contentElement))
        {
            return new List<ExtractedRequirement>();
        }

        var content = contentElement.GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                "Model ne khaali response bheja. " +
                "max_completion_tokens badhao ya document chhota karo.");
        }

        return ParseRequirements(content);
    }

    private List<ExtractedRequirement> ParseRequirements(
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
                "requirements",
                out var array))
        {
            return new List<ExtractedRequirement>();
        }

        var results = new List<ExtractedRequirement>();

        foreach (var item in array.EnumerateArray())
        {
            results.Add(new ExtractedRequirement
            {
                Code = ReadString(item, "code"),
                Description = ReadString(item, "description"),
                Category = ReadString(item, "category"),
                Priority = ReadString(item, "priority"),
                IsMandatory = ReadBool(item, "isMandatory"),
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
        if (element.TryGetProperty(
                propertyName,
                out var value) &&
            value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? "";
        }

        return "";
    }

    private bool ReadBool(
        JsonElement element,
        string propertyName)
    {
        if (element.TryGetProperty(
                propertyName,
                out var value))
        {
            if (value.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (value.ValueKind == JsonValueKind.False)
            {
                return false;
            }
        }

        return false;
    }
}

using System.Text;
using System.Text.Json;
using PresalesGPT.Api.Models;

namespace PresalesGPT.Api.Services;

public class SectionDefinition
{
    public int Order { get; set; }

    public string Name { get; set; } = "";

    public string Instruction { get; set; } = "";
}

public class GeneratedSection
{
    public string Content { get; set; } = "";

    public string CoveredRequirements { get; set; } = "";

    public string Assumptions { get; set; } = "";
}

public class ProposalService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ProposalService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public static readonly List<SectionDefinition> Sections =
        new()
        {
            new SectionDefinition
            {
                Order = 1,
                Name = "Executive Summary",
                Instruction =
                    "Write a concise executive summary for senior " +
                    "stakeholders. State the customer's situation, the " +
                    "outcome they are seeking, and the shape of the " +
                    "proposed engagement. Do not list technology product " +
                    "names in detail here. Around 200 words."
            },
            new SectionDefinition
            {
                Order = 2,
                Name = "Understanding of Requirements",
                Instruction =
                    "Demonstrate that the requirements have been " +
                    "understood. Group them by theme and restate the " +
                    "customer's needs in your own words. Reference the " +
                    "requirement codes. Do not propose solutions here."
            },
            new SectionDefinition
            {
                Order = 3,
                Name = "Proposed Solution",
                Instruction =
                    "Describe the proposed solution approach. Map the " +
                    "approach to the requirement codes it addresses. " +
                    "Where a specific technology is implied by the " +
                    "requirements, you may name it. Where it is not, " +
                    "describe the capability instead of naming a product."
            },
            new SectionDefinition
            {
                Order = 4,
                Name = "Scope of Work",
                Instruction =
                    "Define what is included in scope. Use clear " +
                    "statements of work derived only from the " +
                    "requirements provided. Be specific about " +
                    "deliverables."
            },
            new SectionDefinition
            {
                Order = 5,
                Name = "Delivery Approach",
                Instruction =
                    "Describe the delivery methodology, phases and " +
                    "governance. Do not invent specific dates or " +
                    "durations unless they appear in the requirements."
            },
            new SectionDefinition
            {
                Order = 6,
                Name = "Assumptions and Exclusions",
                Instruction =
                    "List assumptions the proposal depends on, and " +
                    "explicitly list what is excluded from scope. Be " +
                    "honest about gaps where the requirements are silent."
            },
            new SectionDefinition
            {
                Order = 7,
                Name = "Risks and Mitigations",
                Instruction =
                    "Identify delivery risks that arise from the stated " +
                    "requirements and describe a mitigation for each. " +
                    "Only raise risks that are grounded in the " +
                    "requirements provided. Do not list every " +
                    "requirement as a separate risk. Raise a risk only " +
                    "where there is a credible delivery threat, and say " +
                    "so plainly rather than restating the requirement."
            }
        };

    private const string SystemPrompt = """
        You are a senior presales consultant writing a section of a customer
        proposal.

        CUSTOMER IDENTITY
        The customer name is supplied to you as CUSTOMER. Treat it as a label
        only. If the requirements themselves refer to a different organisation
        name, the requirements are authoritative and the supplied label is
        likely to be wrong.
        When the two disagree, do not name either organisation. Write "you"
        and "your organisation" throughout instead. Never merge the two names
        and never invent a third.
        Never introduce any company, brand, product, or organisation name that
        does not appear in the requirements or in the CUSTOMER field.

        GROUNDING RULES
        Write only what the supplied requirements support. You must not invent
        any of the following:
        - Prices, rates, day counts or effort estimates
        - Specific dates, durations or timelines
        - Team sizes or named individuals
        - Client references, case studies or past project names
        - Certifications, partnership tiers or accreditations
        - Performance guarantees or service level numbers
        - Product names that the requirements do not imply

        If the section would normally contain such information and you do not
        have it, write a clearly marked placeholder such as
        [Commercial team to confirm] or [Delivery lead to confirm] instead of
        inventing a value.

        CONFIDENTIALITY OF INTERNAL METADATA
        The requirement metadata supplied to you, namely the mandatory flag,
        the priority and the category, is our internal working classification.
        It must never appear in the proposal text in any form.
        Never write phrases such as "this requirement is classified as
        non-mandatory", "although marked as non-mandatory", "this is a high
        priority requirement" or "categorised as technical".
        Use the metadata only to decide how much emphasis to give a topic. The
        customer must never see how we have internally triaged their
        requirements.
        You may refer to something as mandatory only when the customer's own
        requirement wording states it, not because our metadata says so.

        STYLE
        Write in professional British English prose. Address the customer as
        "you" and the supplier as "we". Avoid marketing superlatives such as
        world-class, cutting-edge, best-in-class or industry leading. Be
        specific and factual rather than promotional.
        Use short paragraphs. Use bullet points only where a list is genuinely
        clearer than prose.

        TRACEABILITY
        In coveredRequirements, list only the requirement codes this section
        genuinely addresses in its own text, comma separated. Do not list a
        code unless the section actually discusses it. Claiming broad coverage
        you have not written is worse than claiming none.
        In assumptions, list any assumption you had to make, one per line. If
        you made none, return an empty string.

        OUTPUT
        Return ONLY valid JSON, no markdown fences:
        {"content":"...","coveredRequirements":"TR-01, TR-04",
        "assumptions":"..."}
        """;

    public async Task<GeneratedSection> GenerateSectionAsync(
        SectionDefinition section,
        Opportunity opportunity,
        List<Requirement> requirements)
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

        var requirementText = new StringBuilder();

        foreach (var requirement in requirements)
        {
            requirementText.AppendLine(
                $"[{requirement.Code}] " +
                $"(internal metadata - category {requirement.Category}, " +
                $"priority {requirement.Priority}, " +
                $"mandatory {(requirement.IsMandatory ? "yes" : "no")}) " +
                requirement.Description);
        }

        var userPrompt =
            $"CUSTOMER (label only, may be unreliable): " +
            $"{opportunity.CustomerName}\n" +
            $"OPPORTUNITY: {opportunity.Name}\n" +
            $"INDUSTRY: {opportunity.Industry}\n" +
            $"REGION: {opportunity.Region}\n\n" +
            $"APPROVED REQUIREMENTS:\n" +
            $"{requirementText}\n\n" +
            $"SECTION TO WRITE: {section.Name}\n" +
            $"INSTRUCTION: {section.Instruction}";

        var requestBody = new Dictionary<string, object>
        {
            ["messages"] = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = userPrompt }
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

        using var parsedResponse =
            JsonDocument.Parse(responseBody);

        var choices = parsedResponse
            .RootElement
            .GetProperty("choices");

        if (choices.GetArrayLength() == 0)
        {
            throw new InvalidOperationException(
                "Model ne koi response nahi diya.");
        }

        var message = choices[0].GetProperty("message");

        if (!message.TryGetProperty("content", out var contentElement))
        {
            throw new InvalidOperationException(
                "Model response me content nahi mila.");
        }

        var content = contentElement.GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                $"Section '{section.Name}' ke liye khaali response aaya.");
        }

        return ParseSection(content);
    }

    private GeneratedSection ParseSection(string content)
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

        var root = parsed.RootElement;

        return new GeneratedSection
        {
            Content = ReadString(root, "content"),
            CoveredRequirements =
                ReadString(root, "coveredRequirements"),
            Assumptions = ReadString(root, "assumptions")
        };
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
}

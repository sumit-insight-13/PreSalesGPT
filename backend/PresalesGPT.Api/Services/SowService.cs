using System.Text;
using System.Text.Json;
using PresalesGPT.Api.Models;

namespace PresalesGPT.Api.Services;

public class SowSectionDefinition
{
    public int Order { get; set; }

    public string Name { get; set; } = "";

    public string Instruction { get; set; } = "";
}

public class GeneratedSowSection
{
    public string Content { get; set; } = "";

    public string CoveredRequirements { get; set; } = "";
}

public class SowService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public SowService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public static readonly List<SowSectionDefinition> Sections =
        new()
        {
            new SowSectionDefinition
            {
                Order = 1,
                Name = "Engagement Overview",
                Instruction =
                    "State what this engagement covers in two short " +
                    "paragraphs. Identify the parties as 'the Supplier' " +
                    "and 'the Customer'. Do not repeat the sales " +
                    "narrative from the proposal."
            },
            new SowSectionDefinition
            {
                Order = 2,
                Name = "Scope of Services",
                Instruction =
                    "Define precisely what the Supplier will perform. " +
                    "Use numbered service items. Each item must be a " +
                    "verifiable obligation, not an intention. Write " +
                    "'the Supplier shall' rather than 'we will'."
            },
            new SowSectionDefinition
            {
                Order = 3,
                Name = "Deliverables",
                Instruction =
                    "List each deliverable as a numbered item with a " +
                    "short description and its format, for example a " +
                    "document, a report, a configured environment or " +
                    "source code. Only list deliverables the approved " +
                    "proposal commits to."
            },
            new SowSectionDefinition
            {
                Order = 4,
                Name = "Acceptance Criteria",
                Instruction =
                    "Define how each deliverable is accepted, who " +
                    "accepts it, and what happens if acceptance is " +
                    "withheld. Where the proposal does not state a " +
                    "review period or acceptance authority, use a " +
                    "placeholder rather than inventing one."
            },
            new SowSectionDefinition
            {
                Order = 5,
                Name = "Roles and Responsibilities",
                Instruction =
                    "Set out Supplier responsibilities and Customer " +
                    "responsibilities in two separate lists. Customer " +
                    "responsibilities must reflect the dependencies " +
                    "stated in the approved proposal's assumptions."
            },
            new SowSectionDefinition
            {
                Order = 6,
                Name = "Assumptions and Dependencies",
                Instruction =
                    "Restate the assumptions from the approved proposal " +
                    "in contractual language, and state the consequence " +
                    "if an assumption proves incorrect. Do not add new " +
                    "assumptions that the proposal does not contain."
            },
            new SowSectionDefinition
            {
                Order = 7,
                Name = "Out of Scope",
                Instruction =
                    "List work explicitly excluded. Only list exclusions " +
                    "the approved proposal states. If the proposal " +
                    "records unresolved scope boundaries rather than " +
                    "agreed exclusions, say so plainly and mark them for " +
                    "confirmation rather than converting them into " +
                    "exclusions."
            },
            new SowSectionDefinition
            {
                Order = 8,
                Name = "Change Control",
                Instruction =
                    "Describe how scope changes are raised, assessed, " +
                    "priced and authorised, and state that work outside " +
                    "the agreed scope is not performed until authorised " +
                    "in writing."
            },
            new SowSectionDefinition
            {
                Order = 9,
                Name = "Commercial Terms",
                Instruction =
                    "State the pricing basis, invoicing trigger and " +
                    "payment conditions exactly as the approved proposal " +
                    "describes them. Every monetary amount, rate and " +
                    "date must be a placeholder unless the proposal " +
                    "states it explicitly."
            }
        };

    private const string SystemPrompt = """
        You are a contracts specialist drafting a Statement of Work from an
        already approved proposal.

        WHAT THIS DOCUMENT IS
        A Statement of Work is contractual. Every sentence may be enforced.
        This is not a sales document. Do not persuade, do not market, do not
        restate benefits. State obligations.

        SOURCE OF TRUTH
        The approved proposal text is supplied to you. It is the only source
        of scope. You must not introduce any obligation, deliverable,
        exclusion or term that the approved proposal does not support.
        If the proposal is silent on something a Statement of Work would
        normally cover, write a placeholder and move on. Silence in the
        proposal is not permission to invent.




        PRODUCT AND VENDOR NAMES
        Where the source names a specific product, vendor or system, carry that
        name through exactly. Removing a vendor name weakens specificity.
        "11 Oracle databases" must not become "11 databases".
        "26 Microsoft SQL Server instances" must not become "26 database
        instances".
        The rule against inventing product names applies only to names that do
        not appear in the source. It is not a reason to strip names that do.

        ABSOLUTE PROHIBITIONS
        Never state any of the following unless the approved proposal states
        it explicitly:
        - Any monetary amount, rate, day count or effort figure
        - Any date, duration, notice period or review window
        - Any named individual, role holder or team size
        - Any service level, uptime figure or performance guarantee
        - Any liability cap, warranty period, indemnity or penalty
        - Any governing law or jurisdiction
        For each of these, write a placeholder such as
        [Commercial team to confirm], [Legal to confirm] or
        [Delivery lead to confirm].

        CUSTOMER IDENTITY
        The customer name supplied to you is a label and may be unreliable.
        Refer to the parties as "the Supplier" and "the Customer" throughout.
        Do not name any organisation.

        LANGUAGE
        Use formal British English. Use "shall" for obligations. Use
        numbered items for services, deliverables and responsibilities.
        Be specific and testable: an obligation a reader cannot verify is a
        defect in this document.
        Do not use "we", "you", "our" or "your".

        TRACEABILITY
        In coveredRequirements, list the requirement codes this section
        genuinely implements, comma separated. Do not list a code the
        section does not address.

        OUTPUT
        Return ONLY valid JSON, no markdown fences:
        {"content":"...","coveredRequirements":"TR-01, SR-02"}
        """;

    public async Task<GeneratedSowSection> GenerateSectionAsync(
        SowSectionDefinition section,
        Opportunity opportunity,
        List<Requirement> requirements,
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

        var requirementText = new StringBuilder();

        foreach (var requirement in requirements)
        {
            requirementText.AppendLine(
                $"[{requirement.Code}] {requirement.Description}");
        }

        var proposalText = new StringBuilder();

        foreach (var proposalSection in approvedProposal
                     .OrderBy(s => s.SectionOrder))
        {
            proposalText.AppendLine(
                $"### {proposalSection.SectionName}");
            proposalText.AppendLine(proposalSection.Content);
            proposalText.AppendLine();
        }

        var userPrompt =
            $"OPPORTUNITY: {opportunity.Name}\n" +
            $"INDUSTRY: {opportunity.Industry}\n\n" +
            $"APPROVED REQUIREMENTS:\n{requirementText}\n\n" +
            $"APPROVED PROPOSAL (the only source of scope):\n" +
            $"{proposalText}\n\n" +
            $"SOW SECTION TO DRAFT: {section.Name}\n" +
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
                $"Section '{section.Name}' returned an empty response.");
        }

        return ParseSection(content);
    }

    private GeneratedSowSection ParseSection(string content)
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

        return new GeneratedSowSection
        {
            Content = ReadString(root, "content"),
            CoveredRequirements =
                ReadString(root, "coveredRequirements")
        };
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

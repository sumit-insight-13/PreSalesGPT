using System.Text.RegularExpressions;
using PresalesGPT.Api.Models;

namespace PresalesGPT.Api.Services;

public class RequirementCoverage
{
    public string Code { get; set; } = "";

    public string Description { get; set; } = "";

    public string Category { get; set; } = "";

    public string Priority { get; set; } = "";

    public bool IsMandatory { get; set; }

    public bool IsCovered { get; set; }

    public bool IsClaimedOnly { get; set; }

    public List<string> CoveredInSections { get; set; } = new();
}

public class SectionCoverage
{
    public int SectionOrder { get; set; }

    public string SectionName { get; set; } = "";

    public bool IsApproved { get; set; }

    public int RequirementsCovered { get; set; }

    public int PlaceholderCount { get; set; }

    public List<string> Placeholders { get; set; } = new();

    public List<string> UnknownCodes { get; set; } = new();
}

public class CustomerNameCheck
{
    public string OpportunityCustomerName { get; set; } = "";

    public bool FoundInSourceDocuments { get; set; }

    public bool AppearsInProposal { get; set; }

    public bool IsMismatch { get; set; }

    public List<string> SectionsNamingCustomer { get; set; } = new();
}

public class CoverageReport
{
    public int TotalApproved { get; set; }

    public int CoveredCount { get; set; }

    public int UncoveredCount { get; set; }

    public int UncoveredMandatoryCount { get; set; }

    public int CoveragePercent { get; set; }

    public int TotalPlaceholders { get; set; }

    public int SectionsAwaitingApproval { get; set; }

    public bool IsReadyForRelease { get; set; }

    public CustomerNameCheck CustomerName { get; set; } = new();

    public List<string> Blockers { get; set; } = new();

    public List<RequirementCoverage> Requirements { get; set; } = new();

    public List<SectionCoverage> Sections { get; set; } = new();
}

public class CoverageService
{
    private static readonly Regex PlaceholderPattern =
        new(@"\[[^\[\]]{3,120}\]", RegexOptions.Compiled);

    private static readonly HashSet<string> CompanySuffixes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "limited", "ltd", "plc", "inc", "incorporated",
            "corporation", "corp", "company", "co",
            "group", "holdings", "gmbh", "pvt", "private",
            "llc", "llp", "the", "and"
        };

    public CoverageReport Build(
        List<Requirement> approvedRequirements,
        List<ProposalSection> sections,
        string sourceDocumentText = "",
        string customerName = "")
    {
        var report = new CoverageReport
        {
            TotalApproved = approvedRequirements.Count
        };

        report.CustomerName.OpportunityCustomerName =
            customerName ?? "";

        var knownCodes = approvedRequirements
            .Select(r => r.Code)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        BuildSectionCoverage(report, sections, knownCodes);

        BuildRequirementCoverage(
            report,
            approvedRequirements,
            sections);

        ComputeTotals(report);

        BuildBlockers(report);

        BuildCustomerNameCheck(
            report,
            sections,
            sourceDocumentText);

        report.IsReadyForRelease = report.Blockers.Count == 0;

        return report;
    }

    private void BuildSectionCoverage(
        CoverageReport report,
        List<ProposalSection> sections,
        HashSet<string> knownCodes)
    {
        foreach (var section in sections.OrderBy(s => s.SectionOrder))
        {
            var claimed = SplitCodes(section.CoveredRequirements);

            var placeholders = PlaceholderPattern
                .Matches(section.Content ?? "")
                .Select(m => m.Value)
                .Distinct()
                .ToList();

            var unknown = claimed
                .Where(c => !knownCodes.Contains(c))
                .ToList();

            report.Sections.Add(new SectionCoverage
            {
                SectionOrder = section.SectionOrder,
                SectionName = section.SectionName,
                IsApproved = section.IsApproved,
                RequirementsCovered = claimed.Count,
                PlaceholderCount = placeholders.Count,
                Placeholders = placeholders,
                UnknownCodes = unknown
            });
        }
    }

    private void BuildRequirementCoverage(
        CoverageReport report,
        List<Requirement> approvedRequirements,
        List<ProposalSection> sections)
    {
        foreach (var requirement in approvedRequirements
                     .OrderBy(r => r.Code))
        {
            var coverage = new RequirementCoverage
            {
                Code = requirement.Code,
                Description = requirement.Description,
                Category = requirement.Category,
                Priority = requirement.Priority,
                IsMandatory = requirement.IsMandatory
            };

            var claimedIn = new List<string>();
            var mentionedIn = new List<string>();

            foreach (var section in sections)
            {
                var claimed = SplitCodes(section.CoveredRequirements);

                if (claimed.Contains(
                        requirement.Code,
                        StringComparer.OrdinalIgnoreCase))
                {
                    claimedIn.Add(section.SectionName);
                }

                if (ContainsWholeWord(
                        section.Content ?? "",
                        requirement.Code))
                {
                    mentionedIn.Add(section.SectionName);
                }
            }

            coverage.CoveredInSections = mentionedIn.Count > 0
                ? mentionedIn
                : claimedIn;

            coverage.IsCovered =
                claimedIn.Count > 0 || mentionedIn.Count > 0;

            coverage.IsClaimedOnly =
                claimedIn.Count > 0 && mentionedIn.Count == 0;

            report.Requirements.Add(coverage);
        }
    }

    private void ComputeTotals(CoverageReport report)
    {
        report.CoveredCount =
            report.Requirements.Count(r => r.IsCovered);

        report.UncoveredCount =
            report.Requirements.Count(r => !r.IsCovered);

        report.UncoveredMandatoryCount =
            report.Requirements.Count(
                r => !r.IsCovered && r.IsMandatory);

        report.CoveragePercent = report.TotalApproved == 0
            ? 0
            : (int)Math.Round(
                report.CoveredCount * 100.0 / report.TotalApproved);

        report.TotalPlaceholders =
            report.Sections.Sum(s => s.PlaceholderCount);

        report.SectionsAwaitingApproval =
            report.Sections.Count(s => !s.IsApproved);
    }

    private void BuildBlockers(CoverageReport report)
    {
        if (report.UncoveredMandatoryCount > 0)
        {
            report.Blockers.Add(
                $"{report.UncoveredMandatoryCount} mandatory " +
                $"requirement(s) are not addressed anywhere in the " +
                $"proposal.");
        }

        var otherUncovered =
            report.UncoveredCount - report.UncoveredMandatoryCount;

        if (otherUncovered > 0)
        {
            report.Blockers.Add(
                $"{otherUncovered} non-mandatory requirement(s) are not " +
                $"addressed anywhere in the proposal.");
        }

        if (report.TotalPlaceholders > 0)
        {
            report.Blockers.Add(
                $"{report.TotalPlaceholders} unresolved placeholder(s) " +
                $"still need input from the delivery or commercial team.");
        }

        if (report.SectionsAwaitingApproval > 0)
        {
            report.Blockers.Add(
                $"{report.SectionsAwaitingApproval} section(s) have not " +
                $"been reviewed and approved.");
        }

        var claimedOnly =
            report.Requirements.Count(r => r.IsClaimedOnly);

        if (claimedOnly > 0)
        {
            report.Blockers.Add(
                $"{claimedOnly} requirement(s) are listed as covered but " +
                $"their code does not appear in any section text. Verify " +
                $"these are genuinely addressed.");
        }

        var unknownTotal =
            report.Sections.Sum(s => s.UnknownCodes.Count);

        if (unknownTotal > 0)
        {
            report.Blockers.Add(
                $"{unknownTotal} requirement code(s) referenced by the " +
                $"proposal do not match any approved requirement.");
        }
    }

    private void BuildCustomerNameCheck(
        CoverageReport report,
        List<ProposalSection> sections,
        string sourceDocumentText)
    {
        var check = report.CustomerName;

        var name = check.OpportunityCustomerName.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var hasSource = !string.IsNullOrWhiteSpace(sourceDocumentText);

        check.FoundInSourceDocuments =
            hasSource && NameAppearsIn(sourceDocumentText, name);

        foreach (var section in sections.OrderBy(s => s.SectionOrder))
        {
            if (NameAppearsIn(section.Content ?? "", name))
            {
                check.SectionsNamingCustomer.Add(section.SectionName);
            }
        }

        check.AppearsInProposal =
            check.SectionsNamingCustomer.Count > 0;

        if (!hasSource || check.FoundInSourceDocuments)
        {
            return;
        }

        check.IsMismatch = check.AppearsInProposal;

        if (check.IsMismatch)
        {
            report.Blockers.Add(
                $"The customer name '{name}' appears in " +
                $"{check.SectionsNamingCustomer.Count} proposal " +
                $"section(s) but does not appear anywhere in the source " +
                $"documents. Confirm the proposal names the correct " +
                $"organisation before release.");
        }
        else
        {
            report.Blockers.Add(
                $"The customer name '{name}' on this opportunity does " +
                $"not appear in the source documents. Confirm the " +
                $"correct document has been uploaded.");
        }
    }

    private static bool NameAppearsIn(string text, string name)
    {
        if (string.IsNullOrWhiteSpace(text) ||
            string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        if (ContainsWholeWord(text, name))
        {
            return true;
        }

        var tokens = Regex
            .Split(name, @"[^A-Za-z0-9]+")
            .Where(t => t.Length > 2)
            .Where(t => !CompanySuffixes.Contains(t))
            .ToList();

        if (tokens.Count == 0)
        {
            return false;
        }

        return tokens.All(token => ContainsWholeWord(text, token));
    }

    private static bool ContainsWholeWord(string text, string term)
    {
        if (string.IsNullOrWhiteSpace(text) ||
            string.IsNullOrWhiteSpace(term))
        {
            return false;
        }

        var pattern =
            $@"(?<![A-Za-z0-9]){Regex.Escape(term)}(?![A-Za-z0-9])";

        return Regex.IsMatch(
            text,
            pattern,
            RegexOptions.IgnoreCase);
    }

    private static List<string> SplitCodes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new List<string>();
        }

        return value
            .Split(new[] { ',', ';', '|' },
                StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim())
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

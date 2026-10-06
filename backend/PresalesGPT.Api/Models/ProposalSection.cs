namespace PresalesGPT.Api.Models;

public class ProposalSection
{
    public int Id { get; set; }

    public int OpportunityId { get; set; }

    public int SectionOrder { get; set; }

    public string SectionName { get; set; } = "";

    public string Content { get; set; } = "";

    public string CoveredRequirements { get; set; } = "";

    public string Assumptions { get; set; } = "";

    public bool IsApproved { get; set; }

    public bool IsEdited { get; set; }

    public DateTime GeneratedOn { get; set; }

    public DateTime? UpdatedOn { get; set; }
}

namespace PresalesGPT.Api.Models;

public class Clarification
{
    public int Id { get; set; }

    public int OpportunityId { get; set; }

    public string Reference { get; set; } = "";

    public string Question { get; set; } = "";

    /// <summary>
    /// Why the answer matters to the response. This is internal and is
    /// never exported to the customer.
    /// </summary>
    public string Rationale { get; set; } = "";

    /// <summary>
    /// Ambiguity, Gap, Conflict, Feasibility, Commercial or Compliance.
    /// </summary>
    public string Category { get; set; } = "";

    /// <summary>
    /// Blocking, Important or Minor. A blocking question is one the
    /// response cannot be completed without.
    /// </summary>
    public string Impact { get; set; } = "Important";

    public string RelatedRequirements { get; set; } = "";

    public string SourceReference { get; set; } = "";

    public bool IsApproved { get; set; }

    public bool IsEdited { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime? UpdatedOn { get; set; }
}

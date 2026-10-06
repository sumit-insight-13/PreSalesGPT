namespace PresalesGPT.Api.Models;

public class Requirement
{
    public int Id { get; set; }

    public int OpportunityId { get; set; }

    public int DocumentId { get; set; }

    public string Code { get; set; } = "";

    public string Description { get; set; } = "";

    public string Category { get; set; } = "";

    public string Priority { get; set; } = "Medium";

    public bool IsMandatory { get; set; }

    public string SourceReference { get; set; } = "";

    public bool IsApproved { get; set; }

    public DateTime CreatedOn { get; set; }
}

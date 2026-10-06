namespace PresalesGPT.Api.Models;

public class Opportunity
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string CustomerName { get; set; } = "";

    public string Industry { get; set; } = "";

    public string Region { get; set; } = "";

    public decimal EstimatedValue { get; set; }

    public DateTime CreatedOn { get; set; }
}
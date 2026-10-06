namespace PresalesGPT.Api.Models;

public class DeckSlide
{
    public int Id { get; set; }

    public int OpportunityId { get; set; }

    public int SlideOrder { get; set; }

    /// <summary>
    /// Bullets, Stats, TwoColumn or Callout. Chosen by the model to suit
    /// the content rather than applied uniformly.
    /// </summary>
    public string Layout { get; set; } = "Bullets";

    public string Title { get; set; } = "";

    /// <summary>
    /// One bullet per line. Used by the Bullets layout.
    /// </summary>
    public string Bullets { get; set; } = "";

    /// <summary>
    /// JSON payload for the non-bullet layouts. Kept as a single column
    /// because these shapes are never queried independently and the set
    /// of layouts is expected to keep changing.
    /// </summary>
    public string LayoutData { get; set; } = "";

    public string SpeakerNotes { get; set; } = "";

    public string SourceSection { get; set; } = "";

    public DateTime GeneratedOn { get; set; }
}

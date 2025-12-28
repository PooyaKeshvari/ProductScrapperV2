namespace ProductScrapperV2.Domain.Entities;

public class Competitor
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public bool IsAutoDiscovered { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<PriceRecord> PriceRecords { get; set; } = new List<PriceRecord>();
    public ICollection<CompetitorSuggestion> Suggestions { get; set; } = new List<CompetitorSuggestion>();
}

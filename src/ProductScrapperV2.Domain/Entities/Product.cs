namespace ProductScrapperV2.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public decimal OwnPrice { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<PriceRecord> PriceRecords { get; set; } = new List<PriceRecord>();
    public ICollection<CompetitorSuggestion> Suggestions { get; set; } = new List<CompetitorSuggestion>();
}

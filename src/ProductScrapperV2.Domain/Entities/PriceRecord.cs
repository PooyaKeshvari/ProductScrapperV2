namespace ProductScrapperV2.Domain.Entities;

public class PriceRecord
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid CompetitorId { get; set; }

    public string ProductTitle { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "IRR";
    public decimal MatchPercentage { get; set; }
    public decimal ConfidenceScore { get; set; }
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;

    public Product? Product { get; set; }
    public Competitor? Competitor { get; set; }
}

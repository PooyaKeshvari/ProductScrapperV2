namespace ProductScrapperV2.Domain.Entities;

public class CompetitorSuggestion
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid CompetitorId { get; set; }
    public int SuggestedRank { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal CredibilityScore { get; set; }

    public Product? Product { get; set; }
    public Competitor? Competitor { get; set; }
}

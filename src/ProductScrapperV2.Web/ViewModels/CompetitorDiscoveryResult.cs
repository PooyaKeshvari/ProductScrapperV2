namespace ProductScrapperV2.Web.ViewModels;


public sealed class CompetitorDiscoveryResult
{
    public string CompetitorName { get; init; } = string.Empty;
    public string WebsiteUrl { get; init; } = string.Empty;
    public int SuggestedRank { get; init; }
    public double ConfidenceScore { get; init; }
}
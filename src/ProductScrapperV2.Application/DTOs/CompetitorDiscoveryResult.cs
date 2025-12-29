namespace ProductScrapperV2.Application.DTOs;

public record CompetitorDiscoveryResult(
    string CompetitorName,
    string WebsiteUrl,
    decimal CredibilityScore,
    int SuggestedRank,
    string Reason);

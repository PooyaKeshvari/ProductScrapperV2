namespace ProductScrapperV2.Application.DTOs;

public record ScrapeResultDto(
    string ProductTitle,
    string ProductUrl,
    decimal Price,
    decimal MatchPercentage,
    decimal ConfidenceScore);

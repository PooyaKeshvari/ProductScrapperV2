namespace ProductScrapperV2.Application.DTOs;

public record PriceComparisonDto(
    Guid ProductId,
    string ProductName,
    decimal OwnPrice,
    IReadOnlyCollection<CompetitorPriceDto> CompetitorPrices,
    CompetitorPriceDto? Cheapest,
    CompetitorPriceDto? MostExpensive);

public record CompetitorPriceDto(
    Guid CompetitorId,
    string CompetitorName,
    string CompetitorUrl,
    string ProductTitle,
    string ProductUrl,
    decimal Price,
    decimal MatchPercentage,
    decimal ConfidenceScore,
    DateTimeOffset CapturedAt);

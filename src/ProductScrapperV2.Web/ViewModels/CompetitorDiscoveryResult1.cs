namespace ProductScrapperV2.Web.ViewModels;

public record CompetitorDto(Guid Id, string Name, string WebsiteUrl, bool IsAutoDiscovered);


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

public record ProductDto(Guid Id, string Name, string? Sku, decimal OwnPrice);




public sealed class ScrapeResultDto
{
    public string ProductTitle { get; init; } = string.Empty;

    /// <summary>
    /// -1 = Out of stock
    ///  0 = Not detected
    /// >0 = Price
    /// </summary>
    public decimal Price { get; init; }
    public string ProductUrl { get; init; }

    public decimal MatchPercentage { get; init; }
    public decimal ConfidenceScore { get; init; }

    // اختیاری ولی مفید
    public bool IsOutOfStock => Price < 0;
}

namespace ProductScrapperV2.Web.ViewModels;

public sealed class DailyPriceHistoryDto
{
    public DateOnly Date { get; init; }

    public decimal MinPrice { get; init; }
    public decimal MaxPrice { get; init; }

    public IReadOnlyCollection<CompetitorPriceDto> Prices { get; init; } =
        Array.Empty<CompetitorPriceDto>();
}

public sealed class ProductPriceHistoryDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = "";
    public decimal OwnPrice { get; init; }

    public IReadOnlyCollection<DailyPriceHistoryDto> DailyHistory { get; init; }
        = Array.Empty<DailyPriceHistoryDto>();
}


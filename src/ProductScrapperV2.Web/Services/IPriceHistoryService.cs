using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using ProductScrapperV2.Web.ViewModels;
using ZennerDownlink.Data;

namespace ProductScrapperV2.Web.Services;

public interface IPriceHistoryService
{
    Task<ProductPriceHistoryDto> GetProductHistoryAsync(
        Guid productId,
        CancellationToken ct);
}

public sealed class PriceHistoryService : IPriceHistoryService
{
    private readonly AppDbContext _db;

    public PriceHistoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ProductPriceHistoryDto> GetProductHistoryAsync(
        Guid productId,
        CancellationToken ct)
    {
            var product = await _db.Products
          .FirstAsync(p => p.Id == productId, ct);

            var records = await _db.PriceRecords
                .Include(p => p.Competitor)
                .Where(p => p.ProductId == productId && p.Price > 0)
                .ToListAsync(ct);

            var grouped = records
                .GroupBy(r => DateOnly.FromDateTime(r.CapturedAt.DateTime))
                .OrderByDescending(g => g.Key)
                .Select(g => new DailyPriceHistoryDto
                {
                    Date = g.Key,
                    MinPrice = g.Min(x => x.Price),
                    MaxPrice = g.Max(x => x.Price),
                    Prices = g.Select(r => new CompetitorPriceDto(
                    r.CompetitorId,
                    r.Competitor!.Name, // Extract base URL
                    r.Competitor!.WebsiteUrl, // Extract base URL
                        r.ProductTitle,
                        r.ProductUrl,
                        r.Price,
                        r.MatchPercentage,
                        r.ConfidenceScore,
                        r.CapturedAt
                    )).ToList()
                })
                .ToList();


        return new ProductPriceHistoryDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            OwnPrice = product.OwnPrice,
            DailyHistory = grouped
        };
    }
}

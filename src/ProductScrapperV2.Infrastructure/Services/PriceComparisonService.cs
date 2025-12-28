using Microsoft.EntityFrameworkCore;
using ProductScrapperV2.Application.DTOs;
using ProductScrapperV2.Application.Interfaces;
using ProductScrapperV2.Infrastructure.Data;

namespace ProductScrapperV2.Infrastructure.Services;

public class PriceComparisonService : IPriceComparisonService
{
    private readonly AppDbContext _dbContext;

    public PriceComparisonService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PriceComparisonDto> CompareSingleAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products.FirstAsync(p => p.Id == productId, cancellationToken);
        var records = await _dbContext.PriceRecords
            .Include(p => p.Competitor)
            .Where(p => p.ProductId == productId)
            .OrderBy(p => p.Price)
            .ToListAsync(cancellationToken);

        var competitorPrices = records.Select(record => new CompetitorPriceDto(
            record.CompetitorId,
            record.Competitor?.Name ?? string.Empty,
            record.Competitor?.WebsiteUrl ?? string.Empty,
            record.ProductTitle,
            record.ProductUrl,
            record.Price,
            record.MatchPercentage,
            record.ConfidenceScore,
            record.CapturedAt)).ToList();

        var cheapest = competitorPrices.OrderBy(p => p.Price).FirstOrDefault();
        var mostExpensive = competitorPrices.OrderByDescending(p => p.Price).FirstOrDefault();

        return new PriceComparisonDto(
            product.Id,
            product.Name,
            product.OwnPrice,
            competitorPrices,
            cheapest,
            mostExpensive);
    }

    public async Task<IReadOnlyCollection<PriceComparisonDto>> CompareBulkAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var result = new List<PriceComparisonDto>();
        foreach (var productId in productIds)
        {
            result.Add(await CompareSingleAsync(productId, cancellationToken));
        }
        return result;
    }
}

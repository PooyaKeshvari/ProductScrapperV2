using ProductScrapperV2.Application.DTOs;

namespace ProductScrapperV2.Application.Interfaces;

public interface IPriceComparisonService
{
    Task<PriceComparisonDto> CompareSingleAsync(Guid productId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PriceComparisonDto>> CompareBulkAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);
}

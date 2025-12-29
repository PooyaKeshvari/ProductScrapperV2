using ProductScrapperV2.Application.DTOs;

namespace ProductScrapperV2.Application.Interfaces;

public interface IExcelExportService
{
    Task<byte[]> ExportPriceComparisonAsync(
        IReadOnlyCollection<PriceComparisonDto> comparisons,
        CancellationToken cancellationToken);
}

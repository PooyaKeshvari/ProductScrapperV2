using ProductScrapperV2.Application.DTOs;

namespace ProductScrapperV2.Application.Interfaces;

public interface ICompetitorDiscoveryService
{
    Task<IReadOnlyCollection<CompetitorDiscoveryResult>> DiscoverCompetitorsAsync(
        string productName,
        CancellationToken cancellationToken);
}

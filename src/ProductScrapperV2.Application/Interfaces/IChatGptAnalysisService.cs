using ProductScrapperV2.Application.DTOs;

namespace ProductScrapperV2.Application.Interfaces;

public interface IChatGptAnalysisService
{
    Task<ScrapeResultDto?> AnalyzeProductAsync(
        string productName,
        string pageUrl,
        IReadOnlyCollection<string> rawElements,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CompetitorDiscoveryResult>> AnalyzeCompetitorsAsync(
        string productName,
        IReadOnlyCollection<string> searchResults,
        CancellationToken cancellationToken);
}

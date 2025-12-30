namespace ProductScrapperV2.Application.Interfaces;

public interface IScrapingService
{
    Task<IReadOnlyCollection<string>> SearchGoogleAsync(string query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> ExtractRawElementsAsync(string url, CancellationToken cancellationToken);
}

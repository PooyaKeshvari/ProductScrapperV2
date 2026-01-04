namespace ProductScrapperV2.Web.Services;

public interface IScrapingService
{
    Task<IReadOnlyCollection<string>> SearchGoogleAsync(string query, CancellationToken ct);

    Task<IScrapingSession> CreateSessionAsync(CancellationToken ct);
}

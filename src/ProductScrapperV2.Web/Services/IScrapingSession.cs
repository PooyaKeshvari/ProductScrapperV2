using ProductScrapperV2.Web.ViewModels;

namespace ProductScrapperV2.Web.Services;

public interface IScrapingSession : IAsyncDisposable
{
    Task NavigateAsync(string url, CancellationToken ct);
    Task ClickAsync(string cssSelector, CancellationToken ct);
    Task<PageSnapshotDto> CaptureSnapshotAsync(CancellationToken ct);
}
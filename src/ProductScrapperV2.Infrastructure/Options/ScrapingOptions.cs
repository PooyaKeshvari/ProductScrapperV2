namespace ProductScrapperV2.Infrastructure.Options;

public class ScrapingOptions
{
    public int PageLoadTimeoutSeconds { get; set; } = 20;
    public int MaxRetryAttempts { get; set; } = 3;
    public int DelayBetweenJobsSeconds { get; set; } = 5;
}

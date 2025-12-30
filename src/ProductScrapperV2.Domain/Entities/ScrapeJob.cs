namespace ProductScrapperV2.Domain.Entities;

public class ScrapeJob
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Status { get; set; } = JobStatus.Pending;
    public int AttemptCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    public Product? Product { get; set; }
}

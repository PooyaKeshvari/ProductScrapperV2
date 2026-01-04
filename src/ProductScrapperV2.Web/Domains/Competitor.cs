using Microsoft.CodeAnalysis;

namespace ProductScrapperV2.Web.Domains;

public class Competitor
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public string? WebsiteHost { get; set; }
    public bool IsAutoDiscovered { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<PriceRecord> PriceRecords { get; set; } = new List<PriceRecord>();
    public ICollection<CompetitorSuggestion> Suggestions { get; set; } = new List<CompetitorSuggestion>();
}

public class CompetitorSuggestion
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid CompetitorId { get; set; }
    public int SuggestedRank { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal CredibilityScore { get; set; }

    public Product? Product { get; set; }
    public Competitor? Competitor { get; set; }
}

public static class JobStatus
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}

public class PriceRecord
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid CompetitorId { get; set; }

    public string ProductTitle { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "IRR";
    public decimal MatchPercentage { get; set; }
    public decimal ConfidenceScore { get; set; }
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;

    public Product? Product { get; set; }
    public Competitor? Competitor { get; set; }
}

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public decimal OwnPrice { get; set; }

    public decimal? AverageMarketPrice { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<PriceRecord> PriceRecords { get; set; } = new List<PriceRecord>();
    public ICollection<CompetitorSuggestion> Suggestions { get; set; } = new List<CompetitorSuggestion>();
}

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

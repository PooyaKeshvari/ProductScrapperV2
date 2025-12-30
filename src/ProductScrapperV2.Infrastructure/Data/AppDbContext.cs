using Microsoft.EntityFrameworkCore;
using ProductScrapperV2.Domain.Entities;

namespace ProductScrapperV2.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Competitor> Competitors => Set<Competitor>();
    public DbSet<PriceRecord> PriceRecords => Set<PriceRecord>();
    public DbSet<CompetitorSuggestion> CompetitorSuggestions => Set<CompetitorSuggestion>();
    public DbSet<ScrapeJob> ScrapeJobs => Set<ScrapeJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Name).HasMaxLength(300).IsRequired();
            entity.Property(p => p.Sku).HasMaxLength(100);
            entity.Property(p => p.OwnPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Competitor>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
            entity.Property(c => c.WebsiteUrl).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<PriceRecord>(entity =>
        {
            entity.Property(p => p.ProductTitle).HasMaxLength(300).IsRequired();
            entity.Property(p => p.ProductUrl).HasMaxLength(1000).IsRequired();
            entity.Property(p => p.Currency).HasMaxLength(10).IsRequired();
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.Property(p => p.MatchPercentage).HasPrecision(5, 2);
            entity.Property(p => p.ConfidenceScore).HasPrecision(5, 2);
        });

        modelBuilder.Entity<CompetitorSuggestion>(entity =>
        {
            entity.Property(c => c.Reason).HasMaxLength(500);
            entity.Property(c => c.CredibilityScore).HasPrecision(5, 2);
        });

        modelBuilder.Entity<ScrapeJob>(entity =>
        {
            entity.Property(j => j.Status).HasMaxLength(50).IsRequired();
            entity.Property(j => j.ErrorMessage).HasMaxLength(1000);
        });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductScrapperV2.Application.Interfaces;
using ProductScrapperV2.Infrastructure.Data;
using ProductScrapperV2.Infrastructure.Options;
using ProductScrapperV2.Infrastructure.Services;

namespace ProductScrapperV2.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenAiOptions>(configuration.GetSection("OpenAI"));
        services.Configure<ScrapingOptions>(configuration.GetSection("Scraping"));

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICompetitorDiscoveryService, CompetitorDiscoveryService>();
        services.AddScoped<IPriceComparisonService, PriceComparisonService>();
        services.AddScoped<IScrapingService, SeleniumScrapingService>();
        services.AddScoped<IExcelExportService, ExcelExportService>();

        services.AddHttpClient<IChatGptAnalysisService, OpenAiChatGptAnalysisService>((provider, client) =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHostedService<ScrapeWorker>();

        return services;
    }
}

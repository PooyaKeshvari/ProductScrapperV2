using Microsoft.EntityFrameworkCore;
using ProductScrapperV2.Web.Services;
using ZennerDownlink.Data;
using IScrapingService = ProductScrapperV2.Web.Services.IScrapingService;

namespace ProductScrapperV2.Web;


public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IScrapingService, SeleniumScrapingService>();
        services.AddScoped<IPriceComparisonService, PriceComparisonService>();
        services.AddScoped<IExcelExportService, ExcelExportService>();
        services.AddScoped<IPriceHistoryService, PriceHistoryService>();

        services.AddHttpClient<IChatGptAnalysisService, ChatGptAnalysisService>((provider, client) =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });


        services.AddSingleton<IScrapeCache, InMemoryScrapeCache>();



        services.AddHostedService<ScrapeWorker>();

        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium.Support.UI;
using ProductScrapperV2.Application.Interfaces;
using ProductScrapperV2.Infrastructure;
using ProductScrapperV2.Infrastructure.Data;
using ProductScrapperV2.Infrastructure.Options;
using ProductScrapperV2.Infrastructure.Services;
using IClock = ProductScrapperV2.Application.Interfaces.IClock;
using SystemClock = ProductScrapperV2.Infrastructure.Services.SystemClock;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddInfrastructure(builder.Configuration);

// Register DbContext once with transient error resiliency enabled
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptionsAction: sqlOptions =>
        {
            // Enable retry on failure for transient SQL errors.
            // Retry up to 5 times with a max delay of 30 seconds between retries.
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));

builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.Configure<ScrapingOptions>(builder.Configuration.GetSection("Scraping"));

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<ICompetitorDiscoveryService, CompetitorDiscoveryService>();
builder.Services.AddScoped<IPriceComparisonService, PriceComparisonService>();
builder.Services.AddScoped<IScrapingService, SeleniumScrapingService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();

builder.Services.AddHttpClient<IChatGptAnalysisService, OpenAiChatGptAnalysisService>((provider, client) =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Host.ConfigureHostOptions(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

builder.Services.AddHostedService<ScrapeWorker>();

var app = builder.Build();


// Startup-time DB connectivity check (helps surface configuration/server problems early)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        logger.LogInformation("Checking database connectivity...");
        var canConnect = await db.Database.CanConnectAsync();
        if (!canConnect)
        {
            logger.LogError("Unable to connect to database. Verify connection string and that SQL Server is running.");
            // Optionally: throw new InvalidOperationException("Database not reachable");
        }
        else
        {
            logger.LogInformation("Database connectivity OK.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database connectivity check failed. Verify SQL Server instance and connection string.");
        // Optionally rethrow to stop startup: throw;
    }
}


app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();

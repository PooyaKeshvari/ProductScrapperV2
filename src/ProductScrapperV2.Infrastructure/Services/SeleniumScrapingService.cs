using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Microsoft.Extensions.Options;
using ProductScrapperV2.Application.Interfaces;
using ProductScrapperV2.Infrastructure.Options;

namespace ProductScrapperV2.Infrastructure.Services;

public class SeleniumScrapingService : IScrapingService
{
    private readonly ScrapingOptions _options;

    public SeleniumScrapingService(IOptions<ScrapingOptions> options)
    {
        _options = options.Value;
    }

    public Task<IReadOnlyCollection<string>> SearchGoogleAsync(string query, CancellationToken cancellationToken)
    {
        // NOTE: This uses Selenium to perform a Google search and returns raw link text for AI analysis.
        var results = new List<string>();
        using var driver = BuildDriver();
        driver.Navigate().GoToUrl($"https://www.google.com/search?q={Uri.EscapeDataString(query)}");
        var anchors = driver.FindElements(By.CssSelector("a"));
        foreach (var anchor in anchors)
        {
            if (string.IsNullOrWhiteSpace(anchor.Text))
            {
                continue;
            }
            results.Add($"{anchor.Text} | {anchor.GetAttribute("href")}");
        }
        return Task.FromResult<IReadOnlyCollection<string>>(results);
    }

    public Task<IReadOnlyCollection<string>> ExtractRawElementsAsync(string url, CancellationToken cancellationToken)
    {
        var elements = new List<string>();
        using var driver = BuildDriver();
        driver.Navigate().GoToUrl(url);
        var nodes = driver.FindElements(By.CssSelector("body *"));
        foreach (var node in nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Text))
            {
                continue;
            }
            elements.Add(node.Text.Trim());
        }
        return Task.FromResult<IReadOnlyCollection<string>>(elements);
    }

    private IWebDriver BuildDriver()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        var driver = new ChromeDriver(options);
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(_options.PageLoadTimeoutSeconds);
        return driver;
    }
}

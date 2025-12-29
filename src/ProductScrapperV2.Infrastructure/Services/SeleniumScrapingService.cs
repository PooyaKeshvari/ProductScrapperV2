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
        // NOTE: Use result titles (h3) to reduce noisy links and keep stable output for AI analysis.
        var results = new List<string>();
        using var driver = BuildDriver();
        driver.Navigate().GoToUrl($"https://www.google.com/search?q={Uri.EscapeDataString(query)}&num=10");

        var titles = driver.FindElements(By.CssSelector("h3"));
        foreach (var title in titles)
        {
            var anchor = title.FindElements(By.XPath("./ancestor::a")).FirstOrDefault();
            var href = anchor?.GetAttribute("href");
            if (string.IsNullOrWhiteSpace(href))
            {
                continue;
            }

            if (!Uri.IsWellFormedUriString(href, UriKind.Absolute))
            {
                continue;
            }

            if (href.Contains("google.com", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = string.IsNullOrWhiteSpace(title.Text) ? anchor?.Text : title.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            results.Add($"{text.Trim()} | {href}");
        }

        return Task.FromResult<IReadOnlyCollection<string>>(results.Distinct().ToList());
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

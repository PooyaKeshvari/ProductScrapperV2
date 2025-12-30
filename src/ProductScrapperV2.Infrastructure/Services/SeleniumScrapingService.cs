using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
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
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var driver = BuildDriver();
            driver.Navigate().GoToUrl($"https://www.google.com/search?q={Uri.EscapeDataString(query)}&num=10&hl=fa");

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(_options.PageLoadTimeoutSeconds));
            wait.Until(d => d.FindElements(By.CssSelector("a h3")).Any());

            var titles = driver.FindElements(By.CssSelector("a h3"));
            foreach (var title in titles)
            {
                var anchor = title.FindElements(By.XPath("ancestor::a")).FirstOrDefault();
                var href = anchor?.GetAttribute("href");
                var resolved = NormalizeSearchUrl(href);
                if (string.IsNullOrWhiteSpace(resolved))
                {
                    continue;
                }

                var text = string.IsNullOrWhiteSpace(title.Text) ? anchor?.Text : title.Text;
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                results.Add($"{text.Trim()} | {resolved}");
            }
        }
        catch (WebDriverException)
        {
            return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
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

    private static string? NormalizeSearchUrl(string? href)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return null;
        }

        if (href.Contains("google.com", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(href);
            var target = ExtractQueryValue(uri.Query, "q");
            if (!string.IsNullOrWhiteSpace(target))
            {
                href = target;
            }
        }

        if (!Uri.IsWellFormedUriString(href, UriKind.Absolute))
        {
            return null;
        }

        if (href.Contains("google.com", StringComparison.OrdinalIgnoreCase) ||
            href.Contains("webcache.googleusercontent.com", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return href;
    }

    private static string? ExtractQueryValue(string query, string key)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var trimmed = query.StartsWith("?") ? query[1..] : query;
        var parts = trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length != 2)
            {
                continue;
            }

            if (!string.Equals(Uri.UnescapeDataString(keyValue[0]), key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return Uri.UnescapeDataString(keyValue[1]);
        }

        return null;
    }
}

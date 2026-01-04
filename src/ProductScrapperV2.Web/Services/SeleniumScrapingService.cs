using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ProductScrapperV2.Web.ViewModels;

namespace ProductScrapperV2.Web.Services;

public class SeleniumScrapingService : IScrapingService
{
    private readonly IOptions<ScrapingOptions> _options;

    public SeleniumScrapingService(IOptions<ScrapingOptions> options)
    {
        _options = options;
    }

    public Task<IReadOnlyCollection<string>> SearchGoogleAsync(string query, CancellationToken ct)
    {
        var results = new List<string>();

        using var driver = BuildDriver();
        driver.Navigate().GoToUrl($"https://www.google.com/search?q={Uri.EscapeDataString(query)}&hl=fa");

        Thread.Sleep(2000);

        var titles = driver.FindElements(By.CssSelector("a h3"));
        foreach (var t in titles)
        {
            var a = t.FindElement(By.XPath("ancestor::a"));
            var href = a.GetAttribute("href");
            if (!string.IsNullOrWhiteSpace(href))
                results.Add($"{t.Text} | {href}");
        }

        return Task.FromResult<IReadOnlyCollection<string>>(results);
    }

    public Task<IScrapingSession> CreateSessionAsync(CancellationToken ct)
    {
        return Task.FromResult<IScrapingSession>(
            new SeleniumScrapingSession(BuildDriver(), _options));
    }

    private IWebDriver BuildDriver()
    {
        var options = new ChromeOptions();
        //options.AddArgument("--headless=new");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-gpu");

        // Hide automation flags
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddExcludedArgument("enable-automation");
        options.AddAdditionalOption("useAutomationExtension", false);

        // Normal browser appearance
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--start-maximized");
        options.AddArgument("--disable-infobars");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-setuid-sandbox");

        // Disable features that expose automation
        options.AddArgument("--disable-web-security");
        options.AddArgument("--allow-running-insecure-content");
        // Make sure the chromedriver binary is found in the app output.
        // If you use the Selenium.WebDriver.ChromeDriver NuGet package it will be copied here.
        var driverDirectory = AppContext.BaseDirectory;

        // Create a service that uses the binary in the outp
        return new ChromeDriver(options);
    }

    private sealed class SeleniumScrapingSession : IScrapingSession
    {
        private readonly IWebDriver _driver;
        private readonly ScrapingOptions _options;


        public SeleniumScrapingSession(IWebDriver driver, IOptions<ScrapingOptions> options)
        {
            _driver = driver;
            _options = options.Value;
        }

        public Task NavigateAsync(string url, CancellationToken ct)
        {
            _driver.Navigate().GoToUrl(url);
            return Task.CompletedTask;
        }

        public Task ClickAsync(string cssSelector, CancellationToken ct)
        {
            var el = _driver.FindElement(By.CssSelector(cssSelector));
            el.Click();
            return Task.CompletedTask;
        }

        public Task<PageSnapshotDto> CaptureSnapshotAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var texts = new List<string>();

            // 1️⃣ عنوان‌ها
            texts.AddRange(
                _driver.FindElements(By.CssSelector("h1, h2"))
                    .Select(e => e.Text)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
            );

            // 2️⃣ قیمت‌های عددی (خیلی مهم)
            texts.AddRange(
                _driver.FindElements(By.XPath(
                    "//*[contains(text(),'تومان') or contains(text(),'ریال') or contains(text(),'toman') or contains(text(),'₮')]"))
                .Select(e => e.Text)
                .Distinct()
                .Take(5)
            );

            // 3️⃣ نشانه‌های ناموجود
            texts.AddRange(
                _driver.FindElements(By.XPath(
                    "//*[contains(text(),'ناموجود') or contains(text(),'اتمام موجودی') or contains(text(),'فروشنده ای ندارد')]"))
                .Select(e => e.Text)
                .Distinct()
            );

            var links = _driver.FindElements(By.CssSelector("a[href]"))
                .Where(a => !string.IsNullOrWhiteSpace(a.Text))
                .Select(a => new LinkSnapshotDto
                {
                    Text = a.Text.Trim(),
                    Href = a.GetAttribute("href"),
                    CssSelector = BuildCssSelector(a)
                })
                .DistinctBy(l => l.Href)
                .Take(25)
                .ToList();

            return Task.FromResult(new PageSnapshotDto
            {
                Url = _driver.Url,
                Title = _driver.Title,
                VisibleTexts = texts.Distinct().Take(40).ToList(),
                Links = links
            });
        }


        private static string BuildCssSelector(IWebElement el)
        {
            var id = el.GetAttribute("id");
            if (!string.IsNullOrWhiteSpace(id))
                return $"#{id}";

            var cls = el.GetAttribute("class");
            if (!string.IsNullOrWhiteSpace(cls))
                return "." + cls.Split(' ').First();

            return el.TagName;
        }


        public ValueTask DisposeAsync()
        {
            _driver.Quit();
            _driver.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

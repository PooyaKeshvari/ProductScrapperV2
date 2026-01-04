namespace ProductScrapperV2.Web.Services;

using OpenQA.Selenium;
using System.Globalization;
using System.Text.RegularExpressions;

public static class PriceExtractor
{
    // selectors رایج سایت‌های ایرانی
    private static readonly string[] PriceSelectors =
    {
        "[data-price]",
        ".product-price",
        ".price",
        ".selling-price",
        ".c-product__seller-price-pure",
        ".c-product__seller-price-real",
        "span[itemprop='price']"
    };

    public static decimal? TryExtractFromDom(IWebDriver driver)
    {
        foreach (var selector in PriceSelectors)
        {
            var elements = driver.FindElements(By.CssSelector(selector));
            foreach (var el in elements)
            {
                var text = el.Text;
                var price = TryParsePrice(text);
                if (price.HasValue)
                    return price;
            }
        }

        return null;
    }

    public static decimal? TryExtractFromText(string text)
    {
        var match = Regex.Match(
            text,
            @"(\d{1,3}(,\d{3})+)\s*(تومان|ریال)",
            RegexOptions.IgnoreCase);

        if (!match.Success)
            return null;

        var number = match.Groups[1].Value.Replace(",", "");
        if (!decimal.TryParse(number, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
            return null;

        // اگر ریاله → تبدیل به تومان
        if (match.Value.Contains("ریال"))
            price /= 10;

        return price;
    }

    private static decimal? TryParsePrice(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        return TryExtractFromText(text);
    }
}

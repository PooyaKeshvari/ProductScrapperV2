using Microsoft.Extensions.Options;
using ProductScrapperV2.Web.ViewModels;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CompetitorDiscoveryResult = ProductScrapperV2.Web.ViewModels.CompetitorDiscoveryResult;

namespace ProductScrapperV2.Web.Services;

public interface IChatGptAnalysisService
{
    // legacy (اختیاری – فعلاً نگه می‌داریم)
    Task<ScrapeResultDto?> AnalyzeProductAsync(
        string productName,
        string url,
        IReadOnlyCollection<string> rawElements,
        CancellationToken ct);

    Task<IReadOnlyCollection<CompetitorDiscoveryResult>> AnalyzeCompetitorsAsync(
        string productName,
        IReadOnlyCollection<string> googleResults,
        CancellationToken ct);

    // agent step
    Task<AgentActionDto> DecideNextActionAsync(
        string productName,
        PageSnapshotDto snapshot,
        int step,
        CancellationToken ct);

    // final extraction
    Task<ScrapeResultDto?> ExtractProductAsync(
        string productName,
        PageSnapshotDto snapshot,
        CancellationToken ct);

    // backward alias (optional)
    Task<ScrapeResultDto?> ExtractFromSnapshotAsync(
        string productName,
        PageSnapshotDto snapshot,
        CancellationToken ct);
}

public class ChatGptAnalysisService : IChatGptAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
    {
        new JsonStringEnumConverter()
    }

    };

    public ChatGptAnalysisService(HttpClient httpClient, IOptions<OpenAiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    // -------------------- COMPETITOR DISCOVERY --------------------


    private Task<string?> SendDecisionAsync(string prompt, CancellationToken ct)
    {
        return SendChatRequestAsync(prompt, _options.DecisionModel, ct);
    }


    private Task<string?> SendExtractionAsync(string prompt, CancellationToken ct)
    {
        return SendChatRequestAsync(prompt, _options.ExtractionModel, ct);
    }


    private async Task<string?> SendChatRequestAsync(
    string prompt,
    string model,
    CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var payload = new
        {
            model,
            temperature = 0,
            messages = new[]
            {
            new { role = "system", content = "Return strict JSON only." },
            new { role = "user", content = prompt }
        }
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
    }



    public async Task<IReadOnlyCollection<CompetitorDiscoveryResult>> AnalyzeCompetitorsAsync(
        string productName,
        IReadOnlyCollection<string> searchResults,
        CancellationToken ct)
    {
        var prompt = ChatGptPromptBuilder.BuildCompetitorDiscoveryPrompt(productName, searchResults);
        var response = await SendChatRequestAsync(prompt, ct);

        if (string.IsNullOrWhiteSpace(response))
            return Array.Empty<CompetitorDiscoveryResult>();

        var pureJson = ExtractPureJson(response);
        var normalized = NormalizeToJsonArray(pureJson);

        if (normalized is null)
            return Array.Empty<CompetitorDiscoveryResult>();

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyCollection<CompetitorDiscoveryResult>>(normalized, JsonOptions)
                   ?? Array.Empty<CompetitorDiscoveryResult>();
        }
        catch (JsonException)
        {
            return Array.Empty<CompetitorDiscoveryResult>();
        }
    }


    private static string? NormalizeToJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        var trimmed = json.Trim();

        // اگر خودش آرایه است → اوکی
        if (trimmed.StartsWith("["))
            return trimmed;

        // اگر فقط یک object است → تبدیل به آرایه
        if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
            return $"[{trimmed}]";

        // اگر چند object با , جدا شده‌اند → wrap
        if (trimmed.Contains("},{"))
            return $"[{trimmed}]";

        return null;
    }


    // -------------------- AGENT DECISION --------------------

    public async Task<AgentActionDto> DecideNextActionAsync(
        string productName,
        PageSnapshotDto snapshot,
        int step,
        CancellationToken ct)
    {
        if (snapshot.VisibleTexts.Any(t =>
        t.Contains("تومان") || t.Contains("ریال")))
        {
            // احتمال زیاد صفحه محصول است
            return new AgentActionDto
            {
                Type = AgentActionType.ExtractProduct,
                Reason = "Price detected"
            };
        }

        // Prepare pieces to avoid embedding literal braces directly inside an interpolated raw string
        var visibleTexts = string.Join("\n", snapshot.VisibleTexts.Take(150));
        var links = string.Join("\n", snapshot.Links.Select(l => $"{l.Text} | {l.Href} | {l.CssSelector}"));

        // Non-interpolated raw string for the JSON shape (braces are safe here)
        var jsonShape = """
{
  "type": "Navigate | Click | ExtractProduct | Stop",
  "url": "optional",
  "cssSelector": "optional",
  "reason": "short explanation"
}
""";

        var prompt = $"""
You are an AI web navigation agent.

STEP: {step}
PRODUCT: {productName}

URL: {snapshot.Url} 
TITLE: {snapshot.Title}

VISIBLE TEXTS:
{visibleTexts}

LINKS:
{links}

Return ONLY valid JSON in this shape:
{jsonShape}
""";

        var response = await SendChatRequestAsync(prompt, ct);

        try
        {

            var json = ExtractPureJson(response);



            if (json is null)
                return Stop("Invalid AI response");

            AgentActionDto? action;
            try
            {
                action = JsonSerializer.Deserialize<AgentActionDto>(json, JsonOptions);

            }
            catch
            {
                return Stop("JSON deserialization failed");
            }

            return ValidateAction(action);

            //return JsonSerializer.Deserialize<AgentActionDto>(response!, JsonOptions)
            //       ?? new AgentActionDto { Type = AgentActionType.Stop };
        }
        catch
        {
            return new AgentActionDto { Type = AgentActionType.Stop };
        }
    }



    private static AgentActionDto ValidateAction(AgentActionDto? action)
    {
        if (action is null)
            return Stop("Null action");

        return action.Type switch
        {
            AgentActionType.Navigate when
                !string.IsNullOrWhiteSpace(action.Url)
                => action,

            AgentActionType.Click when
                !string.IsNullOrWhiteSpace(action.CssSelector)
                => action,

            AgentActionType.ExtractProduct
                => action,

            AgentActionType.Stop
                => action,

            _ => Stop("Invalid action payload")
        };
    }

    private static AgentActionDto Stop(string reason) =>
        new()
        {
            Type = AgentActionType.Stop,
            Reason = reason
        };


    // -------------------- PRODUCT EXTRACTION --------------------


    private static string? ExtractPureJson(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return null;

        var trimmed = response.Trim();

        // Remove common code-fence wrappers
        if (trimmed.StartsWith("```"))
        {
            trimmed = trimmed
                .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                .Replace("```", "")
                .Trim();
        }

        // اطمینان از اینکه با { شروع می‌شود
        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');

        if (firstBrace < 0 || lastBrace < 0 || lastBrace <= firstBrace)
            return null;

        return trimmed.Substring(firstBrace, lastBrace - firstBrace + 1);
    }


    public async Task<ScrapeResultDto?> ExtractProductAsync(
        string productName,
        PageSnapshotDto snapshot,
        CancellationToken ct)
    {
        // -------------------------------
        // 1️⃣ VisibleTexts heuristic (NO GPT)
        // -------------------------------
        var visibleText = string.Join("\n", snapshot.VisibleTexts);

        var textPrice = PriceExtractor.TryExtractFromText(visibleText);
        if (textPrice.HasValue)
        {
            return new ScrapeResultDto
            {
                ProductTitle = snapshot.Title,
                ProductUrl = snapshot.Url,
                Price = textPrice.Value,
                MatchPercentage = 85,
                ConfidenceScore = 0.75m
            };
        }

        // -------------------------------
        // 2️⃣ GPT fallback (FULL PAGE)
        // -------------------------------
        var prompt = ChatGptPromptBuilder.BuildExtractProductPrompt(
            productName,
            snapshot);

        var response = await SendExtractionAsync(prompt, ct);
        if (string.IsNullOrWhiteSpace(response))
            return null;

        var json = ExtractPureJson(response);
        if (json is null)
            return null;

        try
        {
            var result = JsonSerializer.Deserialize<ScrapeResultDto>(json, JsonOptions);

            // اگر GPT هم مطمئن نیست → رد
            if (result is null || result.Price == 0)
                return null;

            return result;
        }
        catch
        {
            return null;
        }
    }



    // backward compatibility
    public Task<ScrapeResultDto?> ExtractFromSnapshotAsync(
        string productName,
        PageSnapshotDto snapshot,
        CancellationToken ct)
        => ExtractProductAsync(productName, snapshot, ct);

    // -------------------- LEGACY --------------------

    [Obsolete("LEGACY. Use ExtractProductAsync with PageSnapshotDto.", error: true)]
    public async Task<ScrapeResultDto?> AnalyzeProductAsync(
        string productName,
        string pageUrl,
        IReadOnlyCollection<string> rawElements,
        CancellationToken ct)
    {
        throw new NotSupportedException(
            "AnalyzeProductAsync is deprecated. Use ExtractProductAsync.");
    }


    // -------------------- OPENAI CALL --------------------

    private async Task<string?> SendChatRequestAsync(string prompt, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var payload = new
        {
            model = _options.ExtractionModel,
            messages = new[]
            {
                new { role = "system", content = "You are a strict JSON-only AI agent for product price extraction." },
                new { role = "user", content = prompt }
            },
            temperature = 0.1
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);

        // Use an explicit using-block to scope the JsonDocument and avoid access/scoping issues.
        using (var doc = JsonDocument.Parse(json))
        {
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choicesElement) &&
                choicesElement.ValueKind == JsonValueKind.Array &&
                choicesElement.GetArrayLength() > 0)
            {
                var firstChoice = choicesElement[0];

                if (firstChoice.TryGetProperty("message", out var messageElement) &&
                    messageElement.ValueKind == JsonValueKind.Object &&
                    messageElement.TryGetProperty("content", out var contentElement) &&
                    contentElement.ValueKind == JsonValueKind.String)
                {
                    return contentElement.GetString();
                }
            }
        }

        return null;
    }
}


public static class ChatGptPromptBuilder
{
    /*
    public static string BuildProductAnalysisPrompt(string productName, string pageUrl, IReadOnlyCollection<string> rawElements)
    {
        var jsonExample = """
{
  "productTitle": "...",
  "productUrl": "...",
  "price": 0,
  "matchPercentage": 0,
  "confidenceScore": 0
}
""";

        return $"""
شما یک تحلیل گر قیمت هستید. هدف، استخراج نام محصول، قیمت و لینک صحیح از عناصر خام است.
محصول هدف: {productName}
آدرس صفحه: {pageUrl}

داده های خام (HTML/متن):
{string.Join("\n", rawElements)}

خروجی را فقط به صورت JSON برگردانید با کلیدهای زیر:
{jsonExample}
""";
    }

    public static string BuildCompetitorDiscoveryPrompt(string productName, IReadOnlyCollection<string> searchResults)
    {
        var jsonArrayExample = """
[
  {
    "competitorName": "...",
    "websiteUrl": "...",
    "credibilityScore": 0,
    "suggestedRank": 0,
    "reason": "..."
  }
]
""";

        return $"""
شما باید سایت های فروشنده مرتبط را از نتایج جست و جو استخراج کنید.
محصول هدف: {productName}

نتایج خام جست و جو:
{string.Join("\n", searchResults)}

خروجی را فقط به صورت JSON آرایه ای برگردانید:
{jsonArrayExample}
""";
    }
*/


    public static string BuildExtractProductPrompt(
    string productName,
    PageSnapshotDto snapshot)
    {
        var jsonShape = """
    {
      "productTitle": "...",
      "productUrl": "{snapshotUrl}",
      "price": number,
      "matchPercentage": number,
      "confidenceScore": number
    }
    """;

        var jsonWithUrl = jsonShape.Replace("{snapshotUrl}", snapshot.Url);

        return $"""
    You are a strict JSON-only AI agent for product extraction.

    GOAL:
    Extract product information for: "{productName}"

    PAGE URL:
    {snapshot.Url}

    PAGE TITLE:
    {snapshot.Title}

    VISIBLE TEXTS:
    {string.Join("\n", snapshot.VisibleTexts.Take(100))}

    STRICT RULES:
    - Return ONLY valid JSON
    - NO markdown
    - NO extra text

    JSON FORMAT:
    {jsonWithUrl}
    """;
    }


    public static string BuildAgentDecisionPrompt(
        string productName,
        PageSnapshotDto snapshot,
        int step)
    {
        // Use a non-interpolated raw string for the JSON shape, then insert it into the prompt
        var jsonShape = @"{
      ""type"": ""Navigate | Click | ExtractProduct | Stop"",
      ""url"": ""optional (only for Navigate)"",
      ""cssSelector"": ""optional (only for Click)"",
      ""reason"": ""short reason""
    }";

        return $"""
    You are a web navigation agent controlling a browser.

    GOAL:
    Reach the ACTUAL PRODUCT PAGE for:
    "{productName}"

    CURRENT STEP:
    {step}

    CURRENT PAGE URL:
    {snapshot.Url}

    PAGE TITLE:
    {snapshot.Title}

    IMPORTANT PAGE TEXTS:
    {string.Join("\n", snapshot.VisibleTexts.Take(40))}

    VISIBLE LINKS (text | href | selector):
    {string.Join("\n", snapshot.Links.Select(l =>
        $"{l.Text} | {l.Href} | {l.CssSelector}"))}

    DECISION RULES (VERY STRICT):
    - Return ONLY valid JSON
    - NO markdown
    - NO extra text
    - Choose EXACTLY ONE action

    ACTION LOGIC:
    - If price or stock status is visible → ExtractProduct
    - If this is a clear product page → ExtractProduct
    - If a relevant product link exists → Click
    - If navigation to a better page is needed → Navigate
    - If nothing useful can be done → Stop

    JSON FORMAT (STRICT):
    {jsonShape}
    """;
    }


    public static string BuildCompetitorDiscoveryPrompt(
        string productName,
        IReadOnlyCollection<string> searchResults)
    {
        // Use a non-interpolated raw string for the JSON array example
        var jsonArrayExample = @"[
      {
        ""competitorName"": ""string"",
        ""websiteUrl"": ""string"",
        ""credibilityScore"": 0,
        ""suggestedRank"": 0,
        ""reason"": ""short reason""
      }
    ]";

        return $"""
    You are analyzing Google search results to find ecommerce competitors.

    TARGET PRODUCT:
    {productName}

    SEARCH RESULTS:
    {string.Join("\n", searchResults)}

    STRICT RULES:
    - Return ONLY valid JSON
    - Root element MUST be a JSON ARRAY []
    - Do NOT return multiple objects separated by commas
    - Do NOT wrap results in an object
    - Only include real ecommerce or marketplace websites
    - Rank competitors by relevance and credibility (1 = best)

    JSON FORMAT:
    {jsonArrayExample}
    """;
    }
    

}

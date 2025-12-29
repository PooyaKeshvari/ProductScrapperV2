namespace ProductScrapperV2.Application.Prompts;

public static class ChatGptPromptBuilder
{
    public static string BuildProductAnalysisPrompt(string productName, string pageUrl, IReadOnlyCollection<string> rawElements)
    {
        return $"""
شما یک تحلیل گر قیمت هستید. هدف، استخراج نام محصول، قیمت و لینک صحیح از عناصر خام است.
محصول هدف: {productName}
آدرس صفحه: {pageUrl}

داده های خام (HTML/متن):
{string.Join("\n", rawElements)}

خروجی را فقط به صورت JSON برگردانید با کلیدهای زیر:
{{
  \"productTitle\": \"...\",
  \"productUrl\": \"...\",
  \"price\": 0,
  \"matchPercentage\": 0,
  \"confidenceScore\": 0
}}
""";
    }

    public static string BuildCompetitorDiscoveryPrompt(string productName, IReadOnlyCollection<string> searchResults)
    {
        return $"""
شما باید سایت های فروشنده مرتبط را از نتایج جست و جو استخراج کنید.
محصول هدف: {productName}

نتایج خام جست و جو:
{string.Join("\n", searchResults)}

خروجی را فقط به صورت JSON آرایه ای برگردانید:
[
  {{
    \"competitorName\": \"...\",
    \"websiteUrl\": \"...\",
    \"credibilityScore\": 0,
    \"suggestedRank\": 0,
    \"reason\": \"...\"
  }}
]
""";
    }
}

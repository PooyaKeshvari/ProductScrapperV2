using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProductScrapperV2.Application.DTOs;
using ProductScrapperV2.Application.Interfaces;
using ProductScrapperV2.Application.Prompts;
using ProductScrapperV2.Infrastructure.Options;

namespace ProductScrapperV2.Infrastructure.Services;

public class OpenAiChatGptAnalysisService : IChatGptAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;

    public OpenAiChatGptAnalysisService(HttpClient httpClient, IOptions<OpenAiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<ScrapeResultDto?> AnalyzeProductAsync(
        string productName,
        string pageUrl,
        IReadOnlyCollection<string> rawElements,
        CancellationToken cancellationToken)
    {
        var prompt = ChatGptPromptBuilder.BuildProductAnalysisPrompt(productName, pageUrl, rawElements);
        var response = await SendChatRequestAsync(prompt, cancellationToken);
        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ScrapeResultDto>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public async Task<IReadOnlyCollection<CompetitorDiscoveryResult>> AnalyzeCompetitorsAsync(
        string productName,
        IReadOnlyCollection<string> searchResults,
        CancellationToken cancellationToken)
    {
        var prompt = ChatGptPromptBuilder.BuildCompetitorDiscoveryPrompt(productName, searchResults);
        var response = await SendChatRequestAsync(prompt, cancellationToken);
        if (string.IsNullOrWhiteSpace(response))
        {
            return Array.Empty<CompetitorDiscoveryResult>();
        }

        return JsonSerializer.Deserialize<IReadOnlyCollection<CompetitorDiscoveryResult>>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? Array.Empty<CompetitorDiscoveryResult>();
    }

    private async Task<string?> SendChatRequestAsync(string prompt, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var payload = new
        {
            model = _options.Model,
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant for price analysis." },
                new { role = "user", content = prompt }
            },
            temperature = 0.1
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content;
    }
}

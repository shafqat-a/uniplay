using Microsoft.Extensions.Logging;
using Polly;
using UniPlay.Core.Services;
using UniPlay.Infrastructure.Resilience;

namespace UniPlay.Infrastructure.Services;

/// <summary>
/// HTTP client service with Polly resilience policies
/// </summary>
public class HttpClientService : IHttpClientService
{
    private readonly HttpClient _httpClient;
    private readonly ResiliencePipeline<HttpResponseMessage> _resiliencePipeline;
    private readonly ILogger<HttpClientService> _logger;

    public HttpClientService(
        HttpClient httpClient,
        ILogger<HttpClientService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _resiliencePipeline = ResiliencePolicies.CreateCombinedPolicy();
    }

    public async Task<HttpResponseMessage> ExecuteAsync(Func<Task<HttpResponseMessage>> action)
    {
        try
        {
            return await _resiliencePipeline.ExecuteAsync(async ct => await action(), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP request failed after resilience policies");
            throw;
        }
    }

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        return await ExecuteAsync(() => _httpClient.GetAsync(url));
    }

    public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content)
    {
        return await ExecuteAsync(() => _httpClient.PostAsync(url, content));
    }
}

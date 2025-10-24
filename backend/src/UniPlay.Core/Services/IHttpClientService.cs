namespace UniPlay.Core.Services;

/// <summary>
/// Service for making resilient HTTP calls with Polly policies
/// </summary>
public interface IHttpClientService
{
    /// <summary>
    /// Execute HTTP request with resilience policies
    /// </summary>
    Task<HttpResponseMessage> ExecuteAsync(Func<Task<HttpResponseMessage>> action);

    /// <summary>
    /// Execute HTTP GET request with resilience
    /// </summary>
    Task<HttpResponseMessage> GetAsync(string url);

    /// <summary>
    /// Execute HTTP POST request with resilience
    /// </summary>
    Task<HttpResponseMessage> PostAsync(string url, HttpContent content);
}

using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Yaush.GhostUser.Url
{
    public interface IUrlCreatorService
    {
        Task<CreateShortenUrlResponse> CreateShortenedUrl(string url);
    }

    public class UrlCreatorService(HttpClient httpClient, ILogger<UrlCreatorService> logger) : IUrlCreatorService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<UrlCreatorService> _logger = logger;

        public async Task<CreateShortenUrlResponse> CreateShortenedUrl(string url)
        {
            _logger.LogInformation("Creating shortened url for {url}", url);
            var requestJson = new StringContent(
                JsonSerializer.Serialize(new CreateShortenedUrlRequest() { Url = url }));
            var resp = await _httpClient.PostAsync("/", requestJson);

            if (resp is null || resp.IsSuccessStatusCode == false)
                return CreateShortenUrlResponse.FailureResponse;

            using var responseStream = await resp.Content.ReadAsStreamAsync();
            var shortenedUrlResponse = JsonSerializer.Deserialize<CreateShortenUrlResponse>(responseStream);

            if (shortenedUrlResponse is null)
                return CreateShortenUrlResponse.FailureResponse;

            return shortenedUrlResponse;
        }
    }

    public class CreateShortenedUrlRequest
    {
        public required string Url { get; set; }
    }

    public class CreateShortenUrlResponse()
    {
        public static CreateShortenUrlResponse FailureResponse => new() { Success = false };

        public bool Success { get; set; } = false;
        public string ShortenedUrl { get; set; } = string.Empty;
    }
}

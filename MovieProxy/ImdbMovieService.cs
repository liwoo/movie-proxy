using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MovieProxy;

interface IMovieService
{
    public Task<IEnumerable<PartialMovie>> GetPopularMovies();
    public Task<IEnumerable<PartialMovie>> GetRecentlyAddedMovies();
    public Task<IEnumerable<PartialMovie>> GetRandomMovies();
}

internal record MovieCollectionResponse()
{
    [JsonPropertyName("movie_results")] public List<MovieItemResponse>? MovieResults { get; init; }
}

internal record MovieItemResponse()
{
    public string? Title { get; init; }
    public string? Year { get; init; }
    [JsonPropertyName("imdb_id")] public string? ImdbId { get; init; }
}

internal record MovieImageResponse(string Poster);

public class ImdbMovieService : IMovieService
{
    private readonly HttpClient _client;
    private readonly ILogger<ImdbMovieService> _logger;
    private readonly IConfiguration _config;

    public ImdbMovieService(ILogger<ImdbMovieService> logger, IConfiguration config)
    {
        _logger = logger;
        _client = new HttpClient();
        _config = config;
    }

    private HttpRequestMessage GenerateHttpRequest(string endpoint)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{_config.GetValue<string>("ImdbRoot")}/{endpoint}"),
            Headers =
            {
                {"x-rapidapi-host", _config.GetValue<string>("ImdbHost")},
                {"x-rapidapi-key", _config.GetValue<string>("RapidApiKey")},
            },
        };

        return request;
    }

    private async Task<T?> FetchGenericResponse<T>(string endpoint)
    {
        using var response = await _client.SendAsync(GenerateHttpRequest(endpoint));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var genericResponse = JsonSerializer.Deserialize<T>(body,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        _logger.LogInformation("Responded with {T}", genericResponse);
        return genericResponse;
    }

    private async Task<string> FetchMovieImage(string imdbId)
    {
        var endpoint = $"?type=get-movies-images-by-imdb&imdb={imdbId}";
        var imageResponse = await FetchGenericResponse<MovieImageResponse>(endpoint);
        return imageResponse?.Poster ?? _config.GetValue<string>("DefaultImage");
    }

    private async Task<IEnumerable<PartialMovie>> FetchMovieCollection(string endpoint)
    {
        var movieCollectionResponse = await FetchGenericResponse<MovieCollectionResponse>(endpoint);

        if (movieCollectionResponse?.MovieResults == null) return new List<PartialMovie>() { };
        var moviesWithImages = new List<PartialMovie>() { };

        foreach (var movieResult in movieCollectionResponse.MovieResults)
        {
            var image = await FetchMovieImage(movieResult?.ImdbId ?? "some-id");
            var movie = new PartialMovie(movieResult?.Title ?? "Some Title", image, movieResult?.ImdbId ?? "some-id");
            moviesWithImages.Add(movie);
        }

        return moviesWithImages;
    }
    public async Task<IEnumerable<PartialMovie>> GetPopularMovies()
    {
        return await FetchMovieCollection("?type=get-popular-movies&page=1&year=2022");
    }

    public async Task<IEnumerable<PartialMovie>> GetRecentlyAddedMovies()
    {
        return await FetchMovieCollection("?type=get-recently-added-movies&page=2");
    }

    public async Task<IEnumerable<PartialMovie>> GetRandomMovies()
    {
        return await FetchMovieCollection("?type=get-random-movies&page=1");
    }
}
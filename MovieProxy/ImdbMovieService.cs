using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MovieProxy;

internal interface IMovieService
{
    public Task<IEnumerable<PartialMovie>> GetPopularMovies(int? page);
    public Task<IEnumerable<PartialMovie>> GetRecentlyAddedMovies(int? page);
    public Task<IEnumerable<PartialMovie>> GetRandomMovies(int? page);
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

        if (movieCollectionResponse?.MovieResults == null) return Enumerable.Empty<PartialMovie>();

        var moviesWithImages = Enumerable.Empty<PartialMovie>().ToList();
        //chunk movie results into groups of 5
        var movieChunks = movieCollectionResponse.MovieResults.Chunk(5);
        foreach (var chunk in movieChunks)
        {
            //run in parallel to fetch images
            var tasks = chunk.Select(async movie =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1.5));
                    var image = await FetchMovieImage(movie?.ImdbId ?? "some-image");
                    var movieWithImage = new PartialMovie(movie?.Title ?? "some-title", image, movie?.ImdbId ?? "some-id");
                    moviesWithImages.Add(movieWithImage);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to fetch image for movie {Movie}", movie);
                }
            });
            
            await Task.WhenAll(tasks);
        }


        return moviesWithImages;
    }

    public async Task<IEnumerable<PartialMovie>> GetPopularMovies(int? page = 1)
    {
        return await FetchMovieCollection($"?type=get-popular-movies&page={page}&year=2022");
    }

    public async Task<IEnumerable<PartialMovie>> GetRecentlyAddedMovies(int? page = 1)
    {
        return await FetchMovieCollection($"?type=get-recently-added-movies&page={page}");
    }

    public async Task<IEnumerable<PartialMovie>> GetRandomMovies(int? page = 1)
    {
        return await FetchMovieCollection($"?type=get-random-movies&page={page}");
    }
}
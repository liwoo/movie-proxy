using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MovieProxy;

internal interface IMovieService
{
    public Task<IEnumerable<PartialMovie>> GetPopularMovies(int? page);
    public Task<IEnumerable<PartialMovie>> GetRecentlyAddedMovies(int? page);
    public Task<IEnumerable<PartialMovie>> GetRandomMovies(int? page);
    public Task<IEnumerable<PartialMovie>> GetUpcomingMovies(int? page);
    public Task<IEnumerable<PartialMovie>> GetTrendingMovies(int? page);
    public Task<Movie> GetMovieDetails(string imdbId);
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

internal record MovieImageResponse(string? Fanart);

internal record MovieDetailsResponse()
{
    [JsonPropertyName("imdb_id")] public string? ImdbId { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Year { get; init; }
    public List<string>? Genres { get; init; }
    public List<string>? Stars { get; init; }
    public List<string>? Directors { get; init; }
    public List<string>? Language { get; init; }
    public int? Runtime { get; init; }
    public string? Rated { get; init; }
}

public class ImdbMovieService : IMovieService
{
    private readonly HttpClient _client;
    private readonly ILogger<ImdbMovieService> _logger;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;

    public ImdbMovieService(ILogger<ImdbMovieService> logger, IConfiguration config, IMemoryCache cache)
    {
        _logger = logger;
        _client = new HttpClient();
        _config = config;
        _cache = cache;
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
        //TODO: Add null responses of image properties to test cases
        var endpoint = $"?type=get-movies-images-by-imdb&imdb={imdbId}";
        var imageResponse = await FetchGenericResponse<MovieImageResponse>(endpoint);
        return string.IsNullOrEmpty(imageResponse?.Fanart)
            ? _config.GetValue<string>("DefaultImage")
            : imageResponse.Fanart;
    }

    private async Task<IEnumerable<PartialMovie>> FetchMovieCollection(string endpoint, int? chunksToFetch = -1)
    {
        var movieCollectionResponse = await FetchGenericResponse<MovieCollectionResponse>(endpoint);

        if (movieCollectionResponse?.MovieResults == null) return Enumerable.Empty<PartialMovie>();

        var moviesWithImages = Enumerable.Empty<PartialMovie>().ToList();
        //chunk movie results into groups of 5
        var movieChunks = movieCollectionResponse.MovieResults.Chunk(5);
        foreach (var (chunk, i) in movieChunks.Select((v, i) => (v, i)))
        {
            if (i == chunksToFetch) break;
            //run in parallel to fetch images
            var tasks = chunk.Select(async movie =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1.5));
                    var image = await FetchMovieImage(movie?.ImdbId ?? "some-image");
                    var movieWithImage =
                        new PartialMovie(movie?.Title ?? "some-title", image, movie?.ImdbId ?? "some-id");
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

    private async Task<Movie> PrepareMovieDetails(string imdbId)
    {
        var endpoint = $"?type=get-movie-details&imdb={imdbId}";
        var movieDetailsResponse = await FetchGenericResponse<MovieDetailsResponse>(endpoint);
        var image = await FetchMovieImage(movieDetailsResponse?.ImdbId ?? "some-image");
        var prodAndCast = new List<string>()
        {
            movieDetailsResponse?.Directors?.FirstOrDefault() ?? "some-director"
        };
        prodAndCast.AddRange(movieDetailsResponse?.Stars ?? Enumerable.Empty<string>());
        var recommendationsEndpoint = $"?type=get-similar-movies&imdb={imdbId}&page=1";
        var recommendations = await FetchMovieCollection(recommendationsEndpoint, 1);
        return new Movie(
            movieDetailsResponse?.Title ?? "some-title",
            image,
            movieDetailsResponse?.ImdbId ?? "some-id",
            movieDetailsResponse?.Description ?? "some-description",
            movieDetailsResponse?.Year ?? "some-year",
            movieDetailsResponse?.Genres,
            prodAndCast,
            movieDetailsResponse?.Language,
            movieDetailsResponse?.Runtime ?? 0,
            movieDetailsResponse?.Rated ?? "PG",
            recommendations
        );
    }

    private async Task<T> FetchGenericCachedResponse<T>(string key, Func<Task<T>> callback)
    {
        if (_cache.TryGetValue(key, out T cachedResponse))
            return cachedResponse;
        var results = await callback();
        _cache.Set(key, results, TimeSpan.FromDays(7));
        return results;
    }

    public async Task<IEnumerable<PartialMovie>> GetPopularMovies(int? page = 1)
    {
        async Task<IEnumerable<PartialMovie>> Callback() 
            => await FetchMovieCollection($"?type=get-popular-movies&page={page}&year=2022");

        return await FetchGenericCachedResponse<IEnumerable<PartialMovie>>(
            $"popular-movies-{page}", Callback);
    }

    public async Task<IEnumerable<PartialMovie>> GetRecentlyAddedMovies(int? page = 1)
    {
        async Task<IEnumerable<PartialMovie>> Callback() 
            => await FetchMovieCollection($"?type=get-recently-added-movies&page={page}");
        
        return await FetchGenericCachedResponse<IEnumerable<PartialMovie>>(
            $"recently-added-movies-{page}", Callback);
    }

    public async Task<IEnumerable<PartialMovie>> GetRandomMovies(int? page = 1)
    {
        async Task<IEnumerable<PartialMovie>> Callback() 
            => await FetchMovieCollection($"?type=get-random-movies&page={page}");
        
        return await FetchGenericCachedResponse<IEnumerable<PartialMovie>>(
            $"random-movies-{page}", Callback);
    }

    public async Task<IEnumerable<PartialMovie>> GetUpcomingMovies(int? page = 1)
    {
        async Task<IEnumerable<PartialMovie>> Callback() 
            => await FetchMovieCollection($"?type=get-upcoming-movies&page={page}");
        
        return await FetchGenericCachedResponse<IEnumerable<PartialMovie>>(
            $"upcoming-movies-{page}", Callback);
    }

    public async Task<IEnumerable<PartialMovie>> GetTrendingMovies(int? page = 1)
    {
        async Task<IEnumerable<PartialMovie>> Callback() 
            => await FetchMovieCollection($"?type=get-trending-movies&page={page}");
        
        return await FetchGenericCachedResponse<IEnumerable<PartialMovie>>(
            $"trending-movies-{page}", Callback);
    }

    public async Task<Movie> GetMovieDetails(string imdbId)
    {
        async Task<Movie> Callback() 
            => await PrepareMovieDetails(imdbId);
        
        return await FetchGenericCachedResponse<Movie>(
            $"trending-movies-{imdbId}", Callback); 
    }
}

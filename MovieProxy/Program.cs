using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using MovieProxy;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IMovieService, ImdbMovieService>();
builder.Services.AddMemoryCache();
var apiInfo = new ApiAInfo("MovieProxy", "v1", "https://docs.microsoft.com/en-us/aspnet/core/tutorials/web-api-help-pages");


var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/popular-movies",
    async (int? page, IMovieService movieService) => await movieService.GetPopularMovies(page));
app.MapGet("/api/recent-movies",
    async (int? page, IMovieService movieService) => await movieService.GetRecentlyAddedMovies(page));
app.MapGet("/api/random-movies",
    async (int? page, IMovieService movieService) => await movieService.GetRandomMovies(page));
app.MapGet("/api/trending-movies",
    async (int? page, IMovieService movieService) => await movieService.GetTrendingMovies(page));
app.MapGet("/api/upcoming-movies",
    async (int? page, IMovieService movieService) => await movieService.GetUpcomingMovies(page));
app.MapGet("/api/movies/{id}",
    async (string id, IMovieService movieService) => await movieService.GetMovieDetails(id));

app.MapGet("/", () => "Welcome to MovieProxy")
    .ExcludeFromDescription();

app.Run();

internal record ApiAInfo(string Name, string Version, string Documentation);
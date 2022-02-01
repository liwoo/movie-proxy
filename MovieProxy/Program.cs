using Microsoft.AspNetCore.Mvc;
using MovieProxy;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IMovieService, ImdbMovieService>();
builder.Services.AddResponseCaching();
var apiInfo = new ApiAInfo("MovieProxy", "v1", "https://docs.microsoft.com/en-us/aspnet/core/tutorials/web-api-help-pages");


var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/popular-movies",
    async (IMovieService movieService) => await movieService.GetPopularMovies());
app.MapGet("/api/recent-movies",
    async (IMovieService movieService) => await movieService.GetRecentlyAddedMovies());
app.MapGet("/api/random-movies",
    async (IMovieService movieService) => await movieService.GetRandomMovies());

app.MapGet("/", () => "Welcome to MovieProxy")
    .ExcludeFromDescription();

app.Run();

internal record ApiAInfo(string Name, string Version, string Documentation);
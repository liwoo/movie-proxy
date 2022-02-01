using Microsoft.AspNetCore.Mvc;
using MovieProxy;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IMovieService, ImdbMovieService>();
builder.Services.AddResponseCaching();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/popular-movies",
    async (IMovieService movieService) => await movieService.GetPopularMovies());
app.MapGet("/api/recent-movies",
    async (IMovieService movieService) => await movieService.GetRecentlyAddedMovies());
app.MapGet("/api/random-movies",
    async (IMovieService movieService) => await movieService.GetRandomMovies());
app.MapGet("/", () => "Movie Proxy API");

app.Run();
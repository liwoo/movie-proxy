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
app.Use(async (context, next) =>
{
    context.Response.GetTypedHeaders().CacheControl =
        new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
        {
            Public = true,
            MaxAge = TimeSpan.FromSeconds(2592000)
        };
    context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Vary] =
        new string[] {"Accept-Encoding"};

    await next();
});

app.MapGet("/api/popular-movies", async (IMovieService movieService) => await movieService.GetPopularMovies());
app.MapGet("/api/recent-movies", async (IMovieService movieService) => await movieService.GetRecentlyAddedMovies());
app.MapGet("/api/random-movies", async (IMovieService movieService) => await movieService.GetRandomMovies());

app.Run();
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Web;

namespace MovieProxy;

public record PartialMovie(string Title, string PosterUrl, string ImdbId);
public record Movie (string Title, string PosterUrl, string ImdbId, string Description, string ReleaseYear, List<string>? Genres,
    List<string>? ProducerAndCast, List<string>? Languages, int? Duration, string? Rated, IEnumerable<PartialMovie> Recommendations);
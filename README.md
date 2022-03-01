
# iMDB Movie Proxiy

This is a simple API that wraps around the publically available [iMDB Movies API](https://rapidapi.com/amrelrafie/api/movies-tvshows-data-imdb/). I use it to power a [Showmax Clone](showmax-clone ) App I build in Flutter for my [Youtube Channel](https://www.youtube.com/channel/UCNEXAX15mO3rsVQQQve6Iog). I use [.NET 6 Minimal APIs](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-6.0) for this project - enjoy it!

## Authors

- [@liwoo](https://www.github.com/liwoo)




## API Reference

#### Get all items

```http
  GET /api/popular-movies
```

| Parameter | Type     | Description                |
| :-------- | :------- | :------------------------- |
| `page` | `integer` |  Query Page, starting from 1 |

```http
  GET /api/recent-movies
```

| Parameter | Type     | Description                |
| :-------- | :------- | :------------------------- |
| `page` | `integer` |  Query Page, starting from 1 |

```http
  GET /api/random-movies
```

| Parameter | Type     | Description                |
| :-------- | :------- | :------------------------- |
| `page` | `integer` |  Query Page, starting from 1 |

```http
  GET /api/trending-movies
```

| Parameter | Type     | Description                |
| :-------- | :------- | :------------------------- |
| `page` | `integer` |  Query Page, starting from 1 |

#### Get item

```http
  GET /api/upcoming-movies
```

| Parameter | Type     | Description                |
| :-------- | :------- | :------------------------- |
| `page` | `integer` |  Query Page, starting from 1 |

```http
  GET /api/movies/:id
```

| Parameter | Type     | Description                       |
| :-------- | :------- | :-------------------------------- |
| `id` | `string` |  **Required** Movie ID |



## How to Run this Project
Please make sure you replace the following `appsettings` (typically `appsettings.Development.json`):

- `ImdbHost` - you can fetch this when you register with the API [here](https://rapidapi.com/amrelrafie/api/movies-tvshows-data-imdb/)
- `ImdbRoot` - you will get this upon registration
- `DefaultImage` - provide a link to a default image incase the movie doesn't have a Poster Listed
- `RapidApiKey` - you will get this upon registration


First install all dependencies in the project

```bash
  dotnet restore
```

Then, navigate to `./MovieProxy` and from there

```bash
  dotnet Run
```

## Contributing

Contributions are always welcome!

Be sensible! [Reach out to me](mailto:jeremiahchienda@gmail.com) when in doubt 


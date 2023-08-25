using Microsoft.AspNetCore.Mvc;
namespace web_services_l1.Controllers;
[ApiController]
[Route("[controller]")]
public class MoviesController : ControllerBase
{
    [HttpPost("UploadMovieCsv")]
    public string Post(IFormFile inputFile)
    {
        var strm = inputFile.OpenReadStream();
        byte[] buffer = new byte[inputFile.Length];
        strm.Read(buffer, 0, (int)inputFile.Length);
        string fileContent = System.Text.Encoding.Default.GetString(buffer);
        strm.Close();

        //create a list of movie to save changes only after 1000th movie

        int batchSize = 1000;
        int lines = 0;
        List<Movie> moviesToAdd = new List<Movie>();


        MoviesContext dbContext = new MoviesContext();
        bool skip_header = true;
        foreach (string line in fileContent.Split('\n'))
        {
            lines += 1;
            if (skip_header)
            {
                skip_header = false;
                continue;
            }
            var tokens = line.Split(",");
            if (tokens.Length != 3) continue;

            string MovieID = tokens[0];
            string MovieName = tokens[1];
            string[] Genres = tokens[2].Split("|");
            List<Genre> movieGenres = new List<Genre>();

            foreach (string genre in Genres)
            {
                Genre g = new Genre();
                g.Name = genre;
                if (!dbContext.Genres.Any(e => e.Name == g.Name))
                {
                    dbContext.Genres.Add(g);
                    dbContext.SaveChanges();
                }
                IQueryable<Genre> results = dbContext.Genres.Where(e => e.Name == g.Name);
                if (results.Count() > 0)
                    movieGenres.Add(results.First());
            }

            Movie m = new Movie();
            m.MovieID = int.Parse(MovieID);
            m.Title = MovieName;
            m.Genres = movieGenres;
            moviesToAdd.Add(m);

            if (!dbContext.Movies.Any(e => e.MovieID == m.MovieID) && (moviesToAdd.Count >= batchSize || (moviesToAdd.Count > 0 && lines == fileContent.Split('\n').Length - 1)))
            {
                dbContext.Movies.AddRange(moviesToAdd);
                dbContext.SaveChanges();
                moviesToAdd.Clear();
            }
        }
        return "OK";
    }

    [HttpPost("UploadRatingCsv")]
    public string PostRating(IFormFile inputFile)
    {
        var strm = inputFile.OpenReadStream();
        byte[] buffer = new byte[inputFile.Length];
        strm.Read(buffer, 0, (int)inputFile.Length);
        string fileContent = System.Text.Encoding.Default.GetString(buffer);
        strm.Close();

        //create a list of rating to save changes only after 1000th movie

        int batchSize = 1000;
        int lines = 0;
        List<Rating> ratingsToAdd = new List<Rating>();


        MoviesContext dbContext = new MoviesContext();
        bool skip_header = true;
        foreach (string line in fileContent.Split('\n'))
        {
            lines += 1;
            if (skip_header)
            {
                skip_header = false;
                continue;
            }
            var tokens = line.Split(",");
            if (tokens.Length != 4) continue;

            string UserID = tokens[0];
            string MovieID = tokens[1];
            string Rating = tokens[2];
            string Timestamp = tokens[3];


            //add users 
            User u = new User();
            u.UserID = int.Parse(UserID);
            if (!dbContext.Users.Any(e => e.UserID == u.UserID))
            {
                u.Name = "User number: " + lines;
                dbContext.Users.Add(u);
                dbContext.SaveChanges();
            }

            var rating = Rating.Split(".");

            Rating r = new Rating();
            r.RatingValue = int.Parse(rating[0]);
            r.RatedMovie = dbContext.Movies.FirstOrDefault(r => r.MovieID == int.Parse(MovieID));
            r.RatingUser = dbContext.Users.FirstOrDefault(r => r.UserID == int.Parse(UserID));
            ratingsToAdd.Add(r);

            if (ratingsToAdd.Count >= batchSize || (ratingsToAdd.Count > 0 && lines == fileContent.Split('\n').Length - 1))
            {
                dbContext.Ratings.AddRange(ratingsToAdd);
                dbContext.SaveChanges();
                ratingsToAdd.Clear();
            }
        }
        return "OK";
    }

    [HttpGet("GetGenresById/{id}")]

    public IEnumerable<Genre> GetGenresById(int id)
    {
        MoviesContext dbContext = new MoviesContext();
        return dbContext.Genres.Where(
            g => g.Movies!.Any(m => m.MovieID.Equals(id))
        );
    }

    [HttpGet("GetVectorOfGenresById/{id}")]

    public List<string> GetVectorOfGenresById(int id)
    {
        MoviesContext dbContext = new MoviesContext();
        var list1 = dbContext.Genres.Where(
            g => g.Movies!.Any(m => m.MovieID.Equals(id))
        )
        .ToList()
        .Select(g => g.Name!)
        .ToList();

        return list1;
    }

    [HttpGet("GetCosineSimilarity/{movie1Id}/{movie2Id}")]
    public double GetCosineSimilarity(int movie1Id, int movie2Id)
    {
        MoviesContext dbContext = new MoviesContext();

        var movie1Genres = dbContext.Genres.Where(
                g => g.Movies!.Any(m => m.MovieID.Equals(movie1Id))
            )
            .Select(g => g.Name!)
            .ToList();

        var movie2Genres = dbContext.Genres.Where(
                g => g.Movies!.Any(m => m.MovieID.Equals(movie2Id))
            )
            .Select(g => g.Name!)
            .ToList();

        var allGenres = new HashSet<string>(movie1Genres);
        allGenres.UnionWith(movie2Genres);

        var movie1Vector = allGenres.Select(g => movie1Genres.Contains(g) ? 1 : 0).ToArray();
        var movie2Vector = allGenres.Select(g => movie2Genres.Contains(g) ? 1 : 0).ToArray();

        var dotProduct = 0.0;
        var mag1 = 0.0;
        var mag2 = 0.0;

        for (int i = 0; i < allGenres.Count; i++)
        {
            dotProduct += movie1Vector[i] * movie2Vector[i];
            mag1 += movie1Vector[i] * movie1Vector[i];
            mag2 += movie2Vector[i] * movie2Vector[i];
        }

        var cosineSimilarity = dotProduct / (Math.Sqrt(mag1) * Math.Sqrt(mag2));

        return cosineSimilarity;
    }



    [HttpGet("GetMoviesWithSharedGenres/{id}")]

    public IEnumerable<Movie> GetMoviesWithSharedGenres(int id)
    {
        MoviesContext dbContext = new MoviesContext();

        var inputMovieGenres = dbContext.Genres.Where(
                g => g.Movies!.Any(m => m.MovieID.Equals(id))
            )
            .Select(g => g.Name!)
            .ToList();

        var moviesWithSharedGenres = dbContext.Movies.Where(
                m => m.Genres!.Any(g => inputMovieGenres.Contains(g.Name!))
            )
            .ToList();

        return moviesWithSharedGenres;
    }

    [HttpGet("GetSimilarMovies/{id}/{threshold}")]
    public IEnumerable<Movie> GetSimilarMovies(int id, double threshold)
    {

        var moviesWithSharedGenres = GetMoviesWithSharedGenres(id);

        var similarMovies = new List<Movie>();
        foreach (var movie in moviesWithSharedGenres)
        {
            var cosineSimilarity = GetCosineSimilarity(id, movie.MovieID);
            if (cosineSimilarity > threshold)
            {
                similarMovies.Add(movie);
            }
        }

        return similarMovies;
    }

    [HttpGet("GetMoviesRatedByUser/{userId}")]
    public IEnumerable<Movie> GetMoviesRatedByUser(int userId)
    {
        MoviesContext dbContext = new MoviesContext();

        var user = dbContext.Users.FirstOrDefault(u => u.UserID == userId);
        if (user == null)
        {
            return Enumerable.Empty<Movie>();
        }

        var moviesRatedByUser = dbContext.Ratings
            .Where(r => r.RatingUser.UserID == userId)
            .Select(r => r.RatedMovie)
            .Distinct()
            .ToList();

        return moviesRatedByUser;
    }

    [HttpGet("GetSortedMoviesRatedByUser/{userId}")]
    public IEnumerable<Movie> GetSortedMoviesRatedByUser(int userId)
    {
        MoviesContext dbContext = new MoviesContext();

        var user = dbContext.Users.FirstOrDefault(u => u.UserID == userId);
        if (user == null)
        {
            return Enumerable.Empty<Movie>();
        }

        var sortedMovies = dbContext.Ratings
            .Where(r => r.RatingUser.UserID == userId)
            .OrderByDescending(r => r.RatingValue)
            .Select(r => r.RatedMovie)
            .ToList();

        return sortedMovies;
    }

    [HttpGet("GetSimilarMoviesToHighestRatedByUser/{userId}/{threshold}")]
    public IEnumerable<Movie> GetSimilarMoviesToHighestRatedByUser(int userId, double threshold)
    {
        MoviesContext dbContext = new MoviesContext();

        var highestRatedMovie = dbContext.Ratings
            .Where(r => r.RatingUser.UserID == userId)
            .OrderByDescending(r => r.RatingValue)
            .Select(r => r.RatedMovie)
            .FirstOrDefault();

        if (highestRatedMovie == null)
        {
            return Enumerable.Empty<Movie>();
        }

        var highestRatedMovieGenres = dbContext.Genres
            .Where(g => g.Movies!.Any(m => m.MovieID == highestRatedMovie.MovieID))
            .Select(g => g.Name)
            .ToList();

        if (!highestRatedMovieGenres.Any())
        {
            return Enumerable.Empty<Movie>();
        }

        var moviesWithSharedGenres = dbContext.Movies
            .Where(m => m.Genres!.Any(g => highestRatedMovieGenres.Contains(g.Name)))
            .ToList();

        if (!moviesWithSharedGenres.Any())
        {
            return Enumerable.Empty<Movie>();
        }

        var similarMovies = new List<Movie>();
        foreach (var movie in moviesWithSharedGenres)
        {
            var cosineSimilarity = GetCosineSimilarity(highestRatedMovie.MovieID, movie.MovieID);
            if (cosineSimilarity > threshold)
            {
                similarMovies.Add(movie);
            }
        }

        return similarMovies;
    }

    [HttpGet("GetRecommendationsForUser/{userId}/{recommendationSize}")]
    public IEnumerable<Movie> GetRecommendationsForUser(int userId, int recommendationSize)
    {
        MoviesContext dbContext = new MoviesContext();

        var user = dbContext.Users.FirstOrDefault(u => u.UserID == userId);
        if (user == null)
        {
            return Enumerable.Empty<Movie>();
        }

        var ratedMovies = dbContext.Ratings
            .Where(r => r.RatingUser.UserID == userId)
            .Select(r => r.RatedMovie)
            .Distinct()
            .ToList();

        var similarUsers = dbContext.Ratings
            .Where(r => ratedMovies.Contains(r.RatedMovie))
            .Select(r => r.RatingUser)
            .Distinct()
            .ToList();

        var recommendedMovies = dbContext.Ratings
            .Where(r => similarUsers.Contains(r.RatingUser) && !ratedMovies.Contains(r.RatedMovie))
            .GroupBy(r => r.RatedMovie)
            .Select(g => new { Movie = g.Key, AverageRating = g.Average(r => r.RatingValue) })
            .OrderByDescending(g => g.AverageRating)
            .Select(g => g.Movie)
            .Take(recommendationSize)
            .ToList();

        return recommendedMovies;
    }

    [HttpGet("GetAllGenres")]

    public IEnumerable<Genre> GetAllGenres()
    {
        MoviesContext dbContext = new MoviesContext();
        return dbContext.Genres.AsEnumerable();
    }
    [HttpGet("GetMoviesByName/{search_phrase}")]
    public IEnumerable<Movie> GetMoviesByName(string search_phrase)
    {
        MoviesContext dbContext = new MoviesContext();
        return dbContext.Movies.Where(e => e.Title.Contains(search_phrase));
    }
    [HttpPost("GetMoviesByGenre")]
    public IEnumerable<Movie> GetMoviesByGenre(string search_phrase)
    {
        MoviesContext dbContext = new MoviesContext();
        return dbContext.Movies.Where(
        m => m.Genres.Any(p => p.Name.Contains(search_phrase))
        );
    }
}

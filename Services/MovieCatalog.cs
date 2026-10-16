using System.Globalization;
using System.Text.Json;
using CsvHelper;
using Cinematch.Models;

namespace Cinematch.Services;

public class MovieCatalog
{
    private readonly List<Movie> _movies = new();
    private readonly Dictionary<int, Movie> _byId = new();

    public MovieCatalog(IWebHostEnvironment env)
    {
        string dir = Path.Combine(env.ContentRootPath, "Data");
        var directors = LoadDirectors(Path.Combine(dir, "tmdb_5000_credits.csv"));
        LoadMovies(Path.Combine(dir, "tmdb_5000_movies.csv"), directors);
    }

    public Movie? GetById(int id) =>
        _byId.TryGetValue(id, out var movie) ? movie : null;

    // Used later by the "create a grid" page
        public List<Movie> Search(string text, int max = 30)
    {
        IEnumerable<Movie> query = _movies;

        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(m =>
                m.Title.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                m.Director.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        return query.OrderByDescending(m => m.Popularity).Take(max).ToList();
    }

    private static Dictionary<int, string> LoadDirectors(string path)
    {
        var result = new Dictionary<int, string>();

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            int id = csv.GetField<int>("movie_id");
            string crewJson = csv.GetField("crew") ?? "";
            if (string.IsNullOrWhiteSpace(crewJson)) continue;

            using var doc = JsonDocument.Parse(crewJson);
            foreach (var person in doc.RootElement.EnumerateArray())
            {
                if (person.GetProperty("job").GetString() == "Director")
                {
                    result[id] = person.GetProperty("name").GetString() ?? "";
                    break;
                }
            }
        }
        return result;
    }

    private void LoadMovies(string path, Dictionary<int, string> directors)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            int id = csv.GetField<int>("id");

            // release_date looks like "2009-12-10" (one row is empty)
            string date = csv.GetField("release_date") ?? "";
            int year = 0;
            if (date.Length >= 4) int.TryParse(date[..4], out year);

            var genres = new List<string>();
            string genresJson = csv.GetField("genres") ?? "";
            if (!string.IsNullOrWhiteSpace(genresJson))
            {
                using var doc = JsonDocument.Parse(genresJson);
                foreach (var g in doc.RootElement.EnumerateArray())
                    genres.Add(g.GetProperty("name").GetString() ?? "");
            }

            var movie = new Movie(
                id,
                csv.GetField("title") ?? "",
                year,
                directors.TryGetValue(id, out var d) ? d : "Unknown",
                genres,
                csv.GetField<double>("vote_average"),
                csv.GetField<double>("popularity"),
                csv.GetField("overview") ?? "");

            _movies.Add(movie);
            _byId[id] = movie;
        }
    }
}
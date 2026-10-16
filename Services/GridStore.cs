using System.Globalization;
using CsvHelper;
using Cinematch.Models;

namespace Cinematch.Services;

public class GridStore
{
    public const int MaxGridsPerUser = 20;
    public const int MaxGridsTotal = 2000;

    private readonly string _path;
    private readonly MovieCatalog _catalog;
    private readonly object _lock = new();
    private readonly List<UserGrid> _grids = new();

    public GridStore(IWebHostEnvironment env, MovieCatalog catalog)
    {
        _catalog = catalog;
        _path = Path.Combine(env.ContentRootPath, "Data", "grids.csv");
        if (File.Exists(_path)) Load();
    }

    // Returns null when the grid was saved, or a message explaining why it was rejected
    public string? TryAdd(UserGrid grid)
    {
        string? problem = Validate(grid);
        if (problem is not null) return problem;

        lock (_lock)
        {
            if (_grids.Count >= MaxGridsTotal)
                return "CineMatch is full right now. Please try again later.";

            var mine = _grids.Where(g => SameUser(g.User, grid.User)).ToList();
            if (mine.Count >= MaxGridsPerUser)
                return $"You can have up to {MaxGridsPerUser} grids.";

            var newSet = grid.MovieIds.OrderBy(id => id).ToList();
            bool duplicate = mine.Any(g =>
                g.Topic == grid.Topic && g.MovieIds.OrderBy(id => id).SequenceEqual(newSet));
            if (duplicate) return "You already submitted this exact grid.";

            _grids.Add(grid);
            try
            {
                Save();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _grids.Remove(grid);   // keep memory and file in agreement
                return "Could not save your grid. Please try again.";
            }
            return null;
        }
    }

    // Picks a random grid, avoiding the one currently shown if possible
    public UserGrid? PickRandom(string? excludeId = null)
    {
        lock (_lock)
        {
            var pool = _grids.Where(g => g.Id != excludeId).ToList();
            if (pool.Count == 0) pool = _grids.ToList();
            return pool.Count == 0 ? null : pool[Random.Shared.Next(pool.Count)];
        }
    }

    // All grids by one user, newest first
    public List<UserGrid> GetByUser(string handle)
    {
        lock (_lock)
        {
            return _grids
                .Where(g => SameUser(g.User, handle))
                .OrderByDescending(g => g.CreatedUtc)
                .ToList();
        }
    }

    public UserGrid? Latest()
    {
        lock (_lock)
        {
            return _grids.OrderByDescending(g => g.CreatedUtc).FirstOrDefault();
        }
    }

    private static bool SameUser(string a, string b) =>
        string.Equals(a.TrimStart('@'), b.TrimStart('@'), StringComparison.OrdinalIgnoreCase);

    private string? Validate(UserGrid g)
    {
        if (g.MovieIds.Count != 9) return "A grid needs exactly 9 films.";
        if (g.MovieIds.Distinct().Count() != 9) return "Each film can only be used once.";
        if (g.MovieIds.Any(id => _catalog.GetById(id) is null)) return "One of the films was not found.";

        int nameLength = g.User.TrimStart('@').Length;
        if (nameLength == 0 || nameLength > 30) return "Handle must be 1 to 30 characters.";

        return null;
    }

    private void Load()
    {
        using var reader = new StreamReader(_path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        if (!csv.Read()) return;   // empty file
        csv.ReadHeader();

        while (csv.Read())
        {
            try
            {
                var ids = new List<int>();
                foreach (var part in (csv.GetField("MovieIds") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(part, out int movieId)) ids.Add(movieId);
                }

                if (!DateTime.TryParse(csv.GetField("CreatedUtc"), CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out DateTime created))
                    created = DateTime.MinValue;

                var grid = new UserGrid(
                    csv.GetField("Id") ?? "",
                    csv.GetField("User") ?? "",
                    csv.GetField("Topic") ?? "",
                    ids,
                    created);

                // Rows that don't pass the same checks as new grids are skipped
                if (Validate(grid) is null) _grids.Add(grid);
            }
            catch (CsvHelperException)
            {
                // A broken row (e.g. a missing column) is skipped instead of crashing the app
            }
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        string temp = _path + ".tmp";

        // Write to a temp file first...
        using (var writer = new StreamWriter(temp))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            foreach (var header in new[] { "Id", "User", "Topic", "MovieIds", "CreatedUtc" })
                csv.WriteField(header);
            csv.NextRecord();

            foreach (var g in _grids)
            {
                csv.WriteField(g.Id);
                csv.WriteField(g.User);
                csv.WriteField(g.Topic);
                csv.WriteField(string.Join(";", g.MovieIds));
                csv.WriteField(g.CreatedUtc.ToString("o"));
                csv.NextRecord();
            }
        }   // file is closed here

        // ...then swap it in, so a crash can never leave a half-written grids.csv
        File.Move(temp, _path, overwrite: true);
    }
}
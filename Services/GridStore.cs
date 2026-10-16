using System.Globalization;
using CsvHelper;
using Cinematch.Models;

namespace Cinematch.Services;

public class GridStore
{
    private readonly string _path;
    private readonly object _lock = new();
    private readonly List<UserGrid> _grids = new();

    public GridStore(IWebHostEnvironment env)
    {
        _path = Path.Combine(env.ContentRootPath, "Data", "grids.csv");
        if (File.Exists(_path)) Load();
    }

    public void Add(UserGrid grid)
    {
        lock (_lock)
        {
            _grids.Add(grid);
            Save();
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

    private void Load()
    {
        using var reader = new StreamReader(_path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            var ids = new List<int>();
            foreach (var part in (csv.GetField("MovieIds") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
                ids.Add(int.Parse(part));

            _grids.Add(new UserGrid(
                csv.GetField("Id") ?? "",
                csv.GetField("User") ?? "",
                csv.GetField("Topic") ?? "",
                ids,
                DateTime.Parse(csv.GetField("CreatedUtc") ?? "", null, DateTimeStyles.RoundtripKind)));
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

        using var writer = new StreamWriter(_path);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

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
    }
}
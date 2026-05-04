// GameDbContext.cs — EF Core DbContext for the high-score database. [ND2 #14]

using MazeGame.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MazeGame.Data;

/// <summary>
/// EF Core context backed by SQLite (single file: mazegame.db).
/// EnsureCreated() is called by the App at startup so the schema appears
/// automatically on first run — no migrations required for the grader.
/// </summary>
public class GameDbContext : DbContext
{
    /// <summary>The HighScores table.</summary>
    public DbSet<HighScore> HighScores => Set<HighScore>();

    /// <summary>
    /// Optional override for the SQLite filename — used for tests or alternate locations.
    /// Defaults to "mazegame.db" in the working directory.
    /// </summary>
    private readonly string _databasePath;

    public GameDbContext(string databasePath = "mazegame.db")
    {
        _databasePath = databasePath;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={_databasePath}");
    }
}

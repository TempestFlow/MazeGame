// HighScore.cs — POCO entity persisted to SQLite via EF Core. [ND2 #14]

namespace MazeGame.Data.Entities;

/// <summary>
/// A single high-score record. Stored in the HighScores table inside mazegame.db.
/// EF Core treats Id as the primary key by convention.
/// </summary>
public class HighScore
{
    /// <summary>Auto-generated primary key.</summary>
    public int Id { get; set; }

    /// <summary>Name the player typed at the game-over / victory screen.</summary>
    public string PlayerName { get; set; } = "";

    /// <summary>The highest level the player reached during this run.</summary>
    public int LevelReached { get; set; }

    /// <summary>Computed score: (level * 1000) + (HP * 10) + (kills * 50).</summary>
    public int Score { get; set; }

    /// <summary>Timestamp the run ended (UTC).</summary>
    public DateTime AchievedAt { get; set; }

    /// <summary>True if the player won; false if they died.</summary>
    public bool Victory { get; set; }
}

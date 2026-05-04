// InvalidLevelException.cs — Thrown when LevelLoader is asked for a level that doesn't exist. [ND2 #5]

namespace MazeGame.Core.Exceptions;

/// <summary>
/// Raised by <see cref="Services.LevelLoader.LoadLevel"/> when the requested
/// level number is missing from the template dictionary. Carries the bad
/// level number so callers can decide how to recover (fall back to level 1,
/// log, etc.).
/// </summary>
public class InvalidLevelException : Exception
{
    /// <summary>The level number that was requested but not found.</summary>
    public int LevelNumber { get; }

    public InvalidLevelException(int levelNumber, string message) : base(message)
    {
        LevelNumber = levelNumber;
    }

    public InvalidLevelException(int levelNumber, string message, Exception innerException)
        : base(message, innerException)
    {
        LevelNumber = levelNumber;
    }
}

// DatabaseException.cs — Wraps EF Core / SQLite errors so the App layer sees one type. [ND2 #5]

namespace MazeGame.Core.Exceptions;

/// <summary>
/// Thrown by the data layer (HighScoreRepository) when an underlying EF Core
/// or SQLite operation fails. Always wraps the original exception so the
/// stack trace is preserved for debugging.
/// </summary>
public class DatabaseException : Exception
{
    public DatabaseException(string message) : base(message) { }

    public DatabaseException(string message, Exception innerException)
        : base(message, innerException) { }
}

// GameLogger.cs — A static utility class for managing game messages.
// Demonstrates: [#16] params keyword, [#13] System.Collections.Generic (Queue).
// Provides a centralized message log that the renderer reads each frame.

namespace MazeGame.Core.Utils;

/// <summary>
/// Static utility class that maintains a global message log.
/// Game systems write messages here (e.g., "Picked up key!"), and the
/// renderer reads them to display in the HUD area.
/// Uses a Queue to keep only the most recent messages.
/// </summary>
public static class GameLogger
{
    /// <summary>Maximum number of messages to retain in the log.</summary>
    private const int MaxMessages = 5;

    /// <summary>
    /// [#13] Queue from System.Collections.Generic — stores recent messages
    /// in FIFO order. Oldest messages are dequeued when the limit is reached.
    /// </summary>
    private static readonly Queue<string> _messages = new();

    /// <summary>
    /// [#16] params keyword — accepts any number of message strings.
    /// Adds each message to the log and trims old messages if needed.
    /// </summary>
    /// <param name="messages">One or more messages to log.</param>
    public static void Log(params string[] messages)
    {
        foreach (var msg in messages)
        {
            _messages.Enqueue(msg);

            // Keep the log from exceeding the maximum size
            while (_messages.Count > MaxMessages)
            {
                _messages.Dequeue();
            }
        }
    }

    /// <summary>
    /// Returns all current messages as an array for rendering.
    /// </summary>
    /// <returns>An array of the most recent log messages.</returns>
    public static string[] GetRecentMessages()
    {
        return _messages.ToArray();
    }

    /// <summary>
    /// Clears all messages from the log (e.g., when starting a new level).
    /// </summary>
    public static void Clear()
    {
        _messages.Clear();
    }
}

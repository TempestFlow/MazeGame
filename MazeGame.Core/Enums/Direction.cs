// Direction.cs — Defines the four cardinal movement directions plus a "no movement" state.
// Used throughout the game for player movement, enemy AI, and attack facing.

namespace MazeGame.Core.Enums;

/// <summary>
/// Represents the four cardinal directions a character can face or move.
/// None indicates no movement or a neutral facing direction.
/// </summary>
public enum Direction
{
    /// <summary>No direction / stationary.</summary>
    None,

    /// <summary>Upward (decreasing Y).</summary>
    Up,

    /// <summary>Downward (increasing Y).</summary>
    Down,

    /// <summary>Leftward (decreasing X).</summary>
    Left,

    /// <summary>Rightward (increasing X).</summary>
    Right
}

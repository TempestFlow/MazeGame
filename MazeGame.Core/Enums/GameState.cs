// GameState.cs — Tracks the overall state of the game.
// Used by GameEngine to control which logic runs each frame.

namespace MazeGame.Core.Enums;

/// <summary>
/// Represents the possible states of the game at any given moment.
/// The game engine uses this to decide what to update and render.
/// </summary>
public enum GameState
{
    /// <summary>The game is showing the main menu / title screen.</summary>
    MainMenu,

    /// <summary>The game is actively being played.</summary>
    Playing,

    /// <summary>The player has died and the game is over.</summary>
    GameOver,

    /// <summary>The player has completed all levels and won.</summary>
    Victory
}

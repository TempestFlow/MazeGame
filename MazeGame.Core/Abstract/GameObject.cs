// GameObject.cs — Abstract base class for all objects in the game world.
// Demonstrates: [#9] Abstract class.
// Every entity in the game (Player, Enemy, Key, Door, Potion) inherits
// from this class, which provides shared properties and an abstract Update method.

using MazeGame.Core.Models;

namespace MazeGame.Core.Abstract;

/// <summary>
/// Abstract base class that all game objects inherit from.
/// Provides a position on the grid, a visual symbol for rendering,
/// an active flag for removal, and an abstract Update method that
/// each subclass must implement for per-frame logic.
/// </summary>
public abstract class GameObject
{
    /// <summary>
    /// The current position of this object on the game grid.
    /// </summary>
    public Position Position { get; set; }

    /// <summary>
    /// The ASCII character used to render this object on screen.
    /// Each subclass sets its own symbol (e.g., '@' for Player, 'E' for Enemy).
    /// </summary>
    public char Symbol { get; protected set; }

    /// <summary>
    /// Whether this object is still active in the game world.
    /// When set to false, the object is removed on the next update cycle
    /// (e.g., a picked-up key or a defeated enemy).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Creates a new GameObject at the specified position with the given symbol.
    /// </summary>
    /// <param name="position">Starting grid position.</param>
    /// <param name="symbol">The ASCII character representing this object.</param>
    protected GameObject(Position position, char symbol)
    {
        Position = position;
        Symbol = symbol;
    }

    /// <summary>
    /// Called once per game frame to update this object's state.
    /// Each subclass implements its own logic (e.g., enemy AI movement,
    /// or no-op for static items like keys).
    /// </summary>
    /// <param name="world">The current game world, providing access to the map and other objects.</param>
    public abstract void Update(GameWorld world);

    /// <summary>
    /// Returns the character to render for this object.
    /// Can be overridden if the visual changes based on state.
    /// </summary>
    public virtual char Render()
    {
        return Symbol;
    }
}

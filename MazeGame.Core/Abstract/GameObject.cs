// GameObject.cs — Abstract base for every entity in the world. ND1: [#9] abstract class.

using MazeGame.Core.Models;

namespace MazeGame.Core.Abstract;

/// <summary>
/// Base class for Player, Enemy, Key, Door, Potion, etc. Provides the
/// common surface (position, render symbol, active flag) and forces every
/// subclass to define its own per-frame Update behavior.
/// </summary>
public abstract class GameObject
{
    /// <summary>Current grid position.</summary>
    public Position Position { get; set; }

    /// <summary>ASCII glyph used to render this object.</summary>
    public char Symbol { get; protected set; }

    /// <summary>Whether the object is still part of the world. Set false to remove.</summary>
    public bool IsActive { get; set; } = true;

    protected GameObject(Position position, char symbol)
    {
        Position = position;
        Symbol = symbol;
    }

    /// <summary>Called once per game tick — subclasses define behavior here.</summary>
    public abstract void Update(GameWorld world);

    /// <summary>Render glyph for this object — overridable when state changes the visual.</summary>
    public virtual char Render() => Symbol;
}

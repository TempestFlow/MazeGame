// Key.cs — A collectible key that the player needs to unlock doors.
// Demonstrates: [#1] Custom interface implementation (IInteractable).
// When the player walks onto a key, they collect it and can then open doors.

using MazeGame.Core.Abstract;
using MazeGame.Core.Interfaces;

namespace MazeGame.Core.Models;

/// <summary>
/// A key item that the player must collect to unlock the door and
/// advance to the next level. Implements IInteractable so the game
/// engine can trigger the pickup when the player walks over it.
/// </summary>
public class Key : GameObject, IInteractable
{
    /// <summary>
    /// Message displayed when the player picks up the key.
    /// </summary>
    public string InteractionMessage => "You picked up a key!";

    /// <summary>
    /// Creates a new Key at the given position.
    /// The 'K' symbol is used to render keys on the map.
    /// </summary>
    public Key(Position position) : base(position, 'K')
    {
    }

    /// <summary>
    /// Called when the player walks onto this key.
    /// Gives the player the key and deactivates this item (removes it from the map).
    /// </summary>
    public void OnInteract(Player player)
    {
        // Give the key to the player
        player.CollectKey();
        // Remove the key from the game world
        IsActive = false;
    }

    /// <summary>
    /// Keys are static items — they don't move or change between frames.
    /// </summary>
    public override void Update(GameWorld world)
    {
        // Keys are static — no per-frame logic needed
    }
}

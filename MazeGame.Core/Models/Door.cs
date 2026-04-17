// Door.cs — A locked door that requires a key to pass through.
// Demonstrates: [#1] Custom interface implementation (IInteractable).
// The door is the exit point for each level — the player must have a key to open it.

using MazeGame.Core.Abstract;
using MazeGame.Core.Interfaces;

namespace MazeGame.Core.Models;

/// <summary>
/// A door that leads to the next level. The player must have collected
/// a key before they can open it. Implements IInteractable for the
/// game engine's interaction system.
/// </summary>
public class Door : GameObject, IInteractable
{
    /// <summary>Whether this door has been unlocked by the player.</summary>
    public bool IsUnlocked { get; private set; }

    /// <summary>
    /// Message displayed when the player interacts with the door.
    /// Changes based on whether the player has a key.
    /// </summary>
    public string InteractionMessage =>
        IsUnlocked ? "Door opened! Proceeding to next level..." : "The door is locked. Find a key!";

    /// <summary>
    /// Creates a new locked Door at the given position.
    /// The 'D' symbol is used to render doors on the map.
    /// </summary>
    public Door(Position position) : base(position, 'D')
    {
        IsUnlocked = false;
    }

    /// <summary>
    /// Called when the player walks into the door.
    /// If the player has a key, the door unlocks and the key is consumed.
    /// If not, the player is told they need a key.
    /// </summary>
    public void OnInteract(Player player)
    {
        if (player.HasKey)
        {
            // Consume the key and unlock the door
            player.UseKey();
            IsUnlocked = true;
        }
        // If no key, the door stays locked (message is shown via InteractionMessage)
    }

    /// <summary>
    /// Doors are static objects — no per-frame logic needed.
    /// </summary>
    public override void Update(GameWorld world)
    {
        // Doors don't move or change on their own
    }
}

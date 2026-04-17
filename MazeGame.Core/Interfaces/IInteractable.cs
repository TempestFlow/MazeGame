// IInteractable.cs — Custom interface for objects the player can interact with.
// Demonstrates: [#1] Custom interface.
// Implemented by Key, Door, and Potion to define what happens when
// the player walks into or activates these objects.

namespace MazeGame.Core.Interfaces;

/// <summary>
/// Interface for game objects that the player can interact with.
/// When the player moves onto or activates an interactable object,
/// the game calls <see cref="OnInteract"/> to trigger its effect.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Called when the player interacts with this object.
    /// Each implementor defines its own behavior (e.g., collecting a key,
    /// opening a door, or healing the player).
    /// </summary>
    /// <param name="player">The player who triggered the interaction.</param>
    void OnInteract(Models.Player player);

    /// <summary>
    /// A short message describing what happens when the player interacts
    /// with this object (displayed in the HUD message log).
    /// </summary>
    string InteractionMessage { get; }
}

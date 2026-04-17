// Potion.cs — A health potion (elixir) that restores the player's HP.
// Demonstrates: [#1] Custom interface implementation (IInteractable),
//               [#15] Default arguments (healAmount has a default value).

using MazeGame.Core.Abstract;
using MazeGame.Core.Interfaces;

namespace MazeGame.Core.Models;

/// <summary>
/// A health potion that restores HP when the player walks over it.
/// The heal amount can be customized per potion — stronger potions
/// can appear in later levels.
/// </summary>
public class Potion : GameObject, IInteractable
{
    /// <summary>How much health this potion restores when consumed.</summary>
    public int HealAmount { get; }

    /// <summary>
    /// Message displayed when the player picks up this potion.
    /// </summary>
    public string InteractionMessage => $"Drank a potion! Restored {HealAmount} HP.";

    /// <summary>
    /// Creates a new Potion at the given position.
    /// Uses [#15] default argument — healAmount defaults to 25 if not specified.
    /// </summary>
    /// <param name="position">Grid position of the potion.</param>
    /// <param name="healAmount">HP restored when consumed (default: 25).</param>
    public Potion(Position position, int healAmount = 25) : base(position, '+')
    {
        HealAmount = healAmount;
    }

    /// <summary>
    /// Called when the player walks onto this potion.
    /// Heals the player and removes the potion from the map.
    /// </summary>
    public void OnInteract(Player player)
    {
        // Restore health (capped at max by Player.Heal)
        player.Heal(HealAmount);
        // Remove the potion from the game world
        IsActive = false;
    }

    /// <summary>
    /// Potions are static items — they sit on the ground until picked up.
    /// </summary>
    public override void Update(GameWorld world)
    {
        // Potions don't move or change on their own
    }
}

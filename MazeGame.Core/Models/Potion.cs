// Potion.cs — Health-restoring pickup. ND1: [#1] IInteractable, [#15] default arg.

using MazeGame.Core.Abstract;
using MazeGame.Core.Interfaces;

namespace MazeGame.Core.Models;

/// <summary>
/// A health potion. Restores HP when the player walks onto it; removes itself
/// from the world after use. Heal amount is per-instance so later levels can
/// drop stronger potions.
/// </summary>
public class Potion : GameObject, IInteractable
{
    /// <summary>HP restored when this potion is consumed.</summary>
    public int HealAmount { get; }

    public string InteractionMessage => $"Drank a potion! Restored {HealAmount} HP.";

    /// <summary>[#15] Default argument — most potions heal 25 HP.</summary>
    public Potion(Position position, int healAmount = 25) : base(position, '+')
    {
        HealAmount = healAmount;
    }

    public void OnInteract(Player player)
    {
        player.Heal(HealAmount);
        IsActive = false;
    }

    public override void Update(GameWorld world)
    {
        // Potions are static — nothing to tick.
    }
}

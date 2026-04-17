// Player.cs — The player character (partial class, part 1: properties and formatting).
// Demonstrates: [#4] IFormattable, [#8] Partial class.
// This file contains the player's stats, properties, and string formatting logic.
// Combat and movement logic is in Player.Combat.cs (the other partial file).

using MazeGame.Core.Abstract;
using MazeGame.Core.Enums;

namespace MazeGame.Core.Models;

/// <summary>
/// Represents the player character in the game.
/// This is a partial class — properties and formatting are here,
/// while combat and movement logic live in Player.Combat.cs.
/// Implements IFormattable to support multiple display formats:
///   "S" = short summary, "F" = full details, "H" = health bar.
/// </summary>
public partial class Player : GameObject, IFormattable
{
    // ---------------------------------------------------------------
    // Player stats and properties
    // ---------------------------------------------------------------

    /// <summary>Current health points. The player dies when this reaches 0.</summary>
    public int Health { get; set; }

    /// <summary>Maximum health points (cap for healing).</summary>
    public int MaxHealth { get; set; }

    /// <summary>Damage dealt per attack swing.</summary>
    public int AttackPower { get; set; }

    /// <summary>The current dungeon level the player is on (1-based).</summary>
    public int Level { get; set; }

    /// <summary>Whether the player is currently carrying a key to unlock doors.</summary>
    public bool HasKey { get; set; }

    /// <summary>The direction the player is currently facing (for attacks).</summary>
    public Direction Facing { get; set; }

    /// <summary>
    /// Event that fires when the player's health reaches zero.
    /// The game engine subscribes to this to trigger Game Over.
    /// </summary>
    public event Action? OnDeath;

    /// <summary>
    /// Event that fires when the player picks up an item or performs an action.
    /// Carries a message string for the HUD log.
    /// </summary>
    public event Action<string>? OnAction;

    /// <summary>
    /// Creates a new Player at the given position with default stats.
    /// The '@' symbol is the player's visual representation on the map.
    /// </summary>
    /// <param name="position">Starting position on the grid.</param>
    public Player(Position position) : base(position, '@')
    {
        Health = 100;
        MaxHealth = 100;
        AttackPower = 15;
        Level = 1;
        HasKey = false;
        Facing = Direction.Down;
    }

    // ---------------------------------------------------------------
    // [#4] IFormattable — multiple string format options
    // ---------------------------------------------------------------

    /// <summary>
    /// Formats the player's status into a string based on the format specifier:
    ///   "S" — Short summary:  "Player HP:80"
    ///   "F" — Full details:   "Player [Lv.2] HP:80/100 ATK:15 Pos:(3,5) Key:Yes"
    ///   "H" — Health bar:     "[========  ] 80/100"
    ///   null/other — defaults to short summary.
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        // Choose the output format based on the format specifier
        return format?.ToUpperInvariant() switch
        {
            "S" => $"Player HP:{Health}",
            "F" => $"Player [Lv.{Level}] HP:{Health}/{MaxHealth} ATK:{AttackPower} Pos:{Position} Key:{(HasKey ? "Yes" : "No")}",
            "H" => FormatHealthBar(),
            _   => $"Player HP:{Health}"  // default to short format
        };
    }

    /// <summary>
    /// Builds a visual health bar string like "[========  ] 80/100".
    /// The bar width is 10 characters, filled proportionally to current health.
    /// </summary>
    private string FormatHealthBar()
    {
        // Calculate how many of the 10 bar segments to fill
        int barWidth = 10;
        int filled = (int)((double)Health / MaxHealth * barWidth);
        // Clamp to valid range in case of edge cases
        filled = Math.Clamp(filled, 0, barWidth);

        // Build the bar: filled segments are '=', empty segments are ' '
        string bar = new string('=', filled) + new string(' ', barWidth - filled);
        return $"[{bar}] {Health}/{MaxHealth}";
    }

    /// <summary>
    /// Default ToString returns the short format.
    /// </summary>
    public override string ToString()
    {
        return ToString("S", null);
    }
}

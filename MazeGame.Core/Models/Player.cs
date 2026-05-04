// Player.cs — Player character (partial part 1: stats and formatting).
// ND1: [#4] IFormattable, [#8] partial class.
// ND2: [ND2 #11] ICloneable.

using MazeGame.Core.Abstract;
using MazeGame.Core.Enums;

namespace MazeGame.Core.Models;

/// <summary>
/// The player character. Properties and string formatting live here;
/// movement / combat / heal / key logic is in <c>Player.Combat.cs</c>.
/// Implements:
///   [#4]  IFormattable — "S" short, "F" full, "H" health-bar formats.
///   [ND2 #11] ICloneable — deep stat-copy used as a checkpoint snapshot.
/// </summary>
public partial class Player : GameObject, IFormattable, ICloneable
{
    /// <summary>Current HP. Death fires when this reaches 0.</summary>
    public int Health { get; set; }

    /// <summary>Maximum HP (cap for healing).</summary>
    public int MaxHealth { get; set; }

    /// <summary>Damage dealt per attack.</summary>
    public int AttackPower { get; set; }

    /// <summary>Current dungeon level (1-based).</summary>
    public int Level { get; set; }

    /// <summary>Whether the player currently carries a key.</summary>
    public bool HasKey { get; set; }

    /// <summary>Direction the player is currently facing (for attacks).</summary>
    public Direction Facing { get; set; }

    /// <summary>Fires when health hits 0. GameEngine subscribes to trigger Game Over.</summary>
    public event Action? OnDeath;

    /// <summary>Fires for player-driven actions; carries an HUD message string.</summary>
    public event Action<string>? OnAction;

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
    // [#4] IFormattable
    // ---------------------------------------------------------------

    /// <summary>
    /// Formats the player as text:
    ///   "S" → "Player HP:80"
    ///   "F" → "Player [Lv.2] HP:80/100 ATK:15 Pos:(3,5) Key:Yes"
    ///   "H" → "[========  ] 80/100"
    ///   null/other → "S".
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return format?.ToUpperInvariant() switch
        {
            "S" => $"Player HP:{Health}",
            "F" => $"Player [Lv.{Level}] HP:{Health}/{MaxHealth} ATK:{AttackPower} Pos:{Position} Key:{(HasKey ? "Yes" : "No")}",
            "H" => FormatHealthBar(),
            _   => $"Player HP:{Health}"
        };
    }

    private string FormatHealthBar()
    {
        int barWidth = 10;
        int filled = (int)((double)Health / MaxHealth * barWidth);
        filled = Math.Clamp(filled, 0, barWidth);
        string bar = new string('=', filled) + new string(' ', barWidth - filled);
        return $"[{bar}] {Health}/{MaxHealth}";
    }

    public override string ToString() => ToString("S", null);

    // ---------------------------------------------------------------
    // [ND2 #11] ICloneable — deep snapshot of player stats
    // ---------------------------------------------------------------

    /// <summary>
    /// [ND2 #11] Returns a stat-copy of this Player. Position is a value
    /// type, so it copies by assignment. Events are intentionally not
    /// transferred — the clone is a frozen snapshot, not a live actor.
    /// Used by GameEngine as a per-level checkpoint.
    /// </summary>
    public object Clone()
    {
        return new Player(Position)
        {
            Health = Health,
            MaxHealth = MaxHealth,
            AttackPower = AttackPower,
            Level = Level,
            HasKey = HasKey,
            Facing = Facing,
        };
    }
}

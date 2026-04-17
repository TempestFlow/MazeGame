# Prompt for Claude Code: C# Maze Game Project

## Project Overview

Build a **console-based maze game** in C# (.NET 8+). The game is a top-down Zelda-style dungeon crawler rendered with ASCII characters in the terminal. The player navigates through multiple maze levels, collects keys to unlock doors, picks up health potions (elixirs), and fights enemies with a sword that appears in front of the player (like in classic Zelda games).

**IMPORTANT: Leave plenty of comments throughout all code files. Every class, method, property, interface, enum, and non-trivial block of logic should have a clear comment explaining what it does and why. Use XML doc comments (`///`) for public members and regular comments (`//`) for inline logic. Be generous with comments — this is a university project and readability matters.**

---

## Game Design

### Core Gameplay
- The player (`@`) moves with WASD or arrow keys through a maze made of walls (`#`), floors (`.`), doors (`D`), and keys (`K`).
- To pass to the next level, the player must pick up a key and then walk into a door.
- Enemies (`E`) roam the maze. If the player is adjacent to an enemy and presses the attack key (Space), a sword symbol appears in front of the player and damages the enemy.
- Elixirs/potions (`+`) restore player health when picked up.
- The game has at least 3 levels with increasing difficulty (more enemies, more complex mazes).
- The game ends when the player dies (health reaches 0) or completes all levels.
- Display a HUD (heads-up display) showing: current level, health, attack power, whether the player has a key.

### Visual Representation (Console)
- `@` = Player
- `#` = Wall
- `.` = Floor
- `D` = Door (locked)
- `K` = Key
- `+` = Health potion / elixir
- `E` = Enemy
- `†` or `-`, `|` = Sword attack visual (depending on direction)

---

## Solution Structure (MUST have multiple assemblies)

The solution **must** consist of more than one project (assembly):

```
MazeGame.sln
├── MazeGame.Core/          (Class Library - all game logic, models, interfaces)
│   ├── Interfaces/
│   ├── Abstract/
│   ├── Models/
│   ├── Enums/
│   ├── Services/
│   └── Utils/
└── MazeGame.App/           (Console Application - entry point, game loop, rendering)
    └── Program.cs
```

`MazeGame.App` must reference `MazeGame.Core`.

---

## Mandatory C# Feature Requirements

Every single requirement below **must** be implemented. Each one is marked with a point value — do not skip any.

### 1. Custom Interface (0.5 pts)
Create and apply your own interface. Example: `IInteractable` with a method like `void OnInteract(Player player)` — implemented by Key, Door, Potion classes.

### 2. IComparable<T> (0.5 pts)
Implement `IComparable<T>` on a class. Example: `Enemy` implements `IComparable<Enemy>` to compare enemies by their difficulty/power level. Demonstrate usage by sorting a list of enemies.

### 3. IEquatable<T> (0.5 pts)
Implement `IEquatable<T>` on a class. Example: `Position` struct/class implements `IEquatable<Position>` to compare grid coordinates. Override `Equals()` and `GetHashCode()` properly.

### 4. IFormattable (1 pt)
Implement `IFormattable` on a class. Example: `Player` implements `IFormattable` — format string `"S"` shows short summary (`"Player HP:100"`), `"F"` shows full details (`"Player [Lv.2] HP:100/100 ATK:15 Pos:(3,5) Key:Yes"`), `"H"` shows health bar. Demonstrate by calling `player.ToString("F", null)`.

### 5. Switch with 'when' keyword (0.5 pts)
Use a `switch` statement or expression with `when` guard clauses. Example: in enemy AI logic, switch on enemy type with `when` conditions based on distance to player or health thresholds.

### 6. Range type (0.5 pts)
Use the `Range` type (`..` syntax). Example: when loading a portion of the map data, slicing arrays for level segments, or extracting a sub-array of tiles.

### 7. Multiple assemblies (1 pt)
Already covered by the solution structure above — `MazeGame.Core` and `MazeGame.App` are separate projects/assemblies.

### 8. Sealed or Partial class (0.5 pts)
Use a `sealed` class or a `partial` class. Example: `sealed class BossEnemy : Enemy` (cannot be further inherited), or split `Player` into `partial class Player` across two files (one for combat logic, one for movement).

### 9. Abstract class (0.5 pts)
Use an abstract class. Example: `abstract class GameObject` with abstract method `abstract void Update()` — inherited by `Player`, `Enemy`, `Potion`, `Key`, etc.

### 10. Static constructor (1 pt)
Use a static constructor. Example: `LevelLoader` has a `static LevelLoader()` that initializes default level templates or loads configuration data once. Or an `EnemyFactory` with a static constructor that populates a static dictionary of enemy types.

### 11. Deconstructor (0.5 pts)
Implement a `Deconstruct` method. Example: `Position` has `void Deconstruct(out int x, out int y)` so you can write `var (x, y) = position;`.

### 12. Operator overloading (0.5 pts)
Overload operators. Example: `Position` overloads `+` and `-` for vector math (`position + direction`), or `==` and `!=` for comparison.

### 13. System.Collections / System.Collections.Generic (1 pt)
Use data structures from these namespaces. Example: `List<Enemy>` for enemies on a level, `Dictionary<Position, GameObject>` for the game map, `Queue<string>` for message log, `Stack<Level>` for level history.

### 14. 'is' operator (0.5 pts)
Use the `is` operator for type checking. Example: `if (gameObject is Enemy enemy)` when checking what the player collided with.

### 15. Default and named arguments (0.5 pts)
Use default parameter values and named arguments. Example: `void SpawnEnemy(int x, int y, int health = 50, string name = "Goblin")` called as `SpawnEnemy(3, 5, name: "Skeleton", health: 80)`.

### 16. 'params' keyword (0.5 pts)
Use `params` in a method signature. Example: `void LogMessages(params string[] messages)` or `void AddItems(params GameObject[] items)`.

### 17. Initialization with 'out' arguments (1 pt)
Use `out` parameters for initialization. Example: `bool TryGetGameObject(Position pos, out GameObject obj)` pattern, or `bool TryParseCommand(string input, out Direction direction)`.

### 18. Delegates or lambda functions (1.5 pts)
Use delegates and/or lambda functions. Example: `Action<Player> onLevelComplete` callback, `Func<Enemy, bool>` predicate for filtering enemies, event handlers with lambdas like `player.OnDeath += () => Console.WriteLine("Game Over!");`, or LINQ queries with lambdas.

### 19. Bitwise operations (1 pt)
Use bitwise operations meaningfully. Example: use a `[Flags] enum TileFlags` with values like `None = 0, Walkable = 1, HasItem = 2, HasEnemy = 4, Visible = 8` and manipulate them with `|`, `&`, `^`, `~`. Use this for tile properties or player status effects.

### 20. Null-conditional and null-coalescing operators: ?. ?[] ?? ??= (0.5 pts)
Use these operators. Example: `currentEnemy?.TakeDamage(10)`, `inventory?[0]?.Name ?? "Empty"`, `cachedPath ??= FindPath(start, end)`.

### 21. Pattern matching (1 pt)
Use pattern matching beyond simple `is`. Example: use property patterns, positional patterns, or type patterns in switch expressions:
```csharp
string desc = gameObject switch
{
    Enemy { Health: <= 0 } => "Dead enemy",
    Enemy { IsBoss: true } e when e.Health < 50 => "Wounded boss",
    Potion p => $"Potion (+{p.HealAmount} HP)",
    Key => "A shiny key",
    _ => "Unknown object"
};
```

---

## Implementation Notes

- Use **Console.SetCursorPosition** and **Console.Clear** (or partial redraws) for rendering the maze. Avoid flickering by only redrawing changed tiles if possible.
- Keep the game loop simple: Input → Update → Render.
- Enemy AI can be simple: random movement or move toward player if within range.
- Maze layouts can be hardcoded as `char[,]` or `string[]` arrays — no need for procedural generation (but feel free to add it).
- Use proper OOP design — don't put everything in one file or one class.
- Make sure the game compiles and runs without errors.

---

## Summary Checklist

Before finishing, verify every item is present:

- [ ] Custom interface created and applied
- [ ] IComparable<T> implemented and used
- [ ] IEquatable<T> implemented with Equals + GetHashCode
- [ ] IFormattable implemented with multiple format strings
- [ ] switch with when keyword
- [ ] Range type usage (..)
- [ ] Solution has 2+ projects (assemblies)
- [ ] sealed or partial class used
- [ ] Abstract class used
- [ ] Static constructor present
- [ ] Deconstruct method implemented
- [ ] Operator overloading (+, -, ==, etc.)
- [ ] Collections from System.Collections.Generic used
- [ ] 'is' operator used for type checking
- [ ] Default and named arguments used
- [ ] params keyword used
- [ ] out parameter initialization used
- [ ] Delegates/lambdas/events used
- [ ] Bitwise operations with [Flags] enum
- [ ] ?. ?? ??= operators used
- [ ] Pattern matching (property/positional patterns)
- [ ] **Generous comments on all classes, methods, and logic blocks**

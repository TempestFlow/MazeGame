# Prompt for Claude Code: C# Maze Game Project — ND2 Extension

## Context

This is **ND2 (homework 2)** for the existing MazeGame project. The project already exists with two assemblies (`MazeGame.Core` and `MazeGame.App`) and implements all ND1 requirements. **Do NOT rewrite or break existing code.** Your job is to **extend** the project with new features that satisfy the ND2 requirements below.

Read the existing codebase first (especially `MazeGame.Core/Models/`, `MazeGame.Core/Services/`, and `Program.cs`) to understand the current architecture before adding anything.

**IMPORTANT: Continue the existing commenting style — leave plenty of comments on every new class, method, property, and non-trivial logic block. Use XML doc comments (`///`) for public members and regular comments (`//`) for inline logic. Mark each ND2 requirement with a `[ND2 #N]` tag in comments so the grader can find them easily.**

---

## Feature Scope

Keep changes **minimal** — only add what's needed to satisfy the requirements. The single new gameplay feature is:

**High Score system** — when the player wins or dies, they enter their name, and their score is saved to a SQLite database. Scores can be viewed from the main menu.

Score formula: `score = (currentLevel × 1000) + (playerHealth × 10) + (enemiesDefeated × 50)`.

Everything else (generics, iterators, extensions, exceptions) should be woven into the existing code naturally, not added as standalone demos.

---

## Project Structure Changes

Add a **third assembly** for the data layer (this is good practice and keeps EF concerns separate):

```
MazeGame.sln
├── MazeGame.Core/          (existing — game logic)
├── MazeGame.App/           (existing — console UI, references Core and Data)
└── MazeGame.Data/          (NEW — Entity Framework, database, repositories)
    ├── Entities/
    │   └── HighScore.cs
    ├── GameDbContext.cs
    └── HighScoreRepository.cs
```

`MazeGame.App` references both `MazeGame.Core` and `MazeGame.Data`. `MazeGame.Data` references `MazeGame.Core` only if needed (probably not — keep it independent).

---

## ND2 Requirements (every single one MUST be implemented)

### [ND2 #1] IEnumerable<T> implementation (1 pt)

Make `GameWorld` (in `MazeGame.Core/Models/GameWorld.cs`) implement `IEnumerable<GameObject>`. The enumeration should yield: the player first, then all active enemies, then all active items.

This lets you write:
```csharp
foreach (var obj in world)
{
    Console.WriteLine(CombatService.DescribeObject(obj));
}
```

### [ND2 #2] IEnumerator<T> implementation (1 pt)

Create your own custom enumerator class — **do not use `yield return` for this one**. Create `MazeGame.Core/Utils/GameObjectEnumerator.cs`:

```csharp
public class GameObjectEnumerator : IEnumerator<GameObject>
{
    // Manually implement Current, MoveNext(), Reset(), Dispose()
    // Iterates through player → enemies → items
}
```

`GameWorld.GetEnumerator()` returns an instance of `GameObjectEnumerator`. This explicitly satisfies both #1 and #2 separately.

### [ND2 #3] Iterator with yield return (0.5 pts)

Add a method to `GameWorld` that uses `yield return`:

```csharp
public IEnumerable<Position> GetWalkableNeighbors(Position pos)
{
    // yield return each adjacent walkable tile (up, down, left, right)
}
```

Or:
```csharp
public IEnumerable<Enemy> GetEnemiesInRange(Position center, int range)
{
    foreach (var enemy in Enemies)
        if (enemy.IsActive && enemy.Position.ManhattanDistance(center) <= range)
            yield return enemy;
}
```

Implement at least one such iterator method and use it somewhere in the code (e.g., in enemy AI or combat).

### [ND2 #4] Extending C# types — extension methods (0.5 pts)

Create `MazeGame.Core/Utils/Extensions.cs` with extension methods that extend built-in or existing types. Examples:

```csharp
public static class Extensions
{
    // Extend string
    public static string Truncate(this string s, int maxLength) => ...;
    
    // Extend ConsoleKey
    public static bool IsMovementKey(this ConsoleKey key) => 
        key is ConsoleKey.W or ConsoleKey.A or ConsoleKey.S or ConsoleKey.D 
            or ConsoleKey.UpArrow or ConsoleKey.DownArrow or ConsoleKey.LeftArrow or ConsoleKey.RightArrow;
    
    // Extend Position (existing struct)
    public static bool IsAdjacentTo(this Position p, Position other) => 
        p.ManhattanDistance(other) == 1;
}
```

Use these extensions in actual code (e.g., `key.IsMovementKey()` in `Program.cs` or `GameEngine.ProcessInput`).

### [ND2 #5] Custom exception types (1 pt)

Create at least 2 custom exception classes in `MazeGame.Core/Exceptions/`:

```csharp
public class InvalidLevelException : Exception
{
    public int LevelNumber { get; }
    public InvalidLevelException(int levelNumber, string message) : base(message)
    {
        LevelNumber = levelNumber;
    }
}

public class DatabaseException : Exception
{
    public DatabaseException(string message, Exception innerException) 
        : base(message, innerException) { }
}
```

Throw `InvalidLevelException` in `LevelLoader.LoadLevel()` when an invalid level number is requested. Throw `DatabaseException` from the repository when EF Core operations fail.

### [ND2 #6] try/catch blocks where errors can occur (1 pt)

Add `try`/`catch` in places where exceptions are realistically possible:
- `LevelLoader.LoadLevel()` — catch and rethrow as `InvalidLevelException`
- `HighScoreRepository` methods — catch EF exceptions and wrap them in `DatabaseException`
- Database initialization in `Program.cs` — catch failures gracefully and warn the user (don't crash the game if DB is unavailable)
- Reading player name input — handle `null` or empty input

Example:
```csharp
try
{
    await _context.HighScores.AddAsync(score);
    await _context.SaveChangesAsync();
}
catch (Exception ex)
{
    throw new DatabaseException("Failed to save high score.", ex);
}
```

### [ND2 #7] Custom generic type (1 pt)

Create a custom generic class. Recommended: `MazeGame.Core/Utils/GameCollection.cs`:

```csharp
public class GameCollection<T> : IEnumerable<T>
{
    private readonly List<T> _items = new();
    
    public void Add(T item) { ... }
    public bool Remove(T item) { ... }
    public T this[int index] => _items[index];
    public int Count => _items.Count;
    
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

Make it actually used somewhere — e.g., `GameWorld` could use `GameCollection<Enemy>` instead of (or alongside) `List<Enemy>`. Or add it as a new field used by the engine.

### [ND2 #8] Generic type with `where` constraint (1 pt)

Create another generic class (or method/extend the previous one) with a `where` constraint. Recommended:

```csharp
// In MazeGame.Core/Utils/SortedGameCollection.cs
public class SortedGameCollection<T> where T : IComparable<T>
{
    private readonly List<T> _items = new();
    
    public void Add(T item)
    {
        _items.Add(item);
        _items.Sort();  // works because T : IComparable<T>
    }
    
    public T? Highest => _items.Count > 0 ? _items[^1] : default;
    public T? Lowest => _items.Count > 0 ? _items[0] : default;
}
```

Use this in `GameEngine` to maintain a sorted collection of enemies (since `Enemy : IComparable<Enemy>` from ND1). 

You can also constrain it with `where T : GameObject` for a different generic class, demonstrating both kinds of constraints.

### [ND2 #9] Generic extension method (1 pt)

In `Extensions.cs`, add at least one **generic** extension method:

```csharp
public static T? RandomElement<T>(this IEnumerable<T> source, Random rng)
{
    var list = source.ToList();
    return list.Count == 0 ? default : list[rng.Next(list.Count)];
}

public static IEnumerable<T> WhereActive<T>(this IEnumerable<T> source) where T : GameObject
{
    return source.Where(item => item.IsActive);
}
```

Use these in actual code (e.g., enemy AI picks a `RandomElement` from walkable neighbors, or `world.Enemies.WhereActive()` in `GameEngine`).

### [ND2 #10] Extension deconstructor (1 pt)

Add a `Deconstruct` extension method that adds deconstruction support to a type that doesn't natively have it. Since `KeyValuePair<TKey, TValue>` already has one in modern .NET, find another candidate:

```csharp
public static class Extensions
{
    // Extension deconstructor for HighScore entity
    public static void Deconstruct(this HighScore score, out string name, out int level, out int total)
    {
        name = score.PlayerName;
        level = score.LevelReached;
        total = score.Score;
    }
    
    // OR: extension deconstructor for tuple-like extraction
    public static void Deconstruct(this Enemy enemy, out string name, out int hp, out int atk)
    {
        name = enemy.Name;
        hp = enemy.Health;
        atk = enemy.AttackPower;
    }
}
```

Use it: `var (name, level, total) = highScore;` or `var (name, hp, atk) = enemy;` somewhere in the code.

### [ND2 #11] ICloneable implementation (1 pt)

Implement `ICloneable` on a class — recommended: `Player`. The clone should be a deep copy:

```csharp
public partial class Player : GameObject, IFormattable, ICloneable
{
    public object Clone()
    {
        var clone = new Player(this.Position)
        {
            Health = this.Health,
            MaxHealth = this.MaxHealth,
            AttackPower = this.AttackPower,
            Level = this.Level,
            HasKey = this.HasKey,
            Facing = this.Facing
        };
        return clone;
    }
}
```

Use it: `GameEngine` could clone the player at the start of each level as a "checkpoint" snapshot for stats tracking.

### [ND2 #12] Events in the project (1 pt)

Already partially satisfied by ND1's events (`OnDeath`, `OnAction`, `OnLevelComplete`, etc.). Add at least one **new event** specifically for ND2:

```csharp
// In GameEngine.cs
public event Action<int>? OnEnemyDefeated;  // int = enemy difficulty
public event Action<HighScore>? OnHighScoreSaved;
```

Wire them up — fire `OnEnemyDefeated` whenever an enemy's `IsActive` flips to false in combat. Fire `OnHighScoreSaved` after a successful database write.

Make sure to mark these in comments as `[ND2 #12]`.

### [ND2 #13] LINQ usage (1 pt)

Use LINQ queries somewhere meaningful. Multiple places are fine:

```csharp
// Get top 10 high scores
var topScores = await _context.HighScores
    .OrderByDescending(s => s.Score)
    .Take(10)
    .ToListAsync();

// Count active enemies of a type
int bossCount = world.Enemies.Count(e => e.IsBoss && e.IsActive);

// Group enemies by name
var grouped = world.Enemies
    .Where(e => e.IsActive)
    .GroupBy(e => e.Name)
    .Select(g => new { Name = g.Key, Count = g.Count() });

// Average enemy health on level
double avgHp = world.Enemies.Where(e => e.IsActive).Average(e => e.Health);
```

Use LINQ both for the EF queries (high scores) AND for in-memory game data (enemies, items).

### [ND2 #14] Database with Entity Framework Core (3 pts)

This is the biggest piece. Use **Entity Framework Core with SQLite** so the database is a single file (`mazegame.db`) — no server setup required.

**Steps:**

1. In `MazeGame.Data/MazeGame.Data.csproj`, add NuGet packages:
   - `Microsoft.EntityFrameworkCore`
   - `Microsoft.EntityFrameworkCore.Sqlite`

2. Create `MazeGame.Data/Entities/HighScore.cs`:
```csharp
public class HighScore
{
    public int Id { get; set; }
    public string PlayerName { get; set; } = "";
    public int LevelReached { get; set; }
    public int Score { get; set; }
    public DateTime AchievedAt { get; set; }
    public bool Victory { get; set; }  // true = won, false = died
}
```

3. Create `MazeGame.Data/GameDbContext.cs`:
```csharp
public class GameDbContext : DbContext
{
    public DbSet<HighScore> HighScores => Set<HighScore>();
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=mazegame.db");
    }
}
```

4. Create `MazeGame.Data/HighScoreRepository.cs` with methods:
   - `Task SaveScoreAsync(HighScore score)` — adds and saves
   - `Task<List<HighScore>> GetTopScoresAsync(int count = 10)` — uses LINQ + EF
   - `Task<int> GetTotalGamesPlayedAsync()` — count
   - All methods wrapped in try/catch that throws `DatabaseException`

5. In `Program.cs` (App), at startup:
   - Create the DB context, call `context.Database.EnsureCreated()` to auto-create the schema
   - Wrap in try/catch — if DB fails, show a warning but let the game run anyway

6. Add menu options in the main menu / game over screens:
   - "View High Scores" — fetches top 10 and displays them
   - On game end (death or victory) — prompt for player name, save score

**The `mazegame.db` file should be created in the working directory automatically on first run.** Add it to `.gitignore`.

---

## Existing Code Touchpoints (where to integrate)

- **`GameWorld.cs`** — add `IEnumerable<GameObject>` implementation, `yield return` iterator method.
- **`Player.cs`** — add `ICloneable` implementation.
- **`GameEngine.cs`** — add new event(s), use LINQ on enemies, wire up high-score saving on game end.
- **`LevelLoader.cs`** — throw `InvalidLevelException` for bad level numbers.
- **`Program.cs`** — initialize DB, add high-score menu, prompt for name on game over/victory, use `key.IsMovementKey()` extension.
- **`Renderer.cs`** — add a method `DrawHighScores(List<HighScore> scores)` to display the leaderboard.

---

## Implementation Notes

- **Don't break ND1 features.** Run a mental check at the end: do all 21 ND1 requirements still work?
- **Use `async`/`await`** for all EF operations — that's the modern .NET way.
- **The DB file** (`mazegame.db`) must be created automatically with `EnsureCreated()` so the grader doesn't have to run migrations.
- **Match the existing code style** — same indentation, same comment density, same XML doc style.
- **Keep `MazeGame.Data` independent of UI concerns** — no `Console.WriteLine` in there.
- **Don't add new gameplay features** beyond high scores — this is about C# language requirements, not game design.

---

## Summary Checklist (ND2)

Before finishing, verify every item:

- [ ] [ND2 #1] `IEnumerable<T>` implemented on a class (e.g., `GameWorld`)
- [ ] [ND2 #2] Custom `IEnumerator<T>` class implemented manually (no `yield`)
- [ ] [ND2 #3] At least one method with `yield return`
- [ ] [ND2 #4] Extension methods on existing C# types
- [ ] [ND2 #5] At least 2 custom exception classes used
- [ ] [ND2 #6] try/catch blocks in realistic places (DB ops, level loading, input)
- [ ] [ND2 #7] Custom generic type created and used
- [ ] [ND2 #8] Generic type with `where` constraint
- [ ] [ND2 #9] Generic extension method created and used
- [ ] [ND2 #10] Extension deconstructor (`Deconstruct` extension method)
- [ ] [ND2 #11] `ICloneable` implemented (e.g., on `Player`)
- [ ] [ND2 #12] At least one new event added and fired
- [ ] [ND2 #13] LINQ used in multiple places (EF queries + in-memory)
- [ ] [ND2 #14] EF Core + SQLite + `HighScore` entity + repository + auto-created DB
- [ ] All ND1 requirements still work (no regressions)
- [ ] **Generous comments on all new code, with `[ND2 #N]` tags**
- [ ] `mazegame.db` added to `.gitignore`

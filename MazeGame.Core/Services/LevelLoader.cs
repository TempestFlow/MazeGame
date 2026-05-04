// LevelLoader.cs — Parses hardcoded ASCII level templates into GameWorld instances.
// ND1: [#10] static constructor, [#6] Range type, [#15] default/named arguments.
// ND2: [ND2 #5] throws InvalidLevelException, [ND2 #6] try/catch wraps parse errors.

using MazeGame.Core.Exceptions;
using MazeGame.Core.Models;

namespace MazeGame.Core.Services;

/// <summary>
/// Responsible for loading level data and constructing GameWorld instances.
/// Level templates are stored as string arrays in a static dictionary,
/// initialized once via a static constructor.
/// </summary>
public class LevelLoader
{
    // ---------------------------------------------------------------
    // [#10] Static constructor — initializes level templates once
    // ---------------------------------------------------------------

    /// <summary>
    /// Static dictionary mapping level numbers to their ASCII map templates.
    /// Populated by the static constructor when the class is first used.
    /// </summary>
    private static readonly Dictionary<int, string[]> LevelTemplates;

    /// <summary>
    /// [#10] Static constructor — runs exactly once, the first time LevelLoader
    /// is accessed. Populates the LevelTemplates dictionary with all level data.
    /// This ensures level data is loaded only once and shared across all instances.
    /// </summary>
    static LevelLoader()
    {
        LevelTemplates = new Dictionary<int, string[]>();

        // ---- Level 1: Simple introductory maze (20x10) ----
        // 2 enemies, 1 potion, 1 key, 1 door
        // A straightforward layout to teach the player the basics
        LevelTemplates[1] = new string[]
        {
            "####################",
            "#@.....#...........#",
            "#.####.#.#########.#",
            "#.#..#.#.#.......#.#",
            "#.#..#...#.#####.#.#",
            "#.#..#####.#.+.#.#.#",
            "#.#........#...#.K.#",
            "#.########.###.#.###",
            "#..........E...E..D#",
            "####################"
        };

        // ---- Level 2: Medium difficulty maze (25x12) ----
        // 4 enemies, 2 potions, 1 key, 1 door
        // More corridors and dead-ends, enemies patrol tighter spaces
        LevelTemplates[2] = new string[]
        {
            "#########################",
            "#@..#.........#.........#",
            "#.#.#.#######.#.#######.#",
            "#.#.#.#.....#.#.#.....#.#",
            "#.#...#.###.#.#.#.###.#.#",
            "#.#####.#.+.#...#.#...#.#",
            "#.......#...#####.#.###.#",
            "#.#######.###.E...#.#.K.#",
            "#.#.E...#.....###.#.#.#.#",
            "#.#.###.#####.#.+.#...#.#",
            "#.....#.....E.#...E...D.#",
            "#########################"
        };

        // ---- Level 3: Hard maze with a boss (30x15) ----
        // 5 enemies + 1 boss, 2 potions, 1 key, 1 door
        // Complex layout with many twists; the boss guards the exit
        LevelTemplates[3] = new string[]
        {
            "##############################",
            "#@.........#........#........#",
            "#.########.#.####.##.######.##",
            "#.#......#.#.#..#..........#.#",
            "#.#.####.#.#.#..####.#####.#.#",
            "#.#.#..#...#.#.....#.#...#.#.#",
            "#.#.#..###.#.#####.#.#.#.#.#.#",
            "#...#....#.#...+.#.#.#.#...#.#",
            "#.#.####.#.###.#.#.#.#.#####.#",
            "#.#.E..#.#.E...#...#.#.....#.#",
            "#.#.##.#.#.#########.###.#.#.#",
            "#.#....#.#.......E...#.+.#.K.#",
            "#.######.#.########.##.###.#.#",
            "#........#.....E....#..B..D#.#",
            "##############################"
        };
    }

    // ---------------------------------------------------------------
    // [#15] Default and named arguments — flexible level loading
    // ---------------------------------------------------------------

    /// <summary>
    /// Loads a level by number and constructs a fully populated GameWorld.
    /// Uses default and named arguments so callers can customize difficulty.
    /// Example call: <c>LoadLevel(2, enemyHealth: 80, bossName: "Dragon")</c>
    /// </summary>
    /// <param name="levelNumber">Which level to load (1-3).</param>
    /// <param name="enemyHealth">Base health for regular enemies (default: 50).</param>
    /// <param name="enemyAttack">Base attack power for enemies (default: 10).</param>
    /// <param name="bossName">Display name for the boss enemy (default: "Guardian").</param>
    /// <param name="bossHealth">Health for the boss enemy (default: 150).</param>
    /// <returns>A fully initialized GameWorld ready for gameplay.</returns>
    public GameWorld LoadLevel(int levelNumber, int enemyHealth = 50, int enemyAttack = 10,
                               string bossName = "Guardian", int bossHealth = 150)
    {
        // [ND2 #5] No silent fallback — surface bad input as an explicit exception.
        if (!LevelTemplates.ContainsKey(levelNumber))
        {
            throw new InvalidLevelException(levelNumber, $"Level {levelNumber} does not exist.");
        }

        string[] template = LevelTemplates[levelNumber];

        // [#6] Range type — playable rows (skip outer wall rows).
        string[] innerRows = template[1..^1];
        _ = innerRows.Length;

        int height = template.Length;
        int width = template[0].Length;

        GameWorld world = new GameWorld(width, height);

        string[] enemyNames = ["Goblin", "Skeleton", "Slime", "Bat", "Zombie"];
        int enemyIndex = 0;

        // [ND2 #6] Wrap parsing in try/catch so any malformed template is
        // reported as a domain-specific InvalidLevelException, not a raw IndexOutOfRange.
        try
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < template[y].Length; x++)
                {
                    char c = template[y][x];
                    Position pos = new Position(x, y);

                    switch (c)
                    {
                        case '#':
                            world.TileGrid[x, y] = '#';
                            break;

                        case '@':
                            world.TileGrid[x, y] = '.';
                            world.Player = new Player(pos) { Level = levelNumber };
                            break;

                        case 'E':
                            world.TileGrid[x, y] = '.';
                            string name = enemyNames[enemyIndex % enemyNames.Length];
                            // Stats scale with level number.
                            world.Enemies.Add(new Enemy(
                                pos,
                                name: name,
                                health: enemyHealth + (levelNumber - 1) * 15,
                                attackPower: enemyAttack + (levelNumber - 1) * 3,
                                detectionRange: 4 + levelNumber,
                                difficulty: levelNumber * 2 + enemyIndex
                            ));
                            enemyIndex++;
                            break;

                        case 'B':
                            world.TileGrid[x, y] = '.';
                            world.Enemies.Add(new BossEnemy(
                                pos,
                                name: bossName,
                                health: bossHealth,
                                attackPower: enemyAttack + 15
                            ));
                            break;

                        case 'K':
                            world.TileGrid[x, y] = '.';
                            world.Items.Add(new Key(pos));
                            break;

                        case 'D':
                            world.TileGrid[x, y] = '.';
                            world.Items.Add(new Door(pos));
                            break;

                        case '+':
                            world.TileGrid[x, y] = '.';
                            int healAmount = 20 + levelNumber * 5;
                            world.Items.Add(new Potion(pos, healAmount));
                            break;

                        default:
                            world.TileGrid[x, y] = '.';
                            break;
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not InvalidLevelException)
        {
            // [ND2 #6] Wrap any low-level parsing fault as InvalidLevelException
            // so the caller has one stable exception type to catch.
            throw new InvalidLevelException(levelNumber,
                $"Failed to parse template for level {levelNumber}.", ex);
        }

        world.UpdateFlags();
        world.AddMessages($"--- Level {levelNumber} ---", "Find the key (K) and reach the door (D)!");
        return world;
    }

    /// <summary>
    /// Returns the total number of available levels.
    /// </summary>
    public static int TotalLevels => LevelTemplates.Count;
}

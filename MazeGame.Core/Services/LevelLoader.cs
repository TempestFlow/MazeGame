// LevelLoader.cs — Loads and parses hardcoded level templates into GameWorld instances.
// Demonstrates: [#10] Static constructor, [#6] Range type (..), [#15] Default and named arguments.
// Contains 3 levels of increasing difficulty with hardcoded ASCII map layouts.

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
        // Fall back to level 1 if the requested level doesn't exist
        if (!LevelTemplates.ContainsKey(levelNumber))
        {
            levelNumber = 1;
        }

        string[] template = LevelTemplates[levelNumber];

        // ---------------------------------------------------------------
        // [#6] Range type (..) — extract a portion of the template
        // ---------------------------------------------------------------
        // Use Range syntax to get the playable rows (skip first and last wall rows)
        // This demonstrates the Range type even though we process the full template below
        string[] innerRows = template[1..^1];
        int innerRowCount = innerRows.Length;

        // Determine map dimensions from the template
        int height = template.Length;
        int width = template[0].Length;

        // Create the game world with the correct dimensions
        GameWorld world = new GameWorld(width, height);

        // Enemy names cycle through this list for variety
        string[] enemyNames = ["Goblin", "Skeleton", "Slime", "Bat", "Zombie"];
        int enemyIndex = 0;

        // Parse the template character by character to populate the world
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < template[y].Length; x++)
            {
                char c = template[y][x];
                Position pos = new Position(x, y);

                // Decide what to place based on the template character
                switch (c)
                {
                    case '#':
                        // Wall tile — not walkable
                        world.TileGrid[x, y] = '#';
                        break;

                    case '@':
                        // Player spawn point — place floor and create the player
                        world.TileGrid[x, y] = '.';
                        world.Player = new Player(pos) { Level = levelNumber };
                        break;

                    case 'E':
                        // Enemy spawn point — place floor and create an enemy
                        world.TileGrid[x, y] = '.';
                        string name = enemyNames[enemyIndex % enemyNames.Length];
                        // Scale enemy difficulty with the level number
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
                        // Boss spawn point — place floor and create a boss enemy
                        world.TileGrid[x, y] = '.';
                        world.Enemies.Add(new BossEnemy(
                            pos,
                            name: bossName,
                            health: bossHealth,
                            attackPower: enemyAttack + 15
                        ));
                        break;

                    case 'K':
                        // Key spawn point — place floor and create a key item
                        world.TileGrid[x, y] = '.';
                        world.Items.Add(new Key(pos));
                        break;

                    case 'D':
                        // Door spawn point — place the door character and create a door object
                        world.TileGrid[x, y] = '.';
                        world.Items.Add(new Door(pos));
                        break;

                    case '+':
                        // Potion spawn point — place floor and create a potion
                        world.TileGrid[x, y] = '.';
                        // Later levels get stronger potions
                        int healAmount = 20 + levelNumber * 5;
                        world.Items.Add(new Potion(pos, healAmount));
                        break;

                    default:
                        // Floor tile or any other character
                        world.TileGrid[x, y] = '.';
                        break;
                }
            }
        }

        // Initialize the tile flags based on the loaded map
        world.UpdateFlags();

        // Log a level-start message
        world.AddMessages($"--- Level {levelNumber} ---", "Find the key (K) and reach the door (D)!");

        return world;
    }

    /// <summary>
    /// Returns the total number of available levels.
    /// </summary>
    public static int TotalLevels => LevelTemplates.Count;
}

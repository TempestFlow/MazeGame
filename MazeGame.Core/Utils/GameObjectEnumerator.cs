// GameObjectEnumerator.cs — Hand-written IEnumerator<GameObject> over a GameWorld. [ND2 #2]
// Iterates: player → active enemies → active items.
// No 'yield' is used here on purpose — the manual implementation is the point.

using System.Collections;
using MazeGame.Core.Abstract;
using MazeGame.Core.Models;

namespace MazeGame.Core.Utils;

/// <summary>
/// [ND2 #2] Manually implemented enumerator. Walks the GameWorld in a fixed order:
/// the player first, then each active enemy, then each active item. Skips inactive
/// entities so callers see only "live" objects.
/// </summary>
public class GameObjectEnumerator : IEnumerator<GameObject>
{
    private readonly GameWorld _world;

    // _phase: 0 = before-player, 1 = enemies, 2 = items, 3 = done.
    private int _phase;
    private int _index;
    private GameObject? _current;

    public GameObjectEnumerator(GameWorld world)
    {
        _world = world;
        Reset();
    }

    public GameObject Current => _current
        ?? throw new InvalidOperationException("Enumerator is positioned before the first element or after the last element.");

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        // Phase 0: yield the player exactly once.
        if (_phase == 0)
        {
            _phase = 1;
            _index = 0;
            if (_world.Player != null)
            {
                _current = _world.Player;
                return true;
            }
        }

        // Phase 1: walk active enemies.
        if (_phase == 1)
        {
            while (_index < _world.Enemies.Count)
            {
                var enemy = _world.Enemies[_index++];
                if (enemy.IsActive)
                {
                    _current = enemy;
                    return true;
                }
            }
            _phase = 2;
            _index = 0;
        }

        // Phase 2: walk active items.
        if (_phase == 2)
        {
            while (_index < _world.Items.Count)
            {
                var item = _world.Items[_index++];
                if (item.IsActive)
                {
                    _current = item;
                    return true;
                }
            }
            _phase = 3;
        }

        _current = null;
        return false;
    }

    public void Reset()
    {
        _phase = 0;
        _index = 0;
        _current = null;
    }

    public void Dispose()
    {
        // No unmanaged resources, but the interface requires the method.
    }
}

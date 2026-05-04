// SortedGameCollection.cs — Generic collection that auto-sorts by IComparable<T>. [ND2 #8]

namespace MazeGame.Core.Utils;

/// <summary>
/// [ND2 #8] Generic type with a <c>where T : IComparable&lt;T&gt;</c>
/// constraint. Each Add re-sorts the backing list so Highest/Lowest are
/// constant-time lookups. Used by GameEngine to keep an active-enemy
/// roster sorted by difficulty (Enemy implements IComparable&lt;Enemy&gt;
/// from ND1 #2).
/// </summary>
public class SortedGameCollection<T> where T : IComparable<T>
{
    private readonly List<T> _items = new();

    /// <summary>Number of items currently held.</summary>
    public int Count => _items.Count;

    /// <summary>The item that compares largest, or default(T) when empty.</summary>
    public T? Highest => _items.Count > 0 ? _items[^1] : default;

    /// <summary>The item that compares smallest, or default(T) when empty.</summary>
    public T? Lowest => _items.Count > 0 ? _items[0] : default;

    public void Add(T item)
    {
        _items.Add(item);
        _items.Sort();
    }

    public bool Remove(T item) => _items.Remove(item);

    public void Clear() => _items.Clear();

    public IReadOnlyList<T> AsReadOnly() => _items.AsReadOnly();
}

// GameCollection.cs — A small custom generic collection used by the engine. [ND2 #7]

using System.Collections;

namespace MazeGame.Core.Utils;

/// <summary>
/// [ND2 #7] Custom generic type. Thin wrapper around <see cref="List{T}"/>
/// that exposes a focused surface (Add / Remove / indexer / Count) and
/// implements <see cref="IEnumerable{T}"/> so it composes with LINQ.
/// </summary>
public class GameCollection<T> : IEnumerable<T>
{
    private readonly List<T> _items = new();

    /// <summary>Number of items currently held.</summary>
    public int Count => _items.Count;

    /// <summary>Read-only indexer.</summary>
    public T this[int index] => _items[index];

    public void Add(T item) => _items.Add(item);

    public bool Remove(T item) => _items.Remove(item);

    public void Clear() => _items.Clear();

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

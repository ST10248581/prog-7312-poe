using System.Collections;

namespace SmartX.Api.Data.Collections;

/// <summary>
/// A fixed-capacity circular buffer: the custom collection behind the telemetry
/// pipeline's "most recent N" windows.
/// <para>
/// Telemetry is an unbounded stream, but the dashboard only ever asks about its
/// tail — the last 24 points of a sparkline, the newest reading, the last ten
/// ingest batches. A <see cref="List{T}"/> keeps everything and must be scanned
/// or sorted to find that tail; a ring keeps exactly the window it was sized for.
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Add"/> is O(1) and never allocates: once full, the oldest item is overwritten in place.</description></item>
///   <item><description>Memory is fixed at construction, so a chatty sensor cannot grow its window without bound.</description></item>
///   <item><description>Indexing and <see cref="Latest"/> are O(1); enumeration runs oldest to newest.</description></item>
/// </list>
/// Not thread-safe: callers that share a buffer across threads must lock around it.
/// </summary>
public sealed class RingBuffer<T> : IReadOnlyList<T>
{
    private readonly T[] _items;

    // Index the next Add writes to. Once the buffer is full this is also the
    // position of the oldest item, because that is the slot about to be reused.
    private int _head;
    private int _count;

    // Bumped on every mutation so an enumerator can detect a buffer that changed
    // underneath it, matching the contract of the BCL collections.
    private int _version;

    public RingBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _items = new T[capacity];
    }

    /// <summary>Maximum number of items held before the oldest is overwritten.</summary>
    public int Capacity => _items.Length;

    /// <summary>Number of items currently held, never more than <see cref="Capacity"/>.</summary>
    public int Count => _count;

    public bool IsFull => _count == _items.Length;

    /// <summary>The <paramref name="index"/>th item, counting from the oldest (0).</summary>
    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _items[PhysicalIndex(index)];
        }
    }

    /// <summary>
    /// Appends an item. When the buffer is full the oldest item is evicted and
    /// returned through <paramref name="evicted"/>; otherwise nothing is evicted.
    /// </summary>
    public bool Add(T item, out T? evicted)
    {
        var wasFull = IsFull;
        evicted = wasFull ? _items[_head] : default;

        _items[_head] = item;
        _head = (_head + 1) % _items.Length;

        if (!wasFull)
        {
            _count++;
        }

        _version++;
        return wasFull;
    }

    /// <summary>Appends an item, overwriting the oldest one when the buffer is full.</summary>
    public void Add(T item) => Add(item, out _);

    /// <summary>The newest item, or default when the buffer is empty.</summary>
    public T? Latest => _count == 0 ? default : _items[(_head - 1 + _items.Length) % _items.Length];

    /// <summary>The oldest item still held, or default when the buffer is empty.</summary>
    public T? Oldest => _count == 0 ? default : _items[PhysicalIndex(0)];

    /// <summary>
    /// Copies the newest <paramref name="take"/> items into a new list, oldest
    /// first. Costs O(take), independent of how much telemetry the sensor has sent.
    /// </summary>
    public List<T> TakeLatest(int take)
    {
        var size = Math.Clamp(take, 0, _count);
        var result = new List<T>(size);

        for (var index = _count - size; index < _count; index++)
        {
            result.Add(_items[PhysicalIndex(index)]);
        }

        return result;
    }

    /// <summary>The items newest first, without allocating a reversed copy.</summary>
    public IEnumerable<T> NewestFirst()
    {
        var version = _version;
        for (var index = _count - 1; index >= 0; index--)
        {
            EnsureUnchanged(version);
            yield return _items[PhysicalIndex(index)];
        }
    }

    public void Clear()
    {
        // Release references so evicted items can be collected.
        Array.Clear(_items);
        _head = 0;
        _count = 0;
        _version++;
    }

    public IEnumerator<T> GetEnumerator()
    {
        var version = _version;
        for (var index = 0; index < _count; index++)
        {
            EnsureUnchanged(version);
            yield return _items[PhysicalIndex(index)];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Maps a logical position (0 = oldest) to its slot in the backing array.</summary>
    private int PhysicalIndex(int logicalIndex)
    {
        var start = IsFull ? _head : 0;
        return (start + logicalIndex) % _items.Length;
    }

    private void EnsureUnchanged(int version)
    {
        if (version != _version)
        {
            throw new InvalidOperationException("The ring buffer was modified during enumeration.");
        }
    }
}

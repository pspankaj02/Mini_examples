using System.Collections;

namespace KiranaHub.DSA.DataStructures;

// =========================================================================================
// [KYA KARTA HAI]:
// Fixed-size Circular Buffer (Ring Buffer).
// Delivery rider ke live GPS coordinates (latitude, longitude, timestamp) ko continuous stream me store karne ke liye.
//
// [KYU ZAROORI HAI]:
// Rider ka phone har 1 second me GPS coordinate bhejta hai. Agar standard list me add karte rahein,
// toh memory unlimited grow hogi aur purana data delete karte waqt O(N) array copy penalty lagegi.
// Ring buffer fixed memory slot me rehta hai aur purane data ko automatically overwrite karta hai O(1) me!
//
// [KAISE CHALTA HAI]:
// `_head` aur `_tail` pointers modulo arithmetic (`(index + 1) % capacity`) se circular move karte hain.
// Zero memory reallocations, zero garbage collection pressure!
// =========================================================================================
public class CircularBuffer<T> : IEnumerable<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _tail;
    private int _count;

    public int Capacity => _buffer.Length;
    public int Count => _count;
    public bool IsFull => _count == _buffer.Length;
    public bool IsEmpty => _count == 0;

    public CircularBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentException("Capacity must be greater than zero.", nameof(capacity));
        _buffer = new T[capacity];
        _head = 0;
        _tail = 0;
        _count = 0;
    }

    // O(1) Enqueue - overwrites oldest item when full
    public void Enqueue(T item)
    {
        _buffer[_tail] = item;
        _tail = (_tail + 1) % _buffer.Length;

        if (_count == _buffer.Length)
        {
            // Overwrote oldest item; move head forward
            _head = (_head + 1) % _buffer.Length;
        }
        else
        {
            _count++;
        }
    }

    // O(1) Dequeue - removes oldest item
    public T Dequeue()
    {
        if (IsEmpty) throw new InvalidOperationException("Circular buffer is empty!");

        T item = _buffer[_head];
        _buffer[_head] = default!;
        _head = (_head + 1) % _buffer.Length;
        _count--;
        return item;
    }

    // Read oldest to newest
    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _count; i++)
        {
            int index = (_head + i) % _buffer.Length;
            yield return _buffer[index];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


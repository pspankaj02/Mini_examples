namespace KiranaHub.DSA.DataStructures;

// =========================================================================================
// [KYA KARTA HAI]:
// Custom Hash Table jo Key-Value pairs ko store karti hai aur Hash Collision ko Separate Chaining (Linked list buckets) se resolve karti hai.
// KiranaHub me Barcode/SKU se Instant O(1) Product lookup ke liye.
//
// [KYU ZAROORI HAI]:
// Dictionary C# ka sabse popular data structure hai. Interview aur architecture me samajhna zaroori hai
// ki `GetHashCode()` internally bucket index kaise calculate karta hai aur collision hone par data kaise safe rehta hai.
//
// [KAISE CHALTA HAI]:
// `int bucketIndex = Math.Abs(key.GetHashCode()) % bucketCount`.
// Agar do alag keys ka index same aa jaye (Collision), toh us bucket ke Linked List me naya node append ho jata hai.
// =========================================================================================
public class CustomHashTable<TKey, TValue> where TKey : notnull
{
    private class HashNode
    {
        public TKey Key { get; }
        public TValue Value { get; set; }
        public HashNode? Next { get; set; }

        public HashNode(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }

    private readonly HashNode?[] _buckets;
    private readonly int _capacity;
    private int _count;

    public int Count => _count;

    public CustomHashTable(int capacity = 16)
    {
        _capacity = capacity;
        _buckets = new HashNode?[capacity];
    }

    private int GetBucketIndex(TKey key)
    {
        int hashCode = key.GetHashCode();
        // Bitwise AND with int.MaxValue to avoid Math.Abs(int.MinValue) overflow edge case
        return (hashCode & 0x7FFFFFFF) % _capacity;
    }

    // O(1) Average Put
    public void Put(TKey key, TValue value)
    {
        int index = GetBucketIndex(key);
        var head = _buckets[index];

        // Check if key already exists in the chain
        var current = head;
        while (current != null)
        {
            if (current.Key.Equals(key))
            {
                current.Value = value; // update
                return;
            }
            current = current.Next;
        }

        // Insert new node at head of chain
        var newNode = new HashNode(key, value) { Next = head };
        _buckets[index] = newNode;
        _count++;
    }

    // O(1) Average Get
    public bool TryGet(TKey key, out TValue? value)
    {
        int index = GetBucketIndex(key);
        var current = _buckets[index];

        while (current != null)
        {
            if (current.Key.Equals(key))
            {
                value = current.Value;
                return true;
            }
            current = current.Next;
        }

        value = default;
        return false;
    }
}


namespace KiranaHub.DSA.DataStructures;

// =========================================================================================
// [KYA KARTA HAI]:
// Binary Search Tree (BST) jo items ko unke keys ke according sorted order me maintain karta hai.
// KiranaHub catalog me products ko Price range ke according fast search aur In-Order traversal
// (sorted output) lene ke liye.
//
// [KYU ZAROORI HAI]:
// Unsorted array me search karne par O(N) linear time lagta hai.
// BST me average search time O(log N) hota hai kyunki har step par search space aadhi ho jaati hai.
//
// [KAISE CHALTA HAI]:
// Left child hamesha current node se chota hota hai, aur Right child bada hota hai.
// In-order traversal (Left -> Root -> Right) hamesha strictly sorted sequence deta hai!
// =========================================================================================
public class BinarySearchTree<TKey, TValue> where TKey : IComparable<TKey>
{
    private class BstNode
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }
        public BstNode? Left { get; set; }
        public BstNode? Right { get; set; }

        public BstNode(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }

    private BstNode? _root;

    // O(log N) Average Insert
    public void Insert(TKey key, TValue value)
    {
        _root = InsertRecursive(_root, key, value);
    }

    private BstNode InsertRecursive(BstNode? node, TKey key, TValue value)
    {
        if (node == null) return new BstNode(key, value);

        int cmp = key.CompareTo(node.Key);
        if (cmp < 0)
        {
            node.Left = InsertRecursive(node.Left, key, value);
        }
        else if (cmp > 0)
        {
            node.Right = InsertRecursive(node.Right, key, value);
        }
        else
        {
            node.Value = value; // Update existing
        }

        return node;
    }

    // O(log N) Average Search
    public bool TryFind(TKey key, out TValue? value)
    {
        var current = _root;
        while (current != null)
        {
            int cmp = key.CompareTo(current.Key);
            if (cmp == 0)
            {
                value = current.Value;
                return true;
            }
            current = (cmp < 0) ? current.Left : current.Right;
        }

        value = default;
        return false;
    }

    // In-order traversal yields elements in ascending sorted order
    public IEnumerable<TValue> InOrderTraversal()
    {
        var result = new List<TValue>();
        InOrder(_root, result);
        return result;
    }

    private void InOrder(BstNode? node, List<TValue> list)
    {
        if (node == null) return;
        InOrder(node.Left, list);
        list.Add(node.Value);
        InOrder(node.Right, list);
    }
}


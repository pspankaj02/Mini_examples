using System.Collections;

namespace KiranaHub.DSA.DataStructures;

// =========================================================================================
// [KYA KARTA HAI]:
// Custom Singly Linked List jo generic data `T` ko dynamic nodes ke roop me link karti hai.
// KiranaHub me real-time live orders ki stream ko bina array resize overhead ke handle karne ke liye.
//
// [KYU ZAROORI HAI]:
// Standard C# Arrays fixed size ke hote hain. Array resize karne me O(N) copy penalty lagti hai.
// Linked list me head par insert karna hamesha O(1) constant time hota hai!
//
// [KAISE CHALTA HAI]:
// Har Node ke paas apna Data aur agle Node ka reference (`Next`) heap memory me store hota hai.
//
// [VARIATIONS TO TRY]:
// Doubly Linked List banayein jisme `Previous` pointer bhi ho, taaki backward traversal O(1) me ho sake.
// =========================================================================================
public class CustomLinkedList<T> : IEnumerable<T>
{
    public class Node
    {
        public T Value { get; set; }
        public Node? Next { get; set; }

        public Node(T value)
        {
            Value = value;
            Next = null;
        }
    }

    private Node? _head;
    private int _count;

    public int Count => _count;
    public bool IsEmpty => _count == 0;

    // [O(1) Insertion at Head]
    public void AddFirst(T value)
    {
        var newNode = new Node(value)
        {
            Next = _head
        };
        _head = newNode;
        _count++;
    }

    // [O(N) Insertion at Tail]
    public void AddLast(T value)
    {
        var newNode = new Node(value);
        if (_head == null)
        {
            _head = newNode;
        }
        else
        {
            var current = _head;
            while (current.Next != null)
            {
                current = current.Next;
            }
            current.Next = newNode;
        }
        _count++;
    }

    // [O(1) Removal from Head]
    public T RemoveFirst()
    {
        if (_head == null)
            throw new InvalidOperationException("Linked list is empty!");

        T val = _head.Value;
        _head = _head.Next;
        _count--;
        return val;
    }

    public IEnumerator<T> GetEnumerator()
    {
        var current = _head;
        while (current != null)
        {
            yield return current.Value;
            current = current.Next;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


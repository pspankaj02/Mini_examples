namespace KiranaHub.DSA.DataStructures;

// =========================================================================================
// [KYA KARTA HAI]:
// Min-Heap based Priority Queue.
// KiranaHub me VIP Customers aur Express Delivery orders ko normal orders se pehle process karne ke liye.
//
// [KYU ZAROORI HAI]:
// Agar standard queue use karenge toh First-Come-First-Serve (FIFO) hoga. Lekin instant 10-minute
// express delivery orders ko top priority milni chahiye! Priority Queue O(log N) me highest priority item nikal deta hai.
//
// [KAISE CHALTA HAI]:
// Complete Binary Tree ko Array ke roop me represent karta hai (Parent at index `(i-1)/2`, Left child at `2*i+1`).
// Enqueue par 'Bubble-Up' aur Dequeue par 'Bubble-Down' (Heapify) hota hai.
//
// [OUTPUT EXPECTATION]:
// Lowest priority number (e.g. Priority 1 = VIP Rush) hamesha pehle nikalta hai.
// =========================================================================================
public class CustomPriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
{
    private readonly List<(TElement Element, TPriority Priority)> _heap = new();

    public int Count => _heap.Count;
    public bool IsEmpty => _heap.Count == 0;

    // O(log N) Insert
    public void Enqueue(TElement element, TPriority priority)
    {
        _heap.Add((element, priority));
        BubbleUp(_heap.Count - 1);
    }

    // O(log N) Remove highest priority item (Minimum priority value)
    public TElement Dequeue()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Priority queue is empty!");

        TElement topElement = _heap[0].Element;

        int lastIndex = _heap.Count - 1;
        _heap[0] = _heap[lastIndex];
        _heap.RemoveAt(lastIndex);

        if (_heap.Count > 0)
        {
            BubbleDown(0);
        }

        return topElement;
    }

    private void BubbleUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            if (_heap[index].Priority.CompareTo(_heap[parentIndex].Priority) < 0)
            {
                // Swap with parent
                (_heap[index], _heap[parentIndex]) = (_heap[parentIndex], _heap[index]);
                index = parentIndex;
            }
            else
            {
                break;
            }
        }
    }

    private void BubbleDown(int index)
    {
        int lastIndex = _heap.Count - 1;
        while (true)
        {
            int smallest = index;
            int leftChild = 2 * index + 1;
            int rightChild = 2 * index + 2;

            if (leftChild <= lastIndex && _heap[leftChild].Priority.CompareTo(_heap[smallest].Priority) < 0)
            {
                smallest = leftChild;
            }

            if (rightChild <= lastIndex && _heap[rightChild].Priority.CompareTo(_heap[smallest].Priority) < 0)
            {
                smallest = rightChild;
            }

            if (smallest != index)
            {
                (_heap[index], _heap[smallest]) = (_heap[smallest], _heap[index]);
                index = smallest;
            }
            else
            {
                break;
            }
        }
    }
}


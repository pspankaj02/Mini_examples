namespace KiranaHub.DSA.DataStructures;

// =========================================================================================
// [KYA KARTA HAI]:
// LIFO (Last In First Out) Custom Stack data structure.
// KiranaHub Cart me user ke actions ko "Undo" (pichla action wapas lene) ke liye.
//
// [KYU ZAROORI HAI]:
// Stack LIFO property ensure karti hai ki jo item sabse aakhiri me add hua tha,
// wahi sabse pehle pop hoga (O(1) Push aur Pop).
//
// [KAISE CHALTA HAI]:
// Dynamic array backing store use karta hai jo capacity full hone par double ho jata hai.
// =========================================================================================
public class CustomStack<T>
{
    private T[] _items;
    private int _top;

    public int Count => _top;
    public bool IsEmpty => _top == 0;

    public CustomStack(int initialCapacity = 4)
    {
        _items = new T[initialCapacity];
        _top = 0;
    }

    // O(1) Amortized Push
    public void Push(T item)
    {
        if (_top == _items.Length)
        {
            // Array resizing: double the capacity
            Array.Resize(ref _items, _items.Length * 2);
        }
        _items[_top++] = item;
    }

    // O(1) Pop
    public T Pop()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Stack underflow: No items to pop!");

        T item = _items[--_top];
        _items[_top] = default!; // GC cleanup
        return item;
    }

    // O(1) Peek without removing
    public T Peek()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Stack is empty!");

        return _items[_top - 1];
    }
}


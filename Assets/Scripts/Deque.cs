using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// C# does not come with a double ended queue implementation
/// Creating my own double ended queue will allow for better performance 
/// when compared to a list or a linked list due to a fast way of inserting to 
/// both the start and the end of the list while keeping all of the data close together for cache performance
/// O(1) complexity for data access, and inserting to front and back, this however comes at a 
/// doubled memory cost due to having a back and a front array of the specified capacity

public class Deque<T>
{
    readonly uint Capacity;
    T[] _data;
    int _frontIndex = 0;
    int _backIndex = 0;

    public T Get(int index) { return _data[index - _frontIndex]; }
    public int GetSize() { return _frontIndex + _backIndex; }

    public Deque(uint capacity)
    {
        Capacity = capacity;
        // Allows the full capacity to be added to both sides
        // Removes the need to sort the list but doubles the memory cost
        _data = new T[capacity * 2];
    }

    public void PushFront(T data)
    {

    }

    public void PushBack(T data)
    {

    }

    public void RemoveFront()
    {

    }

    public void RemoveBack()
    {

    }

}

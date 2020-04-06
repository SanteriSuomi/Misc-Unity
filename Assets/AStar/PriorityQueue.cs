using System;
using System.Collections;
using System.Collections.Generic;

public class PriorityQueue<T> : IEnumerable<T> where T : IComparable<T>
{
    private readonly List<T> heap;
    public int Count { get => heap.Count; }
    public bool IsEmptyOrNull { get => heap == null || Count == 0; }

    public T this[int i]
    {
        get
        {
            if (Count <= 0) return default;
            return heap[i];
        }
    }

    /// <summary>
    /// Initialize an empty priority queue.
    /// </summary>
    public PriorityQueue() => heap = new List<T>();

    /// <summary>
    /// Initialize the priority queue with a predetermined amount of memory.
    /// </summary>
    public PriorityQueue(int heapStartSize)
        => heap = new List<T>(heapStartSize);

    /// <summary>
    /// Initialize the priority queue with predetermined data (data gets min-heapified before applied).
    /// </summary>
    /// <param name="data"></param>
    public PriorityQueue(List<T> data)
    {
        MinHeapify(data);
        heap = data;
    }

    private int LastIndex { get => Count - 1; }
    private T LastItem
    {
        get => heap[LastIndex];
        set => heap[LastIndex] = value;
    }
    private T FirstItem
    {
        get => heap[0];
        set => heap[0] = value;
    }

    private enum Child
    {
        Left = 1,
        Right = 2
    }

    /// <summary>
    /// Push a new element and sort the heap.
    /// </summary>
    /// <param name="item"></param>
    public void Push(T item)
    {
        if (item is null) { return; }

        heap.Add(item);
        int currentIndex = LastIndex;
        int currentParentIndex = GetParentIndex(currentIndex);
        while (item.CompareTo(heap[currentParentIndex]) < 0)
        {
            T itemParent = heap[currentParentIndex];
            heap[currentParentIndex] = item;
            heap[currentIndex] = itemParent;
            currentIndex = currentParentIndex;
            currentParentIndex = GetParentIndex(currentIndex);
        }
    }

    /// <summary>
    /// Return and remove the first element, then sort the heap.
    /// </summary>
    /// <returns>T</returns>
    public T Pop()
    {
        if (Count == 0) { return default; }

        T itemToReturn;
        if (Count == 1)
        {
            itemToReturn = FirstItem;
            heap.RemoveAt(LastIndex);
            return itemToReturn;
        }
        else if (Count == 2)
        {
            RemoveFirst();
            return itemToReturn;
        }

        RemoveFirst();
        int currentIndex = 0;
        int leftChild = GetChildIndex(Child.Left, currentIndex);
        int rightChild = GetChildIndex(Child.Right, currentIndex);
        int currentChildIndex = GetChildDirection(leftChild, rightChild, heap);
        while (currentChildIndex <= LastIndex
               && heap[currentIndex].CompareTo(heap[currentChildIndex]) > 0)
        {
            T currentItem = heap[currentIndex];
            T currentChildItem = heap[currentChildIndex];
            heap[currentIndex] = currentChildItem;
            heap[currentChildIndex] = currentItem;
            currentIndex = currentChildIndex;
            leftChild = GetChildIndex(Child.Left, currentIndex);
            rightChild = GetChildIndex(Child.Right, currentIndex);
            currentChildIndex = GetChildDirection(leftChild, rightChild, heap);
        }

        return itemToReturn;

        void RemoveFirst()
        {
            itemToReturn = FirstItem;
            FirstItem = LastItem;
            LastItem = FirstItem;
            heap.RemoveAt(LastIndex);
        }
    }

    /// <summary>
    /// Convert data to minimum-heap form (min-heapify).
    /// </summary>
    /// <param name="data"></param>
    public void MinHeapify(List<T> data)
    {
        int lastIndex = data.Count - 1;
        int currentParentIndex = GetParentIndex(lastIndex); // Start at the last parent.
        while (currentParentIndex >= 0)
        {
            while (GetChildIndex(Child.Left, currentParentIndex) <= lastIndex)
            {
                int leftChild = GetChildIndex(Child.Left, currentParentIndex);
                int rightChild = GetChildIndex(Child.Right, currentParentIndex);
                int currentChildIndex = GetChildDirection(leftChild, rightChild, data);
                if (data[currentParentIndex].CompareTo(data[currentChildIndex]) > 0)
                {
                    T temp = data[currentParentIndex];
                    data[currentParentIndex] = data[currentChildIndex];
                    data[currentChildIndex] = temp;
                    currentParentIndex = currentChildIndex;
                    continue;
                }

                break;
            }

            currentParentIndex--; // Move on to the next parent.
        }
    }

    /// <summary>
    /// Return the first element.
    /// </summary>
    /// <returns></returns>
    public T Peek() => FirstItem;

    private static int GetChildIndex(Child child, int index)
        => index * 2 + (int)child;

    private static int GetParentIndex(int index)
        => (int)Math.Floor((float)index / 2);

    private int GetChildDirection(int leftChild, int rightChild, List<T> data)
    {
        int lastIndex = data.Count - 1;
        if (rightChild <= lastIndex
            && data[rightChild].CompareTo(data[leftChild]) < 0)
        {
            return rightChild;
        }

        return leftChild;
    }

    #region Support Data Iteration
    public IEnumerator<T> GetEnumerator()
        => ((IEnumerable<T>)heap).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable<T>)heap).GetEnumerator();
    #endregion
}
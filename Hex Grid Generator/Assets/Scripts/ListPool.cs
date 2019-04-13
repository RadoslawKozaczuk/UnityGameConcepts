using System.Collections.Generic;

public static class ListPool<T>
{
    static Stack<List<T>> _stack = new Stack<List<T>>();


    /// <summary>
    /// Returns first available list from the pool.
    /// Otherwise, a new list is created and returnd.
    /// </summary>
    public static List<T> Get()
    {
        if (_stack.Count == 0)
            Add(new List<T>());

        return _stack.Pop();
    }

    public static void Add(List<T> list)
    {
        list.Clear();
        _stack.Push(list);
    }
}
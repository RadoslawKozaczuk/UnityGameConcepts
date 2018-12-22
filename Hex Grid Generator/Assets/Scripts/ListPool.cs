using System.Collections.Generic;

public static class ListPool<T>
{
    static Stack<List<T>> _stack = new Stack<List<T>>();

    public static List<T> Get() => _stack.Count > 0 ? _stack.Pop() : new List<T>();

    public static void Add(List<T> list)
    {
        list.Clear();
        _stack.Push(list);
    }
}
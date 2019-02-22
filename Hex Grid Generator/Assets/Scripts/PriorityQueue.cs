using System.Collections.Generic;

public class PriorityQueue
{
	public int Count { get; private set; } = 0;
	List<HexCell> _list = new List<HexCell>();
	int _minimum = int.MaxValue;

	public void Enqueue(HexCell cell)
	{
		Count += 1;
		int priority = cell.Distance + cell.SearchHeuristic;
		if (priority < _minimum)
			_minimum = priority;

		// However, that only works if the list is long enough, otherwise we go out of bounds.
		// We can prevent that by adding dummy elements to the list until it has the required length.
		// These empty elements don't reference a cell, so they're created by adding null to the list.
		while (priority >= _list.Count)
			_list.Add(null);

		// linked list
		cell.NextWithSamePriority = _list[priority];

		// When a cell is added to the queue, let's start by simply using its priority as its index, treating the list as a simple array.
		_list[priority] = cell;
	}

	public HexCell Dequeue()
	{
		Count -= 1;

		for (; _minimum < _list.Count; _minimum++)
		{
			HexCell cell = _list[_minimum];
			if (cell != null)
			{
				_list[_minimum] = cell.NextWithSamePriority;
				return cell;
			}
		}

		return null;
	}

	public void Change(HexCell cell, int oldPriority)
	{
		// Declaring the head of the old priority list to be the current cell, and also keep track of the next cell.
		// We can directly grab the next cell, because we know that there is at least one cell at this index.
		HexCell current = _list[oldPriority];
		HexCell next = current.NextWithSamePriority;

		// If the current cell is the changed cell, then it is the head cell and we can cut it away, as if we dequeued it.
		if (current == cell)
		{
			_list[oldPriority] = next;
		}
		// If not, we have to follow the chain until we end up at the cell in front of the changed cell.
		// That one holds the reference to the cell that has been changed.
		else
		{
			while (next != cell)
			{
				current = next;
				next = current.NextWithSamePriority;
			}

			// At this point, we can remove the changed cell from the linked list, by skipping it.
			current.NextWithSamePriority = cell.NextWithSamePriority;
		}

		// After the cell has been removed, it has to be added again so it ends up in the list for its new priority.
		Enqueue(cell);

		// The Enqueue method increments the count, but we're not actually adding a new cell.
		// So we have to decrement the count to compensate for that.
		Count -= 1;
	}

	public void Clear()
	{
		Count = 0;
		_list.Clear();
		_minimum = int.MaxValue;
	}
}

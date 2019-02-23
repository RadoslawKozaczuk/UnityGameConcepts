using System.Collections.Generic;
using UnityEngine;

public class Pathfinder
{
	struct InternalData
	{
		public readonly int Id;
		public float SearchHeuristic;
		public int NextWithSamePriorityId;

		public InternalData (int id)
		{
			Id = id;
			SearchHeuristic = 0f;
			NextWithSamePriorityId = 0;
		}
	}

	public int Count { get; private set; } = 0;

	readonly List<int> _priorityQueue = new List<int>();
	readonly InternalData[] _internalData;
	HexCell[] _cells;

	int _minimum = int.MaxValue;

	public Pathfinder(int numberOfCells)
	{
		_internalData = new InternalData[numberOfCells];
		for (int i = 0; i < numberOfCells; i++)
			_internalData[i] = new InternalData(i);
	}

	public void FindPath(HexCell[] cells, HexCell fromCell, HexCell toCell)
	{
		_cells = cells;

		for (int i = 0; i < cells.Length; i++)
		{
			cells[i].Distance = int.MaxValue;
			cells[i].DisableHighlight();
		}

		fromCell.EnableHighlight(Color.blue);
		toCell.EnableHighlight(Color.red);

		Search(fromCell, toCell);
	}

	void Search(HexCell fromCell, HexCell toCell)
	{
		Clear();

		fromCell.Distance = 0;

		Enqueue(fromCell, _internalData[fromCell.Id].SearchHeuristic);
		while (Count > 0)
		{
			HexCell current = Dequeue();

			if(current == null)
				break;

			if (current == toCell)
			{
				current = current.PathFrom;

				// visualize the path
				while (current != fromCell)
				{
					current.EnableHighlight(Color.white);
					current = current.PathFrom;
				}

				break;
			}

			for (HexDirection dir = HexDirection.NorthEast; dir <= HexDirection.NorthWest; dir++)
			{
				HexCell neighbor = current.GetNeighbor(dir);
				if (neighbor == null)
					continue;

				HexEdgeType edgeType = current.GetEdgeType(neighbor);
				if (neighbor.IsUnderwater || edgeType == HexEdgeType.Cliff)
					continue;

				// roads are three times faster than not roads
				float distanceToAdd = GameSettings.GetMovementCost(neighbor.TerrainType);
				if (current.HasRoadThroughEdge(dir))
					distanceToAdd /= 2;

				// moving upslope is twice as expensives
				if (edgeType == HexEdgeType.Slope && neighbor.Elevation > current.Elevation)
					distanceToAdd *= 2;

				float distance = current.Distance + distanceToAdd;

				if (neighbor.Distance == int.MaxValue)
				{
					var searchHeuristic = _internalData[neighbor.Id].SearchHeuristic;
					neighbor.Distance = distance;
					neighbor.PathFrom = current;
					_internalData[neighbor.Id].SearchHeuristic = searchHeuristic;
					Enqueue(neighbor, searchHeuristic);
				}
				else if (distance < neighbor.Distance)
				{
					var searchHeuristic = _internalData[neighbor.Id].SearchHeuristic;
					float oldPriority = neighbor.Distance + searchHeuristic;
					neighbor.Distance = distance;
					neighbor.PathFrom = current;
					Change(neighbor, searchHeuristic, oldPriority);
				}
			}
		}
	}

	public void Enqueue(HexCell cell, float searchHeuristic)
	{
		Count += 1;
		int priority = Mathf.RoundToInt(cell.Distance + searchHeuristic);
		if (priority < _minimum)
			_minimum = priority;

		// However, that only works if the list is long enough, otherwise we go out of bounds.
		// We can prevent that by adding dummy elements to the list until it has the required length.
		// These empty elements don't reference a cell, so they're created by adding null to the list.
		while (priority >= _priorityQueue.Count)
			_priorityQueue.Add(int.MinValue);

		// linked list
		_internalData[cell.Id].NextWithSamePriorityId = _priorityQueue[priority];

		// When a cell is added to the queue, let's start by simply using its priority as its index, treating the list as a simple array.
		_priorityQueue[priority] = cell.Id;
	}

	public HexCell Dequeue()
	{
		Count -= 1;

		for (; _minimum < _priorityQueue.Count; _minimum++)
		{
			int id = _priorityQueue[_minimum];

			if (id == int.MinValue)
				continue;

			HexCell cell = _cells[id];
			if (cell != null)
			{
				_priorityQueue[_minimum] = _internalData[cell.Id].NextWithSamePriorityId;
				return cell;
			}
		}

		return null;
	}

	public void Clear()
	{
		Count = 0;
		_priorityQueue.Clear();
		_minimum = int.MaxValue;
	}

	public void Change(HexCell cell, float searchHeuristic, float oldPriority)
	{
		var index = Mathf.RoundToInt(oldPriority);
		// Declaring the head of the old priority list to be the current cell, and also keep track of the next cell.
		// We can directly grab the next cell, because we know that there is at least one cell at this index.
		HexCell current = _cells[_priorityQueue[index]];
		HexCell next = _cells[_internalData[current.Id].NextWithSamePriorityId];

		// If the current cell is the changed cell, then it is the head cell and we can cut it away, as if we dequeued it.
		if (current == cell)
		{
			_priorityQueue[index] = next.Id;
		}
		// If not, we have to follow the chain until we end up at the cell in front of the changed cell.
		// That one holds the reference to the cell that has been changed.
		else
		{
			while (next != cell)
			{
				current = next;
				var nextId = _internalData[current.Id].NextWithSamePriorityId;
				next = _cells[nextId];
			}

			// At this point, we can remove the changed cell from the linked list, by skipping it.
			_internalData[current.Id].NextWithSamePriorityId = _internalData[cell.Id].NextWithSamePriorityId;
		}

		// After the cell has been removed, it has to be added again so it ends up in the list for its new priority.
		Enqueue(cell, searchHeuristic);

		// The Enqueue method increments the count, but we're not actually adding a new cell.
		// So we have to decrement the count to compensate for that.
		Count -= 1;
	}
}

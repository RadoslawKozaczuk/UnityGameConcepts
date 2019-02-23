using System.Collections;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
	PriorityQueue queue = new PriorityQueue();

	// internal operational data
	struct DataInternal
	{
		public int Id; // my counter part object in cell
		public float SearchHeuristic;
	}

	DataInternal[] _internalDataObjects;

	public void FindPath(HexCell[] cells, HexCell fromCell, HexCell toCell)
	{
		_internalDataObjects = new DataInternal[cells.Length];

		for (int i = 0; i < cells.Length; i++)
		{
			_internalDataObjects[i] = new DataInternal() { Id = i };

			cells[i].Distance = int.MaxValue;
			cells[i].DisableHighlight();
		}

		fromCell.EnableHighlight(Color.blue);
		toCell.EnableHighlight(Color.red);

		StartCoroutine(Search(fromCell, toCell));
	}

	IEnumerator Search(HexCell fromCell, HexCell toCell)
	{
		queue.Clear();

		var delay = new WaitForSeconds(1 / 60f);
		fromCell.Distance = 0;

		queue.Enqueue(fromCell, _internalDataObjects[fromCell.Id].SearchHeuristic);
		while (queue.Count > 0)
		{
			yield return delay;
			HexCell current = queue.Dequeue();

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
					var searchHeuristic = _internalDataObjects[neighbor.Id].SearchHeuristic;
					neighbor.Distance = distance;
					neighbor.PathFrom = current;
					_internalDataObjects[neighbor.Id].SearchHeuristic = searchHeuristic;
					queue.Enqueue(neighbor, searchHeuristic);
				}
				else if (distance < neighbor.Distance)
				{
					var searchHeuristic = _internalDataObjects[neighbor.Id].SearchHeuristic;
					float oldPriority = neighbor.Distance + searchHeuristic;
					neighbor.Distance = distance;
					neighbor.PathFrom = current;
					queue.Change(neighbor, searchHeuristic, oldPriority);
				}
			}
		}
	}
}

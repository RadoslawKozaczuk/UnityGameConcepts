using System.Collections;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
	PriorityQueue queue = new PriorityQueue();

	public void FindPath(HexCell[] cells, HexCell fromCell, HexCell toCell)
	{
		for (int i = 0; i < cells.Length; i++)
		{
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

		queue.Enqueue(fromCell);
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
					neighbor.Distance = distance;
					neighbor.PathFrom = current;
					neighbor.SearchHeuristic = neighbor.Coordinates.DistanceTo(toCell.Coordinates);
					queue.Enqueue(neighbor);
				}
				else if (distance < neighbor.Distance)
				{
					float oldPriority = neighbor.Distance + neighbor.SearchHeuristic;
					neighbor.Distance = distance;
					neighbor.PathFrom = current;
					queue.Change(neighbor, oldPriority);
				}
			}
		}
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
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
		var delay = new WaitForSeconds(1 / 60f);
		var queue = new List<HexCell>();
		fromCell.Distance = 0;
		queue.Add(fromCell);

		while (queue.Count > 0)
		{
			yield return delay;
			HexCell current = queue[0];
			queue.RemoveAt(0);

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
				int distanceToAdd = current.HasRoadThroughEdge(dir) ? 1 : 3;

				// moving upslope is twice as expensives
				if (edgeType == HexEdgeType.Slope && neighbor.Elevation > current.Elevation)
					distanceToAdd *= 2;

				int distance = current.Distance + distanceToAdd;
				if (neighbor.Distance == int.MaxValue)
				{
					neighbor.Distance = distance;
					neighbor.PathFrom = current;
					queue.Add(neighbor);
				}
				else if (distance < neighbor.Distance)
				{
					neighbor.Distance = distance;
					neighbor.PathFrom = current;
				}

				queue.Sort((x, y) => x.Distance.CompareTo(y.Distance));
			}
		}
	}
}

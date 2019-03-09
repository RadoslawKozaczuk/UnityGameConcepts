using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameUI : MonoBehaviour
{
	public HexGrid grid;

	// Depending on what's happening, GameUI needs to know which cell is currently underneath the cursor.
	HexCell currentCell;
	HexCell previousCell;
	Unit selectedUnit;

	void Update()
	{
		if (!EventSystem.current.IsPointerOverGameObject())
		{
			if (Input.GetMouseButtonDown(0))
			{
				DoSelection();
			}
		}
	}

	public void SetEditMode(bool toggle)
	{
		enabled = !toggle;
		//grid.ShowUI(!toggle); // I think this is not necessary in my build
		grid.ClearPath();
	}

	// This method will be invoked when a move command is issued and we have a unit selected.
	void DoMove()
	{
		if (grid.HasPath)
		{
			selectedUnit.Location = currentCell;
			grid.ClearPath();
		}
	}

	/// <summary>
	/// Returns true if the clicked cell is different than the previous one, otherwise false.
	/// </summary>
	bool UpdateCurrentCell()
	{
		HexCell cell =	grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));

		// wrong selection - cell not found or is the same cell
		if (cell == null || cell == currentCell)
			return false;

		previousCell = currentCell;
		currentCell = cell;

		return true;
	}

	void DoSelection()
	{
		if (UpdateCurrentCell())
		{
			// new cell has been selected - ok what now?

			// does the new cell have a unit?
			if(currentCell.HasUnit)
			{
				// this unit is our new selected unit
				selectedUnit = currentCell.Unit;
			}
			// does the previous cell have a unit?
			else if(previousCell.HasUnit)
			{
				// that means - move it
				if (selectedUnit.IsValidDestination(currentCell))
				{
					grid.FindPath(selectedUnit.Location, currentCell);
					DoMove();
				}
			}
		}
	}

	void DoPathfinding()
	{
		if (UpdateCurrentCell())
		{
			if (currentCell && selectedUnit.IsValidDestination(currentCell))
			{
				grid.FindPath(selectedUnit.Location, currentCell);
			}
		}
	}
}

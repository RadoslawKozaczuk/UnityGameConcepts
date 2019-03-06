using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameUI : MonoBehaviour
{
	public HexGrid grid;

	// Depending on what's happening, GameUI needs to know which cell is currently underneath the cursor.
	HexCell currentCell;
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

	bool UpdateCurrentCell()
	{
		HexCell cell =	grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
		if (cell != currentCell)
		{
			currentCell = cell;
			return true;
		}
		return false;
	}

	void DoSelection()
	{
		grid.ClearPath();

		if (UpdateCurrentCell())
		{
			selectedUnit = currentCell.Unit;
		}
		else if (selectedUnit)
		{
			if (Input.GetMouseButtonDown(1))
			{
				DoMove();
			}
			else
			{
				DoPathfinding();
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

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Unit : MonoBehaviour
{
	const float travelSpeed = 4f;

	public static Unit unitPrefab;

	public HexCell Location
	{
		get => _location;
		set
		{
			// inform the old location that the unit is no logner there
			if (_location)
				_location.Unit = null;

			_location = value;
			value.Unit = this;
			transform.localPosition = ApplyVerticalOffset(value.Position);
		}
	}
	HexCell _location;

	public float Orientation
	{
		get => _orientation;
		set
		{
			_orientation = value;
			transform.localRotation = Quaternion.Euler(0f, value, 0f);
		}
	}
	float _orientation;

	public bool HasPath
	{
		get => PathToTravel.Count > 0;
	}

	public HexGrid HexGrid;

	List<HexCell> _pathToTravel;
	public List<HexCell> PathToTravel
	{
		get => _pathToTravel;
		set
		{
			if (value == null)
				_pathToTravel.Clear();
			else
				_pathToTravel = value;

			StopCoroutine("TravelPath");
		}
	}

	void OnDrawGizmos()
	{
		if (PathToTravel == null || PathToTravel.Count == 0)
			return;

		// draw sphears along the path in order to visualize it
		for (int i = 1; i < PathToTravel.Count; i++)
		{
			Vector3 a = PathToTravel[i - 1].Position;
			Vector3 b = PathToTravel[i].Position;
			Gizmos.color = Color.grey;

			for (float t = 0f; t < 1f; t += 0.1f)
				Gizmos.DrawSphere(Vector3.Lerp(a, b, t), 1.5f);
		}
	}

	// One downside of coroutines is that they do not survive recompiles while in play mode.
	// Although the game state is always correct, this can lead to units being stuck somewhere along their last path,
	// if you happened to trigger a recompile while they were moving.
	// To mitigate this, let's make sure that units are always in the proper location after a recompile.
	// This can be done by updating their position in OnEnable.
	void OnEnable()
	{
		if (_location)
			transform.localPosition = _location.Position;

		PathToTravel = new List<HexCell>();
	}

	// path in general need to be stored in the unit
	// so unit orders a path, then grid provides it by using the pathfinder
	// and what's also importont the grid and the path finder are both single entities
	// maybe they could be singletons or static objects
	// they dont even need to be objects on the hierarchy tbh
	// pathfinder is not in the hierarchy anyway but grid is

	/// <summary>
	/// Disables highlight for the current path.
	/// </summary>
	public void ClearPath()
	{
		foreach (HexCell cell in PathToTravel)
			cell.DisableHighlight();
	}

	public void VisualizePath()
	{
		// visualize the path
		var first = PathToTravel[0];
		var last = PathToTravel[PathToTravel.Count - 1];

		first.EnableHighlight(Color.blue);
		for (int i = 1; i < PathToTravel.Count - 1; i++)
			PathToTravel[i].EnableHighlight(Color.white);
		last.EnableHighlight(Color.red);
	}

	IEnumerator TravelPath()
	{
		for (int i = 1; i < PathToTravel.Count; i++)
		{
			Vector3 a = PathToTravel[i - 1].Position;
			Vector3 b = PathToTravel[i].Position;

			// 1s per hex
			for (float t = 0f; t < 1f; t += Time.deltaTime * travelSpeed)
			{
				transform.localPosition = Vector3.Lerp(a, b, t);
				yield return null;
			}
		}
	}

	public void ValidateLocation() => transform.localPosition = ApplyVerticalOffset(_location.Position);

	public void Travel()
	{
		VisualizePath();
		StopCoroutine("TravelPath");
		StartCoroutine(TravelPath());
	}

	/// <summary>
	/// Clean the cell's location variable and destroys the unit object.
	/// </summary>
	public void Die()
	{
		_location.Unit = null;
		Destroy(gameObject);
	}

	public static void Load(BinaryReader reader, HexGrid grid)
	{
		HexCoordinates coordinates = HexCoordinates.Load(reader);
		float orientation = reader.ReadSingle();
		grid.AddUnit(Instantiate(unitPrefab), grid.GetCell(coordinates), orientation);
	}

	public void Save(BinaryWriter writer)
	{
		_location.Coordinates.Save(writer);
		writer.Write(_orientation);
	}

	/// <summary>
	/// Return true when the Unit can enter that cell, false otherwise.
	/// </summary>
	public bool IsValidDestination(HexCell cell) => !cell.IsUnderwater && !cell.Unit;

	Vector3 ApplyVerticalOffset(Vector3 position) => new Vector3(position.x, position.y + 2f, position.z);
}

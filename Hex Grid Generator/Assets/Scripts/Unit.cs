using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Unit : MonoBehaviour
{
	const float travelSpeed = 4f;
	const float rotationSpeed = 180f;

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
		if (_pathToTravel == null || _pathToTravel.Count == 0)
			return;

		Vector3 a, b, c = _pathToTravel[0].Position;

		for (int i = 1; i < _pathToTravel.Count; i++)
		{
			a = c;
			b = _pathToTravel[i - 1].Position;
			c = (b + _pathToTravel[i].Position) * 0.5f;
			for (float t = 0f; t < 1f; t += Time.deltaTime * travelSpeed)
				Gizmos.DrawSphere(Bezier.GetPointUnclamped(a, b, c, t), 2f);
		}

		a = c;
		b = _pathToTravel[_pathToTravel.Count - 1].Position;
		c = b;
		for (float t = 0f; t < 1f; t += 0.1f)
			Gizmos.DrawSphere(Bezier.GetPointUnclamped(a, b, c, t), 2f);
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
		Vector3 a, b, c = _pathToTravel[0].Position;

		// To prevent time loss, we have to transfer the remaining time from one segment to the next.
		// This can be done by keeping track of t through the entire travel, not just per segment.
		// Then at the end of each segment, subtract 1 from it.
		float t = Time.deltaTime * travelSpeed;
		for (int i = 1; i < _pathToTravel.Count; i++)
		{
			a = c;
			b = _pathToTravel[i - 1].Position;
			c = (b + _pathToTravel[i].Position) * 0.5f;
			for (; t < 1f; t += Time.deltaTime * travelSpeed)
			{
				transform.localPosition = Bezier.GetPointUnclamped(a, b, c, t);
				Vector3 d = Bezier.GetDerivative(a, b, c, t);
				d.y = 0f; // to be sure that moving up or down does not affest the rotatio
				transform.localRotation = Quaternion.LookRotation(d);
				yield return null;
			}
			t -= 1f;
		}

		a = c;
		b = _pathToTravel[_pathToTravel.Count - 1].Position;
		c = b;
		for (; t < 1f; t += Time.deltaTime * travelSpeed)
		{
			transform.localPosition = Bezier.GetPointUnclamped(a, b, c, t);
			Vector3 d = Bezier.GetDerivative(a, b, c, t);
			d.y = 0f; // to be sure that moving up or down does not affest the rotatio
			transform.localRotation = Quaternion.LookRotation(d);
			yield return null;
		}

		// We don't end exactly at the time that the path should be finished, but just short of it.
		// Once again, how big a difference there can be depends on the frame rate.
		// So let's make sure that the unit ends up exactly at its destination.
		transform.localPosition = Location.Position;

		Orientation = transform.localRotation.eulerAngles.y;
	}

	IEnumerator LookAt(Vector3 point)
	{
		point.y = transform.localPosition.y;
		Quaternion fromRotation = transform.localRotation;
		Quaternion toRotation =	Quaternion.LookRotation(point - transform.localPosition);
		float angle = Quaternion.Angle(fromRotation, toRotation);

		if (angle > 0f)
		{
			float speed = rotationSpeed / angle;

			for (float t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed)
			{
				transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
				yield return null;
			}
		}

		transform.LookAt(point);
		Orientation = transform.localRotation.eulerAngles.y;
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

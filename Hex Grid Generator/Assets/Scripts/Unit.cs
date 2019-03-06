using System.IO;
using UnityEngine;

public class Unit : MonoBehaviour
{
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

	public void ValidateLocation() => transform.localPosition = ApplyVerticalOffset(_location.Position);

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

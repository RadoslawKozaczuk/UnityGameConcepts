using UnityEngine;

public class Unit : MonoBehaviour
{
	public HexCell Location
	{
		get => _location;
		set
		{
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

	Vector3 ApplyVerticalOffset(Vector3 position) => new Vector3(position.x, position.y + 2f, position.z);
}

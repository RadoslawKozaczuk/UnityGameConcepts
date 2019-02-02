using UnityEngine;
using UnityEngine.UI;

public class SaveLoadItem : MonoBehaviour
{
	public SaveLoadMapMenuController Menu;

	public string MapName
	{
		get
		{
			return _mapName;
		}
		set
		{
			_mapName = value;
			transform.GetChild(0).GetComponent<Text>().text = value;
		}
	}

	string _mapName;

	public void Select() => Menu.SelectItem(_mapName);
}
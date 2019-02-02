using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;

public class SaveLoadMapMenuController : MonoBehaviour
{
	public HexGrid HexGrid;
	public Text MenuLabel, ActionButtonLabel;
	public InputField NameInput;
	public RectTransform ListContent;
	public SaveLoadItem ItemPrefab;

	[SerializeField] Button _actionButton;

	void Awake()
	{
	}

	public void OpenInSaveMode()
	{
		_actionButton.onClick.RemoveAllListeners();

		MenuLabel.text = "Save Map";
		ActionButtonLabel.text = "Save";
		_actionButton.onClick.AddListener(Save);

		FillList();
		gameObject.SetActive(true);
		HexMapCamera.Locked = true;
	}

	public void OpenInLoadMode()
	{
		_actionButton.onClick.RemoveAllListeners();

		MenuLabel.text = "Load Map";
		ActionButtonLabel.text = "Load";
		_actionButton.onClick.AddListener(Load);

		FillList();
		gameObject.SetActive(true);
		HexMapCamera.Locked = true;
	}

	public void Close()
	{
		gameObject.SetActive(false);
		HexMapCamera.Locked = false;
	}

	void Save()
	{
		var path = GetSelectedPath();
		if (path == null)
			return;

		using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
		{
			HexGrid.Save(writer);
		}

		Close();
	}

	void Load()
	{
		var path = GetSelectedPath();
		if (path == null)
			return;

		if (!File.Exists(path))
		{
			Debug.LogError("File does not exist " + path);
			return;
		}

		using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
		{
			HexGrid.Load(reader);
			HexMapCamera.ValidatePosition();
		}

		Close();
	}

	public void SelectItem(string name) => NameInput.text = name;

	public void Delete()
	{
		string path = GetSelectedPath();
		if (path == null)
			return;

		if (File.Exists(path))
			File.Delete(path);

		NameInput.text = "";
		FillList();
	}

	string GetSelectedPath()
	{
		string mapName = NameInput.text;
		return mapName.Length == 0
			? null
			: Path.Combine(Application.persistentDataPath, mapName + ".map");
	}

	void FillList()
	{
		// remove old elements
		for (int i = 0; i < ListContent.childCount; i++)
			Destroy(ListContent.GetChild(i).gameObject);

		string[] paths = Directory.GetFiles(Application.persistentDataPath, "*.map");
		Array.Sort(paths);

		for (int i = 0; i < paths.Length; i++)
		{
			SaveLoadItem item = Instantiate(ItemPrefab);
			item.Menu = this;
			item.MapName = Path.GetFileNameWithoutExtension(paths[i]);
			item.transform.SetParent(ListContent, false);
		}
	}
}
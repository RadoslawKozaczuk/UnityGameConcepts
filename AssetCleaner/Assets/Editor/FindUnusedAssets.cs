using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
	public class FindUnusedAssets : EditorWindow
	{
		AssetCollector _collection = new AssetCollector();
		List<DeleteAsset> _deleteAssets = new List<DeleteAsset>();
		Vector2 _scroll;

		[MenuItem("Assets/Delete Unused Assets/only resource", false, 50)]
		static void InitWithoutCode()
		{
			var window = CreateInstance<FindUnusedAssets>();
			window._collection.UseCodeStrip = false;
			window._collection.Collection();
			window.CopyDeleteFileList(window._collection.DeleteFileList);

			window.Show();
		}

		[MenuItem("Assets/Delete Unused Assets/unused by editor", false, 51)]
		static void InitWithout()
		{
			var window = CreateInstance<FindUnusedAssets>();
			window._collection.Collection();
			window.CopyDeleteFileList(window._collection.DeleteFileList);

			window.Show();
		}

		[MenuItem("Assets/Delete Unused Assets/unused by game", false, 52)]
		static void Init()
		{
			var window = CreateInstance<FindUnusedAssets>();
			window._collection.SaveEditorExtensions = false;
			window._collection.Collection();
			window.CopyDeleteFileList(window._collection.DeleteFileList);

			window.Show();
		}

		void OnGui()
		{
			using (var horizonal = new EditorGUILayout.HorizontalScope("box"))
			{
				EditorGUILayout.LabelField("delete unreference assets from buildsettings and resources");

				if (GUILayout.Button("Delete", GUILayout.Width(120), GUILayout.Height(40)) && _deleteAssets.Count != 0)
				{
					RemoveFiles();
					Close();
				}
			}

			using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scroll))
			{
				_scroll = scrollScope.scrollPosition;
				foreach (var asset in _deleteAssets)
				{
					if (string.IsNullOrEmpty(asset.Path))
					{
						continue;
					}

					using (var horizonal = new EditorGUILayout.HorizontalScope())
					{
						asset.IsDelete = EditorGUILayout.Toggle(asset.IsDelete, GUILayout.Width(20));
						var icon = AssetDatabase.GetCachedIcon(asset.Path);
						GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
						if (GUILayout.Button(asset.Path, EditorStyles.largeLabel))
						{
							Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(asset.Path);
						}
					}
				}
			}

		}

		static void CleanDir()
		{
			RemoveEmptyDirectry("Assets");
			AssetDatabase.Refresh();
		}

		void CopyDeleteFileList(IEnumerable<string> deleteFileList)
		{
			foreach (var asset in deleteFileList)
			{
				var filePath = AssetDatabase.GUIDToAssetPath(asset);
				if (string.IsNullOrEmpty(filePath) == false)
				{
					_deleteAssets.Add(new DeleteAsset() { Path = filePath });
				}
			}
		}

		void RemoveFiles()
		{
			try
			{
				string exportDirectry = "BackupUnusedAssets";
				Directory.CreateDirectory(exportDirectry);
				var files = _deleteAssets.Where(item => item.IsDelete).Select(item => item.Path).ToArray();
				string backupPackageName = exportDirectry + "/package" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".unitypackage";
				EditorUtility.DisplayProgressBar("export package", backupPackageName, 0);
				AssetDatabase.ExportPackage(files, backupPackageName);

				int i = 0;
				int length = _deleteAssets.Count;

				foreach (var assetPath in files)
				{
					i++;
					EditorUtility.DisplayProgressBar("delete unused assets", assetPath, (float)i / length);
					AssetDatabase.DeleteAsset(assetPath);
				}

				EditorUtility.DisplayProgressBar("clean directory", "", 1);
				foreach (var dir in Directory.GetDirectories("Assets"))
				{
					RemoveEmptyDirectry(dir);
				}

				System.Diagnostics.Process.Start(exportDirectry);

				AssetDatabase.Refresh();
			}
			catch (System.Exception e)
			{
				Debug.Log(e.Message);
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		static void RemoveEmptyDirectry(string path)
		{
			var dirs = Directory.GetDirectories(path);
			foreach (var dir in dirs)
			{
				RemoveEmptyDirectry(dir);
			}

			var files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly).Where(item => Path.GetExtension(item) != ".meta");
			if (files.Count() == 0 && Directory.GetDirectories(path).Count() == 0)
			{
				var metaFile = AssetDatabase.GetTextMetaFilePathFromAssetPath(path);
				FileUtil.DeleteFileOrDirectory(path);
				FileUtil.DeleteFileOrDirectory(metaFile);
			}
		}

		class DeleteAsset
		{
			public bool IsDelete = true;
			public string Path;
		}
	}
}

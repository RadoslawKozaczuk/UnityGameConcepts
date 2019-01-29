using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Assets.Editor
{
	public class AssetCollector
	{
		public List<string> DeleteFileList = new List<string>();
		readonly ClassReferenceCollection _classCollection = new ClassReferenceCollection();
		readonly ShaderReferenceCollection _shaderCollection = new ShaderReferenceCollection();

		public bool UseCodeStrip = true;
		public bool SaveEditorExtensions = true;

		public void Collection()
		{
			try
			{
				DeleteFileList.Clear();

				if (UseCodeStrip)
					_classCollection.Collection();

				_shaderCollection.Collection();

				// Find assets
				var files = Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories);

				// filter files
				for (int i = 0; i < files.Length; i++)
				{
					var path = files[i];
					var extension = Path.GetExtension(path);

					if (extension == ".meta"
						|| extension == ".js"
						|| extension == ".dll"
						|| Regex.IsMatch(path, "[\\/\\\\]Gizmos[\\/\\\\]")
						|| Regex.IsMatch(path, "[\\/\\\\]Plugins[\\/\\\\]Android[\\/\\\\]")
						|| Regex.IsMatch(path, "[\\/\\\\]Plugins[\\/\\\\]iOS[\\/\\\\]")
						|| Regex.IsMatch(path, "[\\/\\\\]Resources[\\/\\\\]"))
						continue;

					if (UseCodeStrip == false)
					{
						if (extension != ".cs")
							DeleteFileList.Add(AssetDatabase.AssetPathToGUID(path));
					}
					else
					{
						DeleteFileList.Add(AssetDatabase.AssetPathToGUID(path));
					}
				}

				foreach (var path in files)
				{
					var guid = AssetDatabase.AssetPathToGUID(path);
					DeleteFileList.Add(guid);
				}

				EditorUtility.DisplayProgressBar("checking", "collection all files", 0.2f);
				UnregistReferenceFromResources();

				EditorUtility.DisplayProgressBar("checking", "check reference from resources", 0.4f);
				UnregistReferenceFromScenes();

				EditorUtility.DisplayProgressBar("checking", "check reference from scenes", 0.6f);
				if (SaveEditorExtensions)
					UnregistEditorCodes();
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}
		void UnregistReferenceFromResources()
		{
			var allFiles = Directory.GetFiles("Assets/Components/Menu/Sprites", "*.*", SearchOption.AllDirectories);
			var filtered = new List<string>();

			for (int i = 0; i < allFiles.Length; i++)
			{
				var item = allFiles[i];
				if (Regex.IsMatch(item, "[\\/\\\\]Resources[\\/\\\\]") && Path.GetExtension(item) != ".meta")
					filtered.Add(item);
			}

			foreach (var path in AssetDatabase.GetDependencies(filtered.ToArray()))
				UnregistFromDelteList(AssetDatabase.AssetPathToGUID(path));
		}

		void UnregistReferenceFromScenes()
		{
			// Exclude objects that reference from scenes.
			var scenes = EditorBuildSettings.scenes
				.Where(item => item.enabled)
					.Select(item => item.path)
					.ToArray();

			foreach (var path in AssetDatabase.GetDependencies(scenes))
				UnregistFromDelteList(AssetDatabase.AssetPathToGUID(path));
		}

		void UnregistEditorCodes()
		{
			// Exclude objects that reference from Editor API
			var editorcodes = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories)
				.Where(item => Regex.IsMatch(item, "[\\/\\\\]Editor[\\/\\\\]"))
					.ToArray();

			var undeleteClassList = _classCollection.CodeFileList
				.Where(codefile => codefile.Value.Any(guid => DeleteFileList.Contains(guid)) == false)
					.Select(item => item.Key);

			EditorUtility.DisplayProgressBar("checking", "check reference from editor codes", 0.8f);

			foreach (var path in editorcodes)
			{
				var code = File.ReadAllText(path);
				code = Regex.Replace(code, "//.*[\\n\\r]", "");
				code = Regex.Replace(code, "/\\*.*[\\n\\r]\\*/", "");
				if (Regex.IsMatch(code, "(\\[MenuItem|AssetPostprocessor|EditorWindow)"))
				{
					UnregistFromDelteList(AssetDatabase.AssetPathToGUID(path));
					continue;
				}

				foreach (var undeleteClass in undeleteClassList)
					if (Regex.IsMatch(code, string.Format("\\[CustomEditor.*\\(\\s*{0}\\s*\\).*\\]", undeleteClass.Name)))
						UnregistFromDelteList(path);
			}
		}

		void UnregistFromDelteList(string guid)
		{
			if (!DeleteFileList.Contains(guid))
				return;

			DeleteFileList.Remove(guid);

			if (_classCollection.References.ContainsKey(guid))
			{
				foreach (var type in _classCollection.References[guid])
				{
					var codePaths = _classCollection.CodeFileList[type];
					foreach (var codePath in codePaths)
						UnregistFromDelteList(codePath);
				}
			}

			if (!_shaderCollection.ShaderFileList.ContainsValue(guid))
				return;

			var shader = _shaderCollection.ShaderFileList.First(item => item.Value == guid);
			var shaderAssets = _shaderCollection.ShaderReferenceList[shader.Key];
			foreach (var shaderPath in shaderAssets)
				UnregistFromDelteList(shaderPath);
		}
	}
}

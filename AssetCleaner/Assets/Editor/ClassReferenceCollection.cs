using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Assets.Editor
{
	public class ClassReferenceCollection
	{
		// type : guid
		public Dictionary<Type, List<string>> CodeFileList = new Dictionary<Type, List<string>>();
		// guid : types
		public Dictionary<string, List<Type>> References = new Dictionary<string, List<Type>>();

		public void Collection()
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();

			References.Clear();
			EditorUtility.DisplayProgressBar("checking", "collection all type", 0);

			// Connect the files and class.
			var codes = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);
			// connect each classes.
			var firstPassList = GetFirstPassList();

			var allFirstpassTypes = GetAllFirstpassClasses();
			CollectionCodeFileDictionary(allFirstpassTypes, firstPassList);
			sw.Stop();

			var alltypes = GetAllClasses();
			CollectionCodeFileDictionary(alltypes, codes);
			alltypes.AddRange(allFirstpassTypes);

			int count = 0;
			foreach (var codepath in firstPassList)
			{
				CollectionReferenceClasses(AssetDatabase.AssetPathToGUID(codepath), allFirstpassTypes);
				EditorUtility.DisplayProgressBar("checking", "analytics codes", (float)++count / codes.Length * 0.5f + 0.5f);
			}

			count = 0;
			foreach (var codepath in codes)
			{
				CollectionReferenceClasses(AssetDatabase.AssetPathToGUID(codepath), alltypes);
				EditorUtility.DisplayProgressBar("checking", "analytics codes", (float)++count / codes.Length * 0.5f);
			}
		}

		string[] GetFirstPassList()
		{
			string[] plugins = new string[0];
			string[] stdAssets = new string[0];

			if (Directory.Exists("Assets/Plugins"))
				plugins = Directory.GetFiles("Assets/Plugins", "*.cs", SearchOption.AllDirectories);
			if (Directory.Exists("Assets/Standard Assets"))
				stdAssets = Directory.GetFiles("Assets/Standard Assets", "*.cs", SearchOption.AllDirectories);

			string[] newArray = new string[plugins.Length + stdAssets.Length];
			Array.Copy(plugins, newArray, plugins.Length);
			Array.Copy(stdAssets, 0, newArray, plugins.Length, stdAssets.Length);

			return newArray;
		}

		void CollectionCodeFileDictionary(List<Type> alltypes, string[] codes)
		{
			float count = 1;
			foreach (var codePath in codes)
			{
				EditorUtility.DisplayProgressBar("checking", "search files", count++ / codes.Length);

				// connect file and classes.
				var code = File.ReadAllText(codePath);
				code = Regex.Replace(code, "//.*[\\n\\r]", "");
				code = Regex.Replace(code, "/\\*.*[\\n\\r]\\*/", "");

				foreach (var type in alltypes)
				{
					if (CodeFileList.ContainsKey(type) == false)
						CodeFileList.Add(type, new List<string>());

					var list = CodeFileList[type];

					if (string.IsNullOrEmpty(type.Namespace) == false)
					{
						var namespacepattern = string.Format("namespace[\\s.]{0}[{{\\s\\n]", type.Namespace);
						if (!Regex.IsMatch(code, namespacepattern))
							continue;
					}

					string typeName = type.IsGenericTypeDefinition
						? type.GetGenericTypeDefinition().Name.Split('`')[0]
						: type.Name;

					if (Regex.IsMatch(code, string.Format("class\\s*{0}?[\\s:<{{]", typeName)))
					{
						list.Add(AssetDatabase.AssetPathToGUID(codePath));
						continue;
					}

					if (Regex.IsMatch(code, string.Format("struct\\s*{0}[\\s:<{{]", typeName)))
					{
						list.Add(AssetDatabase.AssetPathToGUID(codePath));
						continue;
					}

					if (Regex.IsMatch(code, string.Format("enum\\s*{0}[\\s{{]", type.Name)))
					{
						list.Add(AssetDatabase.AssetPathToGUID(codePath));
						continue;
					}

					if (Regex.IsMatch(code, string.Format("delegate\\s*{0}\\s\\(", type.Name)))
						list.Add(AssetDatabase.AssetPathToGUID(codePath));
				}
			}
		}

		List<Type> GetAllClasses()
		{
			List<Type> alltypes = new List<Type>();

			if (File.Exists("Library/ScriptAssemblies/Assembly-CSharp.dll"))
				alltypes.AddRange(Assembly.LoadFile("Library/ScriptAssemblies/Assembly-CSharp.dll").GetTypes());
			if (File.Exists("Library/ScriptAssemblies/Assembly-CSharp-Editor.dll"))
				alltypes.AddRange(Assembly.LoadFile("Library/ScriptAssemblies/Assembly-CSharp-Editor.dll").GetTypes());

			return alltypes.ToList();
		}

		List<Type> GetAllFirstpassClasses()
		{
			List<Type> alltypes = new List<Type>();
			if (File.Exists("Library/ScriptAssemblies/Assembly-CSharp-firstpass.dll"))
				alltypes.AddRange(Assembly.LoadFile("Library/ScriptAssemblies/Assembly-CSharp-firstpass.dll").GetTypes());
			if (File.Exists("Library/ScriptAssemblies/Assembly-CSharp-Editor-firstpass.dll"))
				alltypes.AddRange(Assembly.LoadFile("Library/ScriptAssemblies/Assembly-CSharp-Editor-firstpass.dll").GetTypes());
			return alltypes;
		}

		void CollectionReferenceClasses(string guid, List<Type> types)
		{
			var codePath = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(codePath) || References.ContainsKey(guid) || !File.Exists(codePath))
				return;

			var code = File.ReadAllText(codePath);
			code = Regex.Replace(code, "//.*[\\n\\r]", "");
			code = Regex.Replace(code, "/\\*.*[\\n\\r]\\*/", "");

			var list = new List<Type>();
			References[guid] = list;

			foreach (var type in types)
			{
				if (!string.IsNullOrEmpty(type.Namespace))
				{
					var namespacepattern = string.Format("[namespace|using][\\s\\.]{0}[{{\\s\\r\\n\\r;]", type.Namespace);
					if (!Regex.IsMatch(code, namespacepattern))
						continue;
				}

				if (!CodeFileList.ContainsKey(type))
					continue;

				string match = type.IsGenericTypeDefinition
					? string.Format("[\\]\\[\\.\\s<(]{0}[\\.\\s\\n\\r>,<(){{]", type.GetGenericTypeDefinition().Name.Split('`')[0])
					: string.Format("[\\]\\[\\.\\s<(]{0}[\\.\\s\\n\\r>,<(){{\\]]", type.Name.Replace("Attribute", ""));

				if (!Regex.IsMatch(code, match))
					continue;

				list.Add(type);
				var typeGuid = CodeFileList[type];
				foreach (var referenceGuid in typeGuid)
					CollectionReferenceClasses(referenceGuid, types);
			}
		}
	}
}

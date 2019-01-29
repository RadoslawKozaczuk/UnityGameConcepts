using System.Collections.Generic;
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
		public Dictionary<System.Type, List<string>> CodeFileList = new Dictionary<System.Type, List<string>>();
		// guid : types
		public Dictionary<string, List<System.Type>> References = new Dictionary<string, List<System.Type>>();

		public void Collection()
		{
			References.Clear();
			EditorUtility.DisplayProgressBar("checking", "collection all type", 0);

			// Connect the files and class.
			var codes = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);
			// connect each classes.
			var firstPassList = new List<string>();
			if (Directory.Exists("Assets/Plugins"))
				firstPassList.AddRange(Directory.GetFiles("Assets/Plugins", "*.cs", SearchOption.AllDirectories));
			if (Directory.Exists("Assets/Standard Assets"))
				firstPassList.AddRange(Directory.GetFiles("Assets/Standard Assets", "*.cs", SearchOption.AllDirectories));

			var allFirstpassTypes = CollectionAllFastspassClasses();
			CollectionCodeFileDictionary(allFirstpassTypes, firstPassList.ToArray());


			var alltypes = CollectionAllClasses();
			CollectionCodeFileDictionary(alltypes, codes.ToArray());
			alltypes.AddRange(allFirstpassTypes);

			int count = 0;
			foreach (var codepath in firstPassList)
			{
				CollectionReferenceClasses(AssetDatabase.AssetPathToGUID(codepath), allFirstpassTypes);
				EditorUtility.DisplayProgressBar("checking", "analytics codes", ((float)++count / codes.Length) * 0.5f + 0.5f);
			}
			count = 0;
			foreach (var codepath in codes)
			{
				CollectionReferenceClasses(AssetDatabase.AssetPathToGUID(codepath), alltypes);
				EditorUtility.DisplayProgressBar("checking", "analytics codes", ((float)++count / codes.Length) * 0.5f);
			}
		}

		void CollectionCodeFileDictionary(List<System.Type> alltypes, string[] codes)
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
					{
						CodeFileList.Add(type, new List<string>());
					}
					var list = CodeFileList[type];

					if (string.IsNullOrEmpty(type.Namespace) == false)
					{
						var namespacepattern = $"namespace[\\s.]{type.Namespace}[{{\\s\\n]";
						if (Regex.IsMatch(code, namespacepattern) == false)
						{
							continue;
						}
					}

					string typeName = type.IsGenericTypeDefinition ? type.GetGenericTypeDefinition().Name.Split('`')[0] : type.Name;
					if (Regex.IsMatch(code, $"class\\s*{typeName}?[\\s:<{{]"))
					{
						list.Add(AssetDatabase.AssetPathToGUID(codePath));
						continue;
					}

					if (Regex.IsMatch(code, $"struct\\s*{typeName}[\\s:<{{]"))
					{
						list.Add(AssetDatabase.AssetPathToGUID(codePath));
						continue;
					}

					if (Regex.IsMatch(code, $"enum\\s*{type.Name}[\\s{{]"))
					{
						list.Add(AssetDatabase.AssetPathToGUID(codePath));
						continue;
					}

					if (Regex.IsMatch(code, $"delegate\\s*{type.Name}\\s\\("))
					{
						list.Add(AssetDatabase.AssetPathToGUID(codePath));
					}
				}
			}
		}

		List<System.Type> CollectionAllClasses()
		{
			List<System.Type> alltypes = new List<System.Type>();

			if (File.Exists("Library/ScriptAssemblies/Assembly-CSharp.dll"))
				alltypes.AddRange(Assembly.LoadFile("Library/ScriptAssemblies/Assembly-CSharp.dll").GetTypes());
			if (File.Exists("Library/ScriptAssemblies/Assembly-CSharp-Editor.dll"))
				alltypes.AddRange(Assembly.LoadFile("Library/ScriptAssemblies/Assembly-CSharp-Editor.dll").GetTypes());

			return alltypes.ToList();
		}

		List<System.Type> CollectionAllFastspassClasses()
		{
			List<System.Type> alltypes = new List<System.Type>();
			if (File.Exists("Library/ScriptAssemblies/Assembly-CSharp-firstpass.dll"))
				alltypes.AddRange(Assembly.LoadFile("Library/ScriptAssemblies/Assembly-CSharp-firstpass.dll").GetTypes());
			if (File.Exists("Library/ScriptAssemblies/Assembly-CSharp-Editor-firstpass.dll"))
				alltypes.AddRange(Assembly.LoadFile("Library/ScriptAssemblies/Assembly-CSharp-Editor-firstpass.dll").GetTypes());
			return alltypes;
		}

		void CollectionReferenceClasses(string guid, List<System.Type> types)
		{
			var codePath = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(codePath) || References.ContainsKey(guid) || File.Exists(codePath) == false)
			{
				return;
			}

			var code = File.ReadAllText(codePath);
			code = Regex.Replace(code, "//.*[\\n\\r]", "");
			code = Regex.Replace(code, "/\\*.*[\\n\\r]\\*/", "");

			var list = new List<System.Type>();
			References[guid] = list;

			foreach (var type in types)
			{

				if (string.IsNullOrEmpty(type.Namespace) == false)
				{
					var namespacepattern = $"[namespace|using][\\s\\.]{type.Namespace}[{{\\s\\r\\n\\r;]";
					if (Regex.IsMatch(code, namespacepattern) == false)
					{
						continue;
					}
				}

				if (CodeFileList.ContainsKey(type) == false)
				{
					continue;
				}

				string match = type.IsGenericTypeDefinition
					? $"[\\]\\[\\.\\s<(]{type.GetGenericTypeDefinition().Name.Split('`')[0]}[\\.\\s\\n\\r>,<(){{]"
					: $"[\\]\\[\\.\\s<(]{type.Name.Replace("Attribute", "")}[\\.\\s\\n\\r>,<(){{\\]]";

				if (!Regex.IsMatch(code, match)) continue;
				list.Add(type);
				var typeGuid = CodeFileList[type];
				foreach (var referenceGuid in typeGuid)
				{
					CollectionReferenceClasses(referenceGuid, types);
				}
			}
		}
	}
}

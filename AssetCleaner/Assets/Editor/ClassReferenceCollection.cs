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
		const string ProgressBarTitle = "Searching for unused assets";

		// type : guid
		public Dictionary<Type, List<string>> CodeFileList = new Dictionary<Type, List<string>>();
		// guid : types
		public Dictionary<string, List<Type>> References = new Dictionary<string, List<Type>>();

		public void Collection()
		{
			References.Clear();
			EditorUtility.DisplayProgressBar(ProgressBarTitle, "collection all type", 0);

			var firstPassList = GetFirstPassList();
			var allFirstpassTypes = GetAllFirstpassClasses();
			CollectionCodeFileDictionary(allFirstpassTypes, firstPassList, "searching files phase 1");

			var codes = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);
			var alltypes = GetAllClassTypes();
			CollectionCodeFileDictionary(alltypes, codes, "searching files phase 2");
			alltypes.AddRange(allFirstpassTypes);

			float count = 0;
			foreach (var codepath in firstPassList)
			{
				CollectionReferenceClasses(AssetDatabase.AssetPathToGUID(codepath), allFirstpassTypes);
				EditorUtility.DisplayProgressBar(ProgressBarTitle, "analytics codes 1", ++count / codes.Length);
			}

			count = 0;
			foreach (var codepath in codes)
			{
				CollectionReferenceClasses(AssetDatabase.AssetPathToGUID(codepath), alltypes);
				EditorUtility.DisplayProgressBar(ProgressBarTitle, "analytics codes 2", ++count / codes.Length);
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

		void CollectionCodeFileDictionary(List<Type> alltypes, string[] codes, string progressBarDescription)
		{
			float count = 0;
			foreach (var codePath in codes)
			{
				EditorUtility.DisplayProgressBar(ProgressBarTitle, progressBarDescription, ++count / codes.Length);

				var code = File.ReadAllText(codePath);
				code = Regex.Replace(code, @"\s", "");

				foreach (var type in alltypes)
				{
					if (CodeFileList.ContainsKey(type) == false)
						CodeFileList.Add(type, new List<string>());

					if (string.IsNullOrEmpty(type.Namespace) == false)
					{
						var namespacepattern = string.Format("namespace[\\s.]{0}[{{\\s\\n]", type.Namespace);
						if (!Regex.IsMatch(code, namespacepattern))
							continue;
					}

					var list = CodeFileList[type];

					string typeName = type.IsGenericTypeDefinition
						? type.GetGenericTypeDefinition().Name.Split('`')[0]
						: type.Name;

					if (Regex.IsMatch(code, string.Format("class{0}|struct{0}", typeName)))
						list.Add(AssetDatabase.AssetPathToGUID(codePath));
					else if (Regex.IsMatch(code, string.Format("enum{0}|delegate{0}", type.Name)))
						list.Add(AssetDatabase.AssetPathToGUID(codePath));
				}
			}
		}

		List<Type> GetAllClassTypes()
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

			var code = Regex.Replace(File.ReadAllText(codePath), @"\s", "");

			var list = new List<Type>();
			References[guid] = list;

			foreach (var type in types)
			{
				if (!string.IsNullOrEmpty(type.Namespace))
				{
					var namespacepattern = string.Format("[namespace|using][\\.]{0}]", type.Namespace);
					if (!Regex.IsMatch(code, namespacepattern))
						continue;
				}

				if (!CodeFileList.ContainsKey(type))
					continue;

				string match = type.IsGenericTypeDefinition
					? string.Format("{0}", type.GetGenericTypeDefinition().Name.Split('`')[0])
					: string.Format("{0}", type.Name.Replace("Attribute", ""));

				if (!Regex.IsMatch(code, match))
					continue;

				list.Add(type);

				foreach (var referenceGuid in CodeFileList[type])
					CollectionReferenceClasses(referenceGuid, types);
			}
		}
	}
}

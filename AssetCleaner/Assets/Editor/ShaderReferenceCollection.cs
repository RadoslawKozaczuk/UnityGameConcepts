using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Assets.Editor
{
	public class ShaderReferenceCollection
	{
		// shader name / shader file guid
		public Dictionary<string, string> ShaderFileList = new Dictionary<string, string>();
		public Dictionary<string, List<string>> ShaderReferenceList = new Dictionary<string, List<string>>();

		public void Collection()
		{
			CollectionShaderFiles();
			CheckReference();
		}

		void CollectionShaderFiles()
		{
			var shaderFiles = Directory.GetFiles(FindUnusedAssets.ComponentsDir, "*.shader", SearchOption.AllDirectories);
			foreach (var shaderFilePath in shaderFiles)
			{
				var code = File.ReadAllText(shaderFilePath);
				var match = Regex.Match(code, "Shader \"(?<name>.*)\"");

				if (match.Success)
				{
					var shaderName = match.Groups["name"].ToString();
					if (ShaderFileList.ContainsKey(shaderName) == false)
						ShaderFileList.Add(shaderName, AssetDatabase.AssetPathToGUID(shaderFilePath));
				}
			}

			var cgFiles = Directory.GetFiles(FindUnusedAssets.ComponentsDir, "*.cg", SearchOption.AllDirectories);
			foreach (var cgFilePath in cgFiles)
			{
				var file = Path.GetFileName(cgFilePath);
				ShaderFileList.Add(file, cgFilePath);
			}

			var cgincFiles = Directory.GetFiles(FindUnusedAssets.ComponentsDir, "*.cginc", SearchOption.AllDirectories);
			foreach (var cgincPath in cgincFiles)
			{
				var file = Path.GetFileName(cgincPath);
				ShaderFileList.Add(file, cgincPath);
			}
		}

		void CheckReference()
		{
			foreach (var shader in ShaderFileList)
			{
				var shaderFilePath = AssetDatabase.GUIDToAssetPath(shader.Value);
				var shaderName = shader.Key;
				var referenceList = new List<string>();

				ShaderReferenceList.Add(shaderName, referenceList);
				var code = File.ReadAllText(shaderFilePath);

				foreach (var checkingShaderName in ShaderFileList.Keys)
				{
					if (Regex.IsMatch(code, string.Format("{0}", checkingShaderName)))
					{
						var filePath = ShaderFileList[checkingShaderName];
						referenceList.Add(filePath);
					}
				}
			}
		}
	}
}
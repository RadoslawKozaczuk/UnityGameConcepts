using UnityEngine;
using UnityEditor;

public class TextureArrayWizard : ScriptableWizard
{
	public Texture2D[] Textures;

	[MenuItem("Assets/Create/Texture Array")]
	static void CreateWizard() => DisplayWizard<TextureArrayWizard>("Create Texture Array", "Create");

	void OnWizardCreate()
	{
		if (Textures.Length == 0)
			return;

		// As the texture array is a single GPU resource, it uses the same filter and wrap modes for all textures.
		Texture2D t = Textures[0];
		Texture2DArray textureArray = new Texture2DArray(
			t.width, t.height, Textures.Length, t.format, t.mipmapCount > 1
		)
		{
			anisoLevel = t.anisoLevel,
			filterMode = t.filterMode,
			wrapMode = t.wrapMode
		};

		for (int i = 0; i < Textures.Length; i++)
		{
			// CopyTexture method copies the raw texture data, one mip level at a time.
			// So we have to loop through all textures and their mip levels.
			for (int m = 0; m < t.mipmapCount; m++)
				Graphics.CopyTexture(Textures[i], 0, m, textureArray, i, m);
		}

		string path = EditorUtility.SaveFilePanelInProject(
			"Save Texture Array", "Texture Array", "asset", "Save Texture Array");

		if (path.Length == 0)
			return;

		// convert the array to an asset
		AssetDatabase.CreateAsset(textureArray, path);
	}
}
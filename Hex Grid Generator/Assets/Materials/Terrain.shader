Shader "Custom/Terrain" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Terrain Texture Array", 2DArray) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows vertex:vert
		#pragma target 3.5

		UNITY_DECLARE_TEX2DARRAY(_MainTex);

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		struct Input 
		{
			float4 color : COLOR;
			float3 worldPos;
			float3 terrain;
		};

		// we add a float3 terrain field to the input structure and copy v.texcoord2.xyz to it
		void vert(inout appdata_full v, out Input data) 
		{
			UNITY_INITIALIZE_OUTPUT(Input, data);
			data.terrain = v.texcoord2.xyz;
		}

		// We have to sample the texture array three times per fragment. 
		// So let's create a convenient function to construct the texture coordinates, sample the array, 
		// and modulate the sample with the splat map for one index.
		float4 GetTerrainColor(Input IN, int index) 
		{
			float3 uvw = float3(IN.worldPos.xz * 0.02, IN.terrain[index]);
			float4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uvw);
			return c * IN.color[index];
		}

		void surf(Input IN, inout SurfaceOutputStandard o) 
		{
			fixed4 c = GetTerrainColor(IN, 0) +	GetTerrainColor(IN, 1) + GetTerrainColor(IN, 2);
			o.Albedo = c.rgb * _Color;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}

	FallBack "Diffuse"
}
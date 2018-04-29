// how to name it and where to put it
Shader "MyShader/HelloShader" {

	Properties{
		_myColor("Example Color", Color) = (1, 1, 1, 1) // this is color picker
		_myEmission("Example Emission", Color) = (1, 1, 1, 1)
	}

	SubShader {

		// this part is coded in HLSL (High Level Shader Language)
		CGPROGRAM // Start Tag

			// compiler directive telling Unity how the component should be used
			// shader type / name of the function containing shader / lighting type
			#pragma surface surf Lambert

			// input data like vertices, normals, uvs, etc. 
			struct Input {
				float2 uvMainTex;
			};

			// in order for properties at the top be visible here we need to declare them here
			fixed4 _myColor;
			fixed4 _myEmission;

			void surf(Input IN, inout SurfaceOutput o) {
				o.Albedo = _myColor.rgb;
				o.Emission = _myEmission.rgb;
			}

		ENDCG // End Tag
	}

	FallBack "Diffuse"
}
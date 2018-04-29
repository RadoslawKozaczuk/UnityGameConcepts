Shader "MyShader/PackedPractice" {

	// the purpose of uvs is to allow mapping part of the texture on the polygon
	// uvs are always ordered in anticlockwise order on the face in which the normal face the viewer

	Properties{
		_myColor("Example Color", Color) = (1,1,1,1)
	}

	SubShader {

		CGPROGRAM
			#pragma surface surf Lambert

			struct Input {
				float2 uvMainTex;

				// examples of other input values:

				// float3 viewDir; // information about the angle the model is being viewed from
				// this allows to write shaders that changes depends on the angle 

				// float3 worldPos; // stores information about the coordinates being processed
				// allows to show or not to show the material of the surface based on the location
			};

			fixed4 _myColor;

			void surf(Input IN, inout SurfaceOutput o) {
				o.Albedo.rg = _myColor.xy;
			}

		ENDCG
	}

	FallBack "Diffuse"
}

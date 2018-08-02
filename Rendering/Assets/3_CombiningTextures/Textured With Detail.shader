// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Textured With Detail" {

	// Shader properties are declared in a separate block.
	Properties{
		_Tint("Tint", Color) = (1, 1, 1, 1) // assignment of a defult value
		_MainTex("Texture", 2D) = "white" {} // these bracess are useless but necessary

		// Detail textures don't have to be grayscale, but they commonly are. 
		// Grayscale detail textures will adjust the original color strictly by brightening and darkening it. 
		// This is relatively straightforward to work with. Multiplication with non-gray colors produces less intuitive results. 
		// But nothing is stopping you from doing it. Colored detail textures are used to produce subtle color shifts.
		_DetailTex("Detail Texture", 2D) = "gray" {}
	}

	SubShader{

		// A shader pass is where an object actually gets rendered. 
		// We'll use one pass, but it's possible to have more. 
		// Having more than one pass means that the object gets rendered multiple times, which is required for a lot of effects.
		Pass{

		// Shader passes can contain other statements besides the shader program. So the program has to be separated somehow.
		CGPROGRAM
			#include "UnityCG.cginc"

			// Shaders consist of two programs each.
			// The vertex program is responsible for processing the vertex data of a mesh. 
			// This includes the conversion from object space to display space. 
			#pragma vertex MyVertexProgram

			// The fragment program is responsible for coloring individual pixels that lie inside the mesh's triangles.
			#pragma fragment MyFragmentProgram
			
			float4 _Tint;
			sampler2D _MainTex, _DetailTex;
			// ST stands for Scale and Translation but nowadays it refers to Tiling and Offset (backward compatibility)
			float4 _MainTex_ST, _DetailTex_ST; 

			struct Interpolators {
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 uvDetail : TEXCOORD1;
			};

			struct VertexData {
				float4 position : POSITION;
				float2 uv : TEXCOORD0;
			};

			Interpolators MyVertexProgram(VertexData v) {
				Interpolators i;
				i.position = UnityObjectToClipPos(v.position);
				i.uv = TRANSFORM_TEX(v.uv, _MainTex);
				i.uvDetail = TRANSFORM_TEX(v.uv, _DetailTex);
				return i;
			}

			float4 MyFragmentProgram(Interpolators i) : SV_TARGET{
				float4 color = tex2D(_MainTex, i.uv) * _Tint;
				color *= tex2D(_DetailTex, i.uvDetail) 
					* unity_ColorSpaceDouble; // makes our detail material look the same no matter which color space we're rendering in.
				return color;
			}
			
		ENDCG
		}
	}
}
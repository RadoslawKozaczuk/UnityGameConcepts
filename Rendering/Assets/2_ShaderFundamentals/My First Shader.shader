// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/My First Shader" {

	// Shader properties are declared in a separate block.
	Properties{
		// Variables can have any name, but the convention is to start with an underscore 
		// followed by a capital letter, and lowercase after that to avoid diplications.
		// The property name must be followed by a string and a type, in parenthesis, as if you're invoking a method. 
		// The string is used to label the property in the material inspector.
		_Tint("Tint", Color) = (1, 1, 1, 1) // assignment of a defult value
		_MainTex("Texture", 2D) = "white" {} // these bracess are useless but necessary
	}

	// It is possible to have many SubShaders. 
	// This allows us to provide different sub-shaders for different build platforms or levels of detail
	SubShader{

		// A shader pass is where an object actually gets rendered. 
		// We'll use one pass, but it's possible to have more. 
		// Having more than one pass means that the object gets rendered multiple times, which is required for a lot of effects.
		Pass{

		// Shader passes can contain other statements besides the shader program. So the program has to be separated somehow.
		CGPROGRAM
		
			// Shaders consist of two programs each.
			// The vertex program is responsible for processing the vertex data of a mesh. 
			// This includes the conversion from object space to display space. 
			#pragma vertex MyVertexProgram

			// The fragment program is responsible for coloring individual pixels that lie inside the mesh's triangles.
			#pragma fragment MyFragmentProgram

			// We can split the code into multiple files.
			// This lib includes a few other essential files, and contains some generic functionality.
			#include "UnityCG.cginc"

			// To actually use the property, we have to add a variable to the shader code. Its name has to exactly match the property name.
			// Variable has to be defined before it can be used. The compiler works from top to bottom.
			float4 _Tint;
			sampler2D _MainTex;
			float4 _MainTex_ST; // ST stands for Scale and Translation but nowadays it refers to Tiling and Offset (backward compatibility)

			struct Interpolators {
				float4 position : SV_POSITION;
				float3 localPosition : TEXCOORD1;
				float2 uv : TEXCOORD0;
			};

			struct VertexData {
				float4 position : POSITION;
				float2 uv : TEXCOORD0;
			};

			// The vertex program has to return the final coordinates of a vertex. 
			// How many coordinates? Four, because we're using 4 by 4 transformation matrices
			// So it doesn't know what the GPU should do with it. We have to be very specific about the output of our program.
			Interpolators MyVertexProgram(VertexData v)
			{
				Interpolators i;
				i.localPosition = v.position.xyz; // grab the first three components of the position
				i.position = UnityObjectToClipPos(v.position);
				
				//i.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw; // tiling is stored in first two variables and offset in the last two
				
				i.uv = TRANSFORM_TEX(v.uv, _MainTex); // UnityCG.cginc contains a handy macro that simplifies this boilerplate for us
				
				return i;
			}

			// The fragment program is supposed to output an RGBA color value for one pixel. 
			// We can use a float4 for that as well.Returning 0 will produce solid black.
			float4 MyFragmentProgram(Interpolators i)
				: SV_TARGET // Where the final color should be written to. We use SV_TARGET, which is the default shader target. 
							// This is the frame buffer, which contains the image that we are generating.
			{
				// + 0.5 because negative colors get clamped to zero
				// return float4(i.localPosition + 0.5, 1) * _Tint; // We can output the position as if it were a color.
				
				//return float4(i.uv, 1, 1); // we can out put the UV as it were a color.

				return tex2D(_MainTex, i.uv) * _Tint;
			}
			
		ENDCG
		}
	}
}
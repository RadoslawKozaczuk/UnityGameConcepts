/* Shader Language basic information
	1) The code we write is only a code that is necessary to generate a shader
	2) Shaders operate per pixel or per vertex - we don't need to write loops GPU does that
	3) Variables are different:
		- float - 32 bits, equivalent to C# float
			used for world positions, texture coordinates and calculations
		- half - 16 bits, half of a float
			used for short vectors, directions and dynamic color ranges
		- fixed - 11 bits, lowest precision float
			used in regular colors and simple color operations
		- int - 32 bits, like a C# int
			used for counters and array indexes
	4) Texture Data Types
		- sampler2D
		- samplerCUBE
		each of this has high (float) and low (half) precision versions
	5) Packed Arrays - a convenient way to make an array
		for example: fixed4 or int3
		but they can be accessed like a structure (r,g,b,a) or (x,y,z,w)
		for example color1.r == color1.x // true
		it is also possible to assign range of values 
		color1 = color2.rgb
	6) for complicated calculation there are Packed Matrices
		float4x4 matrix; // declaration
		accessing is weird:
		float myVal = matrix._m20; // access Row2 Col0
*/


// how to name it and where to put it
Shader "Custom/HelloShader" {

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
Shader "Custom/GPUInstancing" {
	/* === GPU Instancing ===
	Use GPU Instancing to draw (or render) multiple copies of the same Mesh at once, using a small number of draw calls.
	It is useful for drawing objects such as buildings, trees and grass, or other things that appear repeatedly in a Scene.

	GPU Instancing only renders identical Meshes with each draw call, but each instance can have different parameters
	(for example, color or scale) to add variation and reduce the appearance of repetition.

	GPU Instancing can reduce the number of draw calls used per Scene.
	This significantly improves the rendering performance of your project.

	For these changes to take effect, you must enable GPU Instancing.
	To do this, select your Shader in the Project window, and in the Inspector, tick the Enable Instancing checkbox.
	*/
	Properties{
		_Color("Color", Color) = (1, 1, 1, 1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}

	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		// Use this to instruct Unity to assume that all the instances have uniform scalings 
		// (the same scale for all X, Y and Z axes).
		// Assuming uniform scaling makes instancing more efficient as it requires less data and work 
		// because all our shapes use a uniform scale.	
		#pragma instancing_options assumeuniformscaling

		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;

		// Every per - instance property must be defined in a specially named constant buffer.
		// Use this pair of macros (END and START) to wrap the properties you want to be made unique to each instance.
		UNITY_INSTANCING_BUFFER_START(Props)
		UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color) // add property to an array named 'Props'
		// if something is defined here it doesn't need to be defined above
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf(Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) 
				// Use this to access a per-instance Shader property declared in an instancing constant buffer. 
				// It uses an instance ID to index into the instance data array. 
				// The arrayName in the macro must match the one in UNITY_INSTANCING_BUFFER_END(name) macro.
				* UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
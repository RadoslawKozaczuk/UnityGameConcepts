Shader "Custom/Water" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard alpha

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			float2 uv1 = IN.worldPos.xz;
			uv1.y += _Time.y;
			float4 noise1 = tex2D(_MainTex, uv1 * 0.025);
			
			float2 uv2 = IN.worldPos.xz;
			uv2.x += _Time.y; // this time we modify the U channel
			float4 noise2 = tex2D(_MainTex, uv2 * 0.025);

			// We produce a blend wave by creating a sine wave that runs diagonally across the water surface. 
			// We do that by adding the X and Z world coordinates together and using that as input of the sin function. 
			// Scale them down so that we get reasonably large bands.
			float blendWave = sin((IN.worldPos.x + IN.worldPos.z) * 0.1 
				+ (noise1.y + noise2.z) + _Time.y); // we add noise to make the wave less obvious
			blendWave *= blendWave; // we square the wave to convert the output from <-1, 1> to <0, 1>

			// Summing both samples can produces results in the 0–2 range, so we have to scale that back to 0–1. 
			// Instead of just halving the waves, we can use the smoothstep function to create a more interesting result. 
			// We'll map ¾–2 to 0–1, so part of the water surface ends up without visible waves.
			float waves = lerp(noise1.z, noise1.w, blendWave) + lerp(noise2.x, noise2.y, blendWave);
			waves = smoothstep(0.75, 2, waves);


			fixed4 c = saturate(_Color + waves);   
			o.Albedo = c.rgb;   
			o.Metallic = _Metallic;   
			o.Smoothness = _Glossiness;   
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}

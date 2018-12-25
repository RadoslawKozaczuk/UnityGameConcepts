﻿Shader "Custom/River" {
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
			float2 uv = IN.uv_MainTex; 
			// As we're only using a small strip of the noise, we could vary the pattern by sliding the strip across the texture. 
			// This is done by adding time to the U coordinate. We have to make sure to change it slowly, otherwise the river will appear to ﬂow sideways.
			uv.x += 0.0625 + _Time.y * 0.005;
			uv.y -= _Time.y * 0.25;
			float4 noise = tex2D(_MainTex, uv);

			float2 uv2 = IN.uv_MainTex;
			uv2.x = uv2.x * 0.0625 - _Time.y * 0.0052; 
			uv2.y -= _Time.y * 0.23; 
			float4 noise2 = tex2D(_MainTex, uv2);

			fixed4 c = saturate(_Color + noise.r * noise2.a);
			o.Albedo = c.rgb; 
			o.Metallic = _Metallic; 
			o.Smoothness = _Glossiness; 
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}

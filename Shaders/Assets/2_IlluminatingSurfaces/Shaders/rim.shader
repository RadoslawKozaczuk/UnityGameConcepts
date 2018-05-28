Shader "Custom/Rim" {
	Properties {
		_RimColor("Rim Color", Color) = (0,0.5,0.5,0.0)
		_RimColor("Rim Power", Range(0.5, 8.0)) = 3.0
	}

	SubShader {
		CGPROGRAM
		#pragma surface surf Lambert
		struct Input {
			float3 viewDir;
		};
		
		float4 _RimColor;
		float _RimPower;
		
		void surf (Input IN, inout SurfaceOutput o) {
			half rim = 1 - saturate(dot(normalize(IN.viewDir), o.Normal));
			// multiplay by the power to change the curve int something more sinusoidal
			o.Emission = (_RimColor.rgb * pow(rim, 3 )) > 0.4 ? rim:0;
		}
		ENDCG
	}
	FallBack "Diffuse"
}

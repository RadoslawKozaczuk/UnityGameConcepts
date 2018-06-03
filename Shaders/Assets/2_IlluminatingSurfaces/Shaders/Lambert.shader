Shader "Custom/Lambert" {

	Properties{
		_Colour("Colour", Color) = (1,1,1,1)
		_SpecColor("Spec Colour", Color) = (1,1,1,1)
		_Spec("Specular", Range(0,1)) = 0.5
		_Gloss("Gloss", Range(0,1)) = 0.5

		_MainTex("Texture", 2D) = "white" {}
		_myBump("Bump Texture", 2D) = "bump" {}
	}

	SubShader
	{
		Tags{ "Queue" = "Geometry"}

		CGPROGRAM
		#pragma surface surf Lambert

		float4 _Colour;
		half _Spec;
		fixed _Gloss;

		sampler2D _MainTex;
		sampler2D _myBump;

		struct Input {
			float2 uv_MainTex;
			float2 uv_myBump;
		};

		void surf(Input IN, inout SurfaceOutput o) {
			fixed4 a = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = a.rgb;
			o.Normal = UnpackNormal(tex2D(_myBump, IN.uv_myBump));
			o.Albedo = a.rgb;
			o.Specular = _Spec;
			o.Gloss = _Gloss;
		}
		ENDCG
	}
	FallBack "Diffuse"
}

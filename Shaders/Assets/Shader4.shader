Shader "Custom/Shader4" {
	Properties {

		// first parameter is type, second is type too I guess, then start color and actual value
		_diffuseTex("Texture", 2D) = "white" {}

		//set this texture to black to stop the white
		//overwhelming the effect if no emission texture
		//is present
		_emissionTex("Texture", 2D) = "black" {}
	}

	SubShader
	{
		CGPROGRAM
			#pragma surface surf Lambert

			// corresponding variables - names have to match
			sampler2D _diffuseTex;
			sampler2D _emissionTex;

			struct Input {
				// this has to match up with the name above
				// to access second uv set simply write 'uv2'
				float2 uv_diffuseTex;
				float2 uv_emissionTex;
			};

			void surf(Input IN, inout SurfaceOutput o) {
				// this tex2D function is part of the HLSL language
				// we multiply color channels by a value
				o.Albedo = (tex2D(_diffuseTex, IN.uv_diffuseTex)).rgb;
				o.Emission = (tex2D(_emissionTex, IN.uv_emissionTex)).rgb;
			}

		ENDCG
	}

	Fallback "Diffuse"
}

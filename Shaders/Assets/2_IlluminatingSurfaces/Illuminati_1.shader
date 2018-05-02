Shader "Custom/Illuminati_1" {
	Properties {

		// normal is just a normal texture where x color value is mapped to <-1, 1>
		// y the same is mapped from <0, 255> to <-1, 1>
		// and z is mapped from <128, 255> to <-1, 0> (thats why normals are so blueish)
		// after mapping we get matrix of vectors where each vector represent the angle of light.

		// bumpmaps change the way light is calculated but no the geometry

		// first parameter is type, second is type too I guess, then start color and actual value
		_myTexture("Diffuse Texture", 2D) = "white" {}

		// this "bump" is mandatory if we want to create a bump map
		_myBump("Bump Texture", 2D) = "bump" {}

		_myBumpAmount("Bump Amount", Range(0, 10)) = 1
		_myBrightness("Brightness", Range(0, 10)) = 1
	}

	SubShader
	{
		CGPROGRAM
			#pragma surface surf Lambert

			// corresponding variables - names have to match
			sampler2D _myTexture;
			sampler2D _myBump;
			half _myBumpAmount;
			half _myBrightness;

			struct Input {
				// this has to match up with the name above
				// to access second uv set simply write 'uv2'
				float2 uv_myTexture;
				float2 uv_myBump;
			};

			void surf(Input IN, inout SurfaceOutput o) {
				o.Albedo = tex2D(_myTexture, IN.uv_myTexture).rgb;
				o.Normal = UnpackNormal(tex2D(_myBump, IN.uv_myBump)) * _myBrightness;
				o.Normal *= float3(_myBumpAmount, _myBumpAmount, 1);
			}

		ENDCG
	}

	Fallback "Diffuse"
}

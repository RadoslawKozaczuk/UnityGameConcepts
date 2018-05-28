Shader "Custom/DotProduct" {

	SubShader {
		CGPROGRAM
		#pragma surface surf Lambert

		// dot product
		// ||a|| * ||b|| * cos(angle)
		// used when we want to process the surface based on where we look at it from
		// used for example for outlining

		// dot product of parallel vectors is 1 
		// when they face in opposite direction the result is -1
		// and when they are right angle the result is 0
		
		struct Input {
			float3 viewDir; // it points directly at the screen
		};

		void surf (Input IN, inout SurfaceOutput o) {
			// dot product of the viewDir and normals allows us to know 
			// if the surface points at the plater and how much
			half dotp = dot(IN.viewDir, o.Normal);

			// red channel is out dot product 
			// so when the view direction channel is in line with the normal we get (1, 1, 1) so white
			o.Albedo = float3(dotp, 1, 1);
		}
		ENDCG
	}
	FallBack "Diffuse"
}

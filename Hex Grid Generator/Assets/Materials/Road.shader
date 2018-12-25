Shader "Custom/Road"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { 
			"RenderType"="Opaque"
			// We want to always draw the roads after the terrain has been drawn. 
			// This is accomplished by rendering them after the regular geometry is drawn, by putting them in a later render queue.
			"Queue" = "Geometry+1"
		}
        LOD 200
		// We want to make sure that the roads are drawn on top of the terrain triangles that sit in the same position. 
		// We do this by adding a depth test offset. 
		// This lets the GPU treat the triangles as if they are closer to the camera than they really are.
		Offset -1, -1

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows decal:blend // that gives us alpha-blended shader instead of opaque

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
			float3 worldPos; // we use world position as a parameter for our noise function
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

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
			float4 noise = tex2D(_MainTex, IN.worldPos.xz * 0.025);

            //fixed4 c = fixed4(IN.uv_MainTex, 1, 1); // trinsition from the middle of the rgoad to the edge
			fixed4 c = _Color * (noise.y * 0.75 + 0.25);

			// blend the road with the terrain, by using the U coordinate as a blend factor
			float blend = IN.uv_MainTex.x;
			blend *= noise.x + 0.5; // we need to add 0.5 because noise function on average is 0.5 
			blend = smoothstep(0.4, 0.7, blend);

            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = blend;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

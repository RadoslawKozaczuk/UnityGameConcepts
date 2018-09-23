#if !defined(LOOKING_THROUGH_WATER_INCLUDED)
#define LOOKING_THROUGH_WATER_INCLUDED

// To figure out how far light has traveled underwater, we have to know how far away whatever lies below the water is. 
// Because the water is transparent, it doesn't write to the depth buffer. All opaque objects have already been rendered, 
// so the depth buffer contains the information that we need.
// Unity makes the depth buffer globally available via the _CameraDepthTexture variable
sampler2D _CameraDepthTexture, _WaterBackground;
float4 _CameraDepthTexture_TexelSize;

float3 _WaterFogColor;
float _WaterFogDensity;

float _RefractionStrength;

float2 AlignWithGrabTexel (float2 uv) {
	#if UNITY_UV_STARTS_AT_TOP
		if (_CameraDepthTexture_TexelSize.y < 0) {
			uv.y = 1 - uv.y;
		}
	#endif

	return
		(floor(uv * _CameraDepthTexture_TexelSize.zw) + 0.5) *
		abs(_CameraDepthTexture_TexelSize.xy);
}

float3 ColorBelowWater (float4 screenPos, float3 tangentSpaceNormal) {
	float2 uvOffset = tangentSpaceNormal.xy * _RefractionStrength;
	uvOffset.y *= _CameraDepthTexture_TexelSize.z * abs(_CameraDepthTexture_TexelSize.y);
	float2 uv = AlignWithGrabTexel((screenPos.xy + uvOffset) / screenPos.w);
	
	// Now we can sample the background depth via the SAMPLE_DEPTH_TEXTURE macro, 
	// and then convert the raw value to the linear depth via the LinearEyeDepth function.
	float backgroundDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
	// This is the depth relative to the screen, not the water surface. 
	// So we need to know the distance between the water and the screen as well
	float surfaceDepth = UNITY_Z_0_FAR_FROM_CLIPSPACE(screenPos.z);
	// The underwater depth is found by subtracting the surface depth from the background depth.
	float depthDifference = backgroundDepth - surfaceDepth;
	
	uvOffset *= saturate(depthDifference);
	uv = AlignWithGrabTexel((screenPos.xy + uvOffset) / screenPos.w);
	backgroundDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
	depthDifference = backgroundDepth - surfaceDepth;
	
	float3 backgroundColor = tex2D(_WaterBackground, uv).rgb;
	float fogFactor = exp2(-_WaterFogDensity * depthDifference);
	return lerp(_WaterFogColor, backgroundColor, fogFactor);
}

#endif
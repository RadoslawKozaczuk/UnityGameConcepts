#if !defined(FLOW_INCLUDED)
#define FLOW_INCLUDED

float3 FlowUVW(float2 uv, float2 flowVector, float2 jump, float flowOffset, float tiling, float time, bool flowB) {
	float phaseOffset = flowB ? 0.5 : 0;
	float progress = frac(time + phaseOffset);
	float3 uvw;
	uvw.xy = uv - flowVector * (progress + flowOffset);
	uvw.xy *= tiling;
	uvw.xy += phaseOffset;
	uvw.xy += (time - progress) * jump;
	uvw.z = 1 - abs(1 - 2 * progress);
	return uvw;
}

float2 DirectionalFlowUV(float2 uv, float3 flowVectorAndSpeed, float tiling, float time, out float2x2 rotation) {
	// Because our flow map doesn't contain vectors of unit length, we have to normalize them first.
	float2 dir = normalize(flowVectorAndSpeed.xy);
	rotation = float2x2(dir.y, dir.x, -dir.x, dir.y);

	// Then construct the matrix using that direction vector and multiply that matrix with the original UV coordinates.
	uv = mul(float2x2(dir.y, -dir.x, dir.x, dir.y), uv);

	// We begin by simply scrolling up, moving the pattern in the positive V direction, 
	// by subtracting the time from the V coordinate. 
	uv.y -= time * flowVectorAndSpeed.z;
	return uv * tiling; // Then apply the tiling.
}

#endif
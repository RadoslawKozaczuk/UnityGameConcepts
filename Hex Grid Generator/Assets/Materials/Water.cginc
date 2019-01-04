#if !defined(WATER_INCLUDED)
#define WATER_INCLUDED

float Foam(float shore, float2 worldXZ, sampler2D noiseTex) 
{
	// Let's make the foam front grow bigger as it approaches the shore.
	// This can be done by taking the square root of the shore value before using it.
	shore = sqrt(shore) * 0.9;
	float2 noiseUV = worldXZ + _Time.y * 0.25;
	float4 noise = tex2D(noiseTex, noiseUV * 0.015);
	float distortion1 = noise.x * (1 - shore);
	float foam1 = sin((shore + distortion1) * 10 - _Time.y);
	foam1 *= foam1;
	float distortion2 = noise.y * (1 - shore);
	float foam2 = sin((shore + distortion2) * 10 + _Time.y + 2);
	foam2 *= foam2 * 0.7;
	return max(foam1, foam2) * shore;
} 

float Waves(float2 worldXZ, sampler2D noiseTex) 
{ 
	float2 uv1 = worldXZ;
	uv1.y += _Time.y;
	float4 noise1 = tex2D(noiseTex, uv1 * 0.025);

	float2 uv2 = worldXZ;
	uv2.x += _Time.y; // this time we modify the U channel for further differenciation
	float4 noise2 = tex2D(noiseTex, uv2 * 0.025);

	// We produce a blend wave by creating a sine wave that runs diagonally across the water surface. 
	// We do that by adding the X and Z world coordinates together and using that as input of the sin function. 
	// Scale them down so that we get reasonably large bands.
	float blendWave = sin((worldXZ.x + worldXZ.y) * 0.1 
		+ (noise1. y + noise2.z) + _Time.y); // we add noise to make the wave less obvious
	blendWave *= blendWave; // we square the wave to convert the output from <-1, 1> to <0, 1>

	// Summing both samples can produces results in the 0–2 range, so we have to scale that back to 0–1. 
	// Instead of just halving the waves, we can use the smoothstep function to create a more interesting result. 
	// We'll map ¾–2 to 0–1, so part of the water surface ends up without visible waves.
	float waves = lerp(noise1.z, noise1.w, blendWave) + lerp(noise2.x, noise2.y, blendWave);
	return smoothstep(0.75, 2, waves);
}

float River(float2 riverUV, sampler2D noiseTex) 
{
	float2 uv = riverUV;
	// As we're only using a small strip of the noise, we could vary the pattern by sliding the strip across the texture. 
	// This is done by adding time to the U coordinate. We have to make sure to change it slowly, otherwise the river will appear to ﬂow sideways.
	uv.x = uv.x * 0.0625 + _Time.y * 0.005;
	uv.y -= _Time.y * 0.25;
	float4 noise = tex2D(noiseTex, uv);

	float2 uv2 = riverUV;
	uv2.x = uv2.x * 0.0625 - _Time.y * 0.0052;
	uv2.y -= _Time.y * 0.23;
	float4 noise2 = tex2D(noiseTex, uv2);

	return noise.x * noise2.w;
}
#endif
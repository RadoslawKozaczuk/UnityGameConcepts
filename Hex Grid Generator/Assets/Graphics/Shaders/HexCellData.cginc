sampler2D _HexCellData;
float4 _HexCellData_TexelSize;

float4 GetCellData(appdata_full v, int index) 
{
	float2 uv;

	// The first step of constructing the U coordinate is to divide the cell index by the texture width.
	// Because we're sampling a texture, we want to use UV coordinates that align with the centers of pixels. 
	// That ensures that we sample the correct pixels. So we add 0.5 before dividing by the texture sizes.
	uv.x = (v.texcoord2[index] + 0.5) * _HexCellData_TexelSize.x;

	// We can extract the row by flooring the number, then subtract that from the number to get the U coordinate.
	float row = floor(uv.x);
	uv.x -= row;

	// The V coordinate is found by dividing the row by the texture height.
	uv.y = (row + 0.5) * _HexCellData_TexelSize.y;

	// Now that we have the desired cell data coordinates, we can sample _HexCellData. 
	// Because we're sampling the texture in the vertex program, we have to explicitly tell the shader which mipmap to use. 
	// This is done via the tex2Dlod function, which requires four texture coordinates. 
	// Because the cell data doesn't have mipmaps, set the extra coordinates to zero.
	float4 data = tex2Dlod(_HexCellData, float4(uv, 0, 0));

	// The fourth data component contains the terrain type index, which we directly stored as a byte. 
	// However, the GPU automatically converted it into a floating-point value in the 0–1 range. 
	// To convert it back to its proper value, multiply it with 255.
	data.w *= 255;
	return data;
}

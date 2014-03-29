
//////////////////////////////////////////////////////////////

//パラメータ
matrix Transform;		//視点変換行列
texture HandTexture : TEXTURE0;
sampler2D HandSampler = sampler_state
{
    Texture = (HandTexture);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;   
    AddressU  = Clamp;
    AddressV  = Clamp;
};

//////////////////////////////////////////////////////////////

struct VertexShaderInput
{
    float3 Position : POSITION;
	float2 Texture : TEXCOORD;
};

struct VertexShaderOutput
{
    float4 Position : POSITION;
	float2 Texture : TEXCOORD;
};

//////////////////////////////////////////////////////////////

VertexShaderOutput VertexShaderFunction(const VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = mul(float4(input.Position, 1.0), Transform);
	output.Texture = input.Texture;
	output.Position.z = 0;

    return output;
}

float4 PixelShaderFunction(const VertexShaderOutput input) : COLOR
{
	return saturate(tex2D(HandSampler,input.Texture));
}

//////////////////////////////////////////////////////////////

technique Mesh
{
	pass p0
	{
		VertexShader = compile vs_2_0 VertexShaderFunction();
		PixelShader = compile ps_2_0 PixelShaderFunction();
	}
}
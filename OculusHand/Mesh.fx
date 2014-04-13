
//////////////////////////////////////////////////////////////

//パラメータ
matrix Transform;		//視点変換行列

float2 TopLeftAngle;		//
float2 BottomRightAngle;	//背景に表示する画像の緯度経度範囲

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

texture BackgroundImage : TEXTURE1;
sampler2D BackgroundSampler = sampler_state
{
    Texture = (BackgroundImage);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;   
	AddressU  = Wrap;
	AddressV  = Wrap;
};

//////////////////////////////////////////////////////////////

struct VertexShaderInput
{
    float3 Position : POSITION0;
	float2 Texture : NORMAL;
};

struct VertexShaderOutput
{
    float4 Position : POSITION;
	float2 Texture : TEXCOORD0;
};

struct PixelShaderInput
{
	float2 Texture : TEXCOORD0;
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

float4 PixelShaderFunction(const PixelShaderInput input) : COLOR0
{
	return saturate(tex2D(HandSampler, input.Texture));
}

float4 PixelShaderAlwaysBlack(const PixelShaderInput input) : COLOR0
{
	return float4(0, 0, 0, 1);
}

//////////////////////////////////////////////////////////////

VertexShaderOutput VertexShaderBackground(const VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = float4(input.Position, 1.0);
	output.Texture = input.Texture;

	return output;
}

float4 PixelShaderBackground(const PixelShaderInput input) : COLOR0
{
	return saturate(tex2D(BackgroundSampler, input.Texture));
}

//////////////////////////////////////////////////////////////

technique Mesh
{
	pass p0
	{
		VertexShader = compile vs_2_0 VertexShaderFunction();
		PixelShader = compile ps_2_0 PixelShaderFunction();
	}

	pass p1
	{
		VertexShader = compile vs_2_0 VertexShaderFunction();
		PixelShader = compile ps_2_0 PixelShaderAlwaysBlack();
	}

	pass p2
	{
		VertexShader = compile vs_2_0 VertexShaderBackground();
		PixelShader = compile ps_2_0 PixelShaderBackground();
	}

	pass p3
	{
		VertexShader = compile vs_2_0 VertexShaderBackground();
		PixelShader = compile ps_2_0 PixelShaderAlwaysBlack();
	}
}

//////////////////////////////////////////////////////////////

//パラメータ
matrix Transform;		//視点変換行列
matrix OculusOrientation;	//Oculusの視線方向回転行列

float ThetaMappingDepth;
float4 DistortionParameter;
float LensHorizontalDistanceRatioFromCenter;

float OffsetU;

static const float PI = 3.14159265358979323846264;

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

texture Distortion : TEXTURE2;
sampler2D DistortionSampler = sampler_state
{
	Texture = (Distortion);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;   
	AddressU  = Clamp;
	AddressV  = Clamp;
};

texture Offset : TEXTURE3;
sampler2D OffsetSampler = sampler_state
{
	Texture = (Offset);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;   
	AddressU  = Clamp;
	AddressV  = Clamp;
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

float4 PixelShaderTexcoord(const float2 uv : TEXCOORD) : COLOR0
{
	return float4(uv.x, uv.y, 0, 1);
}

//////////////////////////////////////////////////////////////

VertexShaderOutput VertexShaderBackground(const VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = float4(input.Position, 1.0);
	
	//UVマップを計算、正距円筒方式の展開
	float4 pos = output.Position;
	pos.z += ThetaMappingDepth;
	pos = mul(pos, OculusOrientation);
	float theta_u = atan2(pos.x, pos.z) / (2 * PI) + 0.5;
	float theta_v = -atan2(pos.y, length(float2(pos.x, pos.z))) / PI + 0.5;

	output.Texture = float2(theta_u, theta_v);

	return output;
}

float4 PixelShaderBackground(const PixelShaderInput input) : COLOR0
{
	return saturate(tex2D(BackgroundSampler, input.Texture));
}

//////////////////////////////////////////////////////////////

VertexShaderOutput VertexShaderDistortion(const VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = float4(input.Position, 1.0);

	//画面を左右に分ける
	float u, v;
	u = output.Position.x;
	v = saturate(-output.Position.y / 2 + 0.5);

	output.Texture = float2(u, v);

	return output;
}

float4 PixelShaderDistortion(const PixelShaderInput input) : COLOR0
{
	const float scaleIn = 1;
	const float scale = 1;

	float2 uv = input.Texture;
	float2 center = float2(0.5, 0.5);

	//[TODO]正しいパラメータを適用する
	float uvHorizontalScale = 0.5 / (1 - 0.3);//LensHorizontalDistanceRatioFromCenter);
	if (uv.x < 0)
	{
		uv.x = 0 + (1 + uv.x) * uvHorizontalScale;
		center.x = 1 - LensHorizontalDistanceRatioFromCenter;
	}
	else
	{
		uv.x = 1 - (1 - uv.x) * uvHorizontalScale;
		center.x = LensHorizontalDistanceRatioFromCenter;
	}

	//Oculus向けにBarrelDistortionを行う
	float2 centered = (uv - center) * scaleIn;
	float rsq = centered.x * centered.x + centered.y * centered.y;
	float2 warped = centered * (DistortionParameter.x + 
		                        DistortionParameter.y * rsq + 
								DistortionParameter.z * rsq * rsq + 
								DistortionParameter.w * rsq * rsq * rsq) * scale + center;

	return saturate(tex2D(DistortionSampler, warped));
}

//////////////////////////////////////////////////////////////

VertexShaderOutput VertexShaderOffset(const VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = float4(input.Position, 1.0);

	float u, v;
	u = saturate(output.Position.x / 2 + 0.5);
	v = saturate(-output.Position.y / 2 + 0.5);

	output.Texture = float2(u, v);

	return output;
}

float4 PixelShaderOffset(const PixelShaderInput input) : COLOR0
{
	float2 uv = input.Texture;
	uv.x -= OffsetU;
	return saturate(tex2D(OffsetSampler, uv));
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
		PixelShader = compile ps_2_0 PixelShaderBackground();
	}

	pass p4
	{
		VertexShader = compile vs_2_0 VertexShaderDistortion();
		PixelShader = compile ps_2_0 PixelShaderDistortion();
	}

	pass p5
	{
		VertexShader = compile vs_2_0 VertexShaderOffset();
		PixelShader = compile ps_2_0 PixelShaderOffset();
	}
}

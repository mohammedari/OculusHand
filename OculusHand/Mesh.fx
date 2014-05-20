
//////////////////////////////////////////////////////////////

//パラメータ
matrix Transform;		//視点変換行列
matrix OculusOrientation;	//Oculusの視線方向回転行列

float ThetaMappingDepth;
float4 DistortionParameter;
float LensHorizontalDistanceRatioFromCenter;

float OffsetU;

static const float PI = 3.14159265358979323846264;

Texture2D HandTexture;
Texture2D BackgroundImage;
Texture2D Distortion;
Texture2D Offset;

sampler WrapSampler
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

sampler ClampSampler
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
};

//////////////////////////////////////////////////////////////

struct VertexShaderInput
{
    float3 Position : POSITION;
	float2 Texture : TEXCOORD;
};

struct VertexShaderOutput
{
    float4 Position : SV_Position;
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

float4 PixelShaderFunction(const VertexShaderOutput input) : SV_Target
{
	return HandTexture.Sample(ClampSampler, input.Texture);
}

float4 PixelShaderAlwaysBlack(const VertexShaderOutput input) : SV_Target
{
	return float4(0, 0, 0, 1);
}

float4 PixelShaderTexcoord(const float2 uv : TEXCOORD) : SV_Target
{
	return float4(uv.x, uv.y, 0, 1);
}

//////////////////////////////////////////////////////////////

VertexShaderOutput VertexShaderBackground(const VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = float4(input.Position, 1.0);
	output.Position.z = 0;
	
	//UVマップを計算、正距円筒方式の展開
	float4 pos = output.Position;
	pos.z += ThetaMappingDepth;
	pos = mul(pos, OculusOrientation);
	float theta_u = atan2(pos.x, pos.z) / (2 * PI) + 0.5;
	float theta_v = -atan2(pos.y, length(float2(pos.x, pos.z))) / PI + 0.5;

	output.Texture = float2(theta_u, theta_v);

	return output;
}

float4 PixelShaderBackground(const VertexShaderOutput input) : SV_Target
{
	return BackgroundImage.Sample(WrapSampler, input.Texture);
}

//////////////////////////////////////////////////////////////

VertexShaderOutput VertexShaderDistortion(const VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = float4(input.Position, 1.0);
	output.Position.z = 0;

	//画面を左右に分ける
	float u, v;
	u = output.Position.x;
	v = saturate(-output.Position.y / 2 + 0.5);

	output.Texture = float2(u, v);

	return output;
}

float4 PixelShaderDistortion(const VertexShaderOutput input) : SV_Target
{
	const float scaleIn = 1;
	const float scale = 1;

	float2 uv = input.Texture;
	float2 center = float2(0.5, 0.5);

	float uvHorizontalScale = 0.5 / (1 - LensHorizontalDistanceRatioFromCenter);
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

	return Distortion.Sample(ClampSampler, warped);
}

//////////////////////////////////////////////////////////////

VertexShaderOutput VertexShaderOffset(const VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = float4(input.Position, 1.0);
	output.Position.z = 0;

	float u, v;
	u = saturate(output.Position.x / 2 + 0.5);
	v = saturate(-output.Position.y / 2 + 0.5);

	output.Texture = float2(u, v);

	return output;
}

float4 PixelShaderOffset(const VertexShaderOutput input) : SV_Target
{
	float2 uv = input.Texture;
	uv.x -= OffsetU;
	return Offset.Sample(ClampSampler, uv);
}

//////////////////////////////////////////////////////////////
technique10 Mesh
{
	pass p0
	{
		SetVertexShader(CompileShader(vs_4_0, VertexShaderFunction()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PixelShaderFunction()));
	}

	pass p1
	{
		SetVertexShader(CompileShader(vs_4_0, VertexShaderFunction()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PixelShaderAlwaysBlack()));
	}

	pass p2
	{
		SetVertexShader(CompileShader(vs_4_0, VertexShaderBackground()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PixelShaderBackground()));
	}

	pass p3
	{
		SetVertexShader(CompileShader(vs_4_0, VertexShaderBackground()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PixelShaderAlwaysBlack()));
	}

	pass p4
	{
		SetVertexShader(CompileShader(vs_4_0, VertexShaderDistortion()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PixelShaderDistortion()));
	}

	pass p5
	{
		SetVertexShader(CompileShader(vs_4_0, VertexShaderOffset()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PixelShaderOffset()));
	}
}

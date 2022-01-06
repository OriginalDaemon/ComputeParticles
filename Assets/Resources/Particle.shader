// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Particle" 
{
	Properties
	{
		baseTexture ("Texture", 2D) = "white" {}
		baseColor ("Main Color", Color) = (1,1,1,1)
	}

	SubShader
	{
		Pass
		{
			Tags
			{
				"Queue" = "Transparent" 
				"RenderType" = "Transparent"
			}
			LOD 200
			Blend OneMinusDstColor One
			BlendOp Add
            ZWrite Off

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			#define VertexShader
			#include "ParticleCommon.cginc"

			#pragma target 5.0
		
			struct PS_INPUT
			{
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD;
			};

			static const float3 Billboard[4] =
			{
				float3(-0.5f, -0.5f, 0.0f),
				float3(-0.5f,  0.5f, 0.0f),
				float3( 0.5f,  0.5f, 0.0f),
				float3( 0.5f, -0.5f, 0.0f),
			};

			static const float2 UVs[4] =
			{
				float2(1.0f, 1.0f),
				float2(0.0f, 1.0f),
				float2(0.0f, 0.0f),
				float2(1.0f, 0.0f),
			};

			PS_INPUT vert(uint vertexID : SV_VertexID)
			{
				PS_INPUT output = (PS_INPUT)0;

				uint particleID = vertexID >> 2;
				uint cornerID = vertexID % 4;
				Particle p = particleBuffer[particleID];
				
				float4 viewSpacePos = mul(UNITY_MATRIX_V, float4(p.position, 1.0f));
				float4 quadCornerPos = float4(viewSpacePos.xyz + Billboard[cornerID] * p.size * 1.0f, viewSpacePos.w);

				output.position = mul(UNITY_MATRIX_P, quadCornerPos);
				output.uv = UVs[cornerID];

				return output;
			}

			Texture2D baseTexture : register(t0);
			SamplerState samplerbaseTexture : register(s0);
            fixed4 baseColor;

			float4 frag(PS_INPUT input) : SV_TARGET
			{
				return baseTexture.Sample(samplerbaseTexture, input.uv) * baseColor;
			}

			ENDCG
		}
	}
	FallBack Off
}

#include "Constants.cginc"

float4 randomSeed;

float rand3dTo1d(float3 value, float3 dotDir = float3(12.9898, 78.233, 37.719))
{
	float3 smallValue = sin(value);
	float random = dot(smallValue, cross(dotDir, randomSeed.xyz));
	random = frac(sin(random) * 143758.5453);
	return random * 2.0 - 1.0;
}

float3 rand3dTo3d(float3 value)
{
	return float3(
		rand3dTo1d(value, float3(12.989, 78.233, 37.719)),
		rand3dTo1d(value, float3(39.346, 11.135, 83.155)),
		rand3dTo1d(value, float3(73.156, 52.235, 09.151))
	);
}

float3 randSphere(float3 value) 
{
	float theta = rand3dTo1d(value, float3(12.989, 78.233, 37.719)) * 2.0 * PI;
	float phi = acos(2.0 * rand3dTo1d(value, float3(39.346, 11.135, 83.155)) - 1.0);
	float r = pow(abs(rand3dTo1d(value,float3(73.156, 52.235, 09.151))), 1.0 / 3.0);
	return float3(
		r * sin(phi) * cos(theta),
		r * sin(phi) * sin(theta),
		r * cos(phi)
	);
}

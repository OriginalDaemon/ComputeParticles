struct Particle
{
    float3 position;
    float3 velocity;
    float life;
    float size;
};

#ifdef VertexShader
	// vertex shaders can only use RWStructuredBuffer on hardware which 
	// supports D3D_11_1 and above.
	StructuredBuffer<Particle> particleBuffer;
#else
	RWStructuredBuffer<Particle> particleBuffer;
#endif

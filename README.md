# ComputeParticles

An example of particles in Unity powered by compute shaders, thus making them run entirely GPU side. This was intended for use in a write-up.

## Known Issues

Particles can only be rendered in editor mode due to the use of additive blending.
Only additive blending can be used as there is no sorting.
There is something off with the vertex shader, resulting in broken quads.
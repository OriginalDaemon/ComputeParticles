using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ComputeParticles))]
public class ComputeParticlesEditor : Editor
{
    public void OnSceneGUI()
    {
        var t = target as ComputeParticles;
        var pos = t.particleEmissionProperties.origin;
        Handles.color = Color.red;
        Handles.RadiusHandle(Quaternion.identity, pos, t.particleEmissionProperties.radius);
    }
}
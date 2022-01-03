#pragma warning disable 0649

using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;
using UnityEngine;

public static class Vector3Extensions
{
    public static float[] ToFloatBuffer(this Vector3 vector)
    {
        return new float[3] { vector.x, vector.y, vector.z };
    }
}

[ExecuteAlways]
public class ComputeParticles : MonoBehaviour
{
    [System.Serializable]
    struct ParticleDef
    {
        public Vector3 position;
        public Vector3 velocity;
        public float life;
        public float size;
    }

    private const int _xThreadCount = 256;

    [System.Serializable]
    public struct ParticleEmissionProperties
    {
        public float radius;
        [HideInInspector]
        public Vector3 origin;
        public float lifetimeMin;
        public float lifetimeMax;
        public float sizeMin;
        public float sizeMax;
        public Vector3 velocityDirection;
        public float velocityMin;
        public float velocityMax;
        [HideInInspector]
        public int particleCount;
        public float rate;
    }
    public ParticleEmissionProperties particleEmissionProperties = new ParticleEmissionProperties();
    private ComputeShader _emissionShader;
    private const string _emissionShaderName = "ParticleEmission";
    private const string _emissionKernalName = "particleEmission";
    private int _emissionShaderKernelID;

    [System.Serializable]
    public struct ParticleSimulationProperties
    {
        public Vector3 acceleration;
        public float fadeFactor;
        [HideInInspector]
        public float deltaTime;
    }
    public ParticleSimulationProperties particleSimulationProperties = new ParticleSimulationProperties();
    private ComputeShader _simulationShader;
    private const string _simulationShaderName = "ParticleSimulation";
    private const string _simulationKernalName = "particleSimulation";
    private int _simulationShaderKernelID;

    public Material material;
    MaterialPropertyBlock _materialProperties;
    private Mesh _mesh = null;

    ComputeBuffer _particles;

    private int _batchSize = 0;
    [Range(1.0f / 60.0f, 1.0f / 10.0f)]
    public float simulationDeltaTime = 1.0f / 20.0f;
    [Range(0.0f, 99.0f)]
    public float warmupTime = 1.0f;
    private float _age = 0;
    private float _targetAge = 0;

    public Color color = new Color(1.0f, 1.0f, 1.0f);

    int maxParticleCount
    {
        get
        {
            return Mathf.FloorToInt(particleEmissionProperties.lifetimeMax * particleEmissionProperties.rate);
        }
    }

    private void OnValidate()
    {
        Initialize();
        Debug.Assert(_emissionShader != null, "The emission compute shader must be set.");
        Debug.Assert(_simulationShader != null, "The simulation compute shader must be set.");
        Debug.Assert(material != null, "A material to render the particles must be supplied.");
    }

    private void TearDown()
    {
        if (_particles != null)
        {
            _particles.Release();
            _particles = null;
        }
    }

    private void InitializeVariables()
    {
        _age = 0;
        _targetAge = warmupTime;
    }

    private void InitializeMesh()
    {
        _mesh = new Mesh();
        _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        var vertexCount = maxParticleCount * 4;
        var indicesCount = maxParticleCount * 6;

        Vector3[] v = new Vector3[vertexCount];
        _mesh.vertices = v;

        int[] tris = new int[indicesCount];
        // Clockwise winding
        for (int i = 0; i < maxParticleCount; ++i)
        {
            tris[i * 6 + 0] = i * 4 + 0;
            tris[i * 6 + 1] = i * 4 + 1;
            tris[i * 6 + 2] = i * 4 + 2;

            tris[i * 6 + 3] = i * 4 + 2;
            tris[i * 6 + 4] = i * 4 + 3;
            tris[i * 6 + 5] = i * 4 + 0;
        };
        _mesh.triangles = tris;
    }

    void InitializeShaders()
    {
        _particles = new ComputeBuffer(maxParticleCount, Marshal.SizeOf(new ParticleDef()));

        _emissionShader = Instantiate(Resources.Load<ComputeShader>(_emissionShaderName));
        _emissionShaderKernelID = _emissionShader.FindKernel(_emissionKernalName);
        _simulationShader = Instantiate(Resources.Load<ComputeShader>(_simulationShaderName));
        _simulationShaderKernelID = _simulationShader.FindKernel(_simulationKernalName);

        _emissionShader.SetBuffer(_emissionShaderKernelID, "particleBuffer", _particles);
        _simulationShader.SetBuffer(_simulationShaderKernelID, "particleBuffer", _particles);

        _materialProperties = new MaterialPropertyBlock();
        _materialProperties.SetBuffer("particleBuffer", _particles);

        _batchSize = (int)Mathf.Ceil(((float)maxParticleCount) / ((float)_xThreadCount));
    }

    private void Initialize()
    {
        TearDown();
        InitializeVariables();
        InitializeMesh();
        InitializeShaders();
        Update();
    }

    private void OnEnable()
    {
        Initialize();
        Camera.onPreCull -= Draw;
        Camera.onPreCull += Draw;
    }

    private void OnDisable()
    {
        TearDown();
        Camera.onPreCull -= Draw;
    }

    void OnDestroy()
    {
        TearDown();
    }

    void _UpdateFields(float deltaTime)
    {
        _age += deltaTime;
        particleEmissionProperties.origin = transform.position;
        particleSimulationProperties.deltaTime = deltaTime;
    }

    private Vector4 GenerateRandomSeed()
    {
        return new Vector4(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(1000.0f, 9999999.0f));
    }

    void _RunEmission()
    {
        _emissionShader.SetFloat("radius", particleEmissionProperties.radius);
        _emissionShader.SetFloats("origin", particleEmissionProperties.origin.ToFloatBuffer());
        _emissionShader.SetFloat("lifetimeMin", particleEmissionProperties.lifetimeMin);
        _emissionShader.SetFloat("lifetimeMax", particleEmissionProperties.lifetimeMax);
        _emissionShader.SetFloat("sizeMin", particleEmissionProperties.sizeMin);
        _emissionShader.SetFloat("sizeMax", particleEmissionProperties.sizeMax);
        _emissionShader.SetFloats("velocityDirection", transform.TransformDirection(particleEmissionProperties.velocityDirection).ToFloatBuffer());
        _emissionShader.SetFloat("velocityMin", particleEmissionProperties.velocityMin);
        _emissionShader.SetFloat("velocityMax", particleEmissionProperties.velocityMax);
        _emissionShader.SetInt("particleCount", (int)Mathf.Min(maxParticleCount, Mathf.Floor(_age * particleEmissionProperties.rate)));
        _emissionShader.SetVector("randomSeed", GenerateRandomSeed());

        _emissionShader.Dispatch(_emissionShaderKernelID, _batchSize, 1, 1);
    }

    void _RunSimulation()
    {
        _simulationShader.SetFloats("acceleration", particleSimulationProperties.acceleration.ToFloatBuffer());
        _simulationShader.SetFloat("fadeFactor", particleSimulationProperties.fadeFactor);
        _simulationShader.SetFloat("deltaTime", particleSimulationProperties.deltaTime);
        _simulationShader.SetVector("randomSeed", GenerateRandomSeed());

        _simulationShader.Dispatch(_simulationShaderKernelID, _batchSize, 1, 1);
    }

    void UpdateIteration(float deltaTime)
    {
        _UpdateFields(deltaTime);
        _RunEmission();
        _RunSimulation();
    }

    void Update()
    {
        _targetAge += Time.deltaTime;
        while (_age + simulationDeltaTime < _targetAge)
        {
            UpdateIteration(simulationDeltaTime);
        }
    }

    private void Draw(Camera camera)
    {
        if (camera)
        {
            _materialProperties.SetColor("baseColor", color);
            Graphics.DrawMesh(_mesh, Matrix4x4.identity, material, 0, camera, 0, _materialProperties);
        }
    }
}

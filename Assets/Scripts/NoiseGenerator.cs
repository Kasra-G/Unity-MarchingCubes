using System;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;

[Serializable]
public struct NoiseParams
{
    [Range(0, 1)]
    public int enabled;
    public int is2D;
    [Range(0, 1)]
    public float mask;
    public float amplitude;
    public Vector3 frequency;
    public Vector3 offset;
    public static int SizeOf => sizeof(float) * 3 * 2 + sizeof(float) * 2 + sizeof(int) * 2;
};
public class NoiseGenerator
{
    private ComputeShader shader;

    private NoiseParams[] noiseParameters;
    private Vector3 dimensions;
    private Vector3 offset;
    private float seed;

    private ComputeBuffer _weightsBuffer;
    private ComputeBuffer _noiseLayersBuffer;

    public NoiseGenerator(Vector3 dimensions, Vector3 offset, NoiseParams[] noiseParameters, float seed)
    {
        this.dimensions = dimensions;
        this.shader = (ComputeShader)Resources.Load("Shaders/PointGenerator");
        this.offset = offset;
        this.seed = seed;
        this.noiseParameters = noiseParameters;
        CreateBuffers();
    }

    ~NoiseGenerator()
    {
        ReleaseBuffers();
    }

    void CreateBuffers()
    {
        _weightsBuffer = new ComputeBuffer((int)(dimensions.x * dimensions.y * dimensions.z), sizeof(float));
        _noiseLayersBuffer = new ComputeBuffer(noiseParameters.Length, NoiseParams.SizeOf);
    }

    public void ReleaseBuffers()
    {
        _weightsBuffer.Release();
        _noiseLayersBuffer.Release();
    }

    public float[] GenerateSimplexScalarField()
    {
        Vector3[] points = new Vector3[(int)(dimensions.x * dimensions.y * dimensions.z)];
        int i = 0;
        for (int zIndex = 0; zIndex < dimensions.z; zIndex++)
        {
            for (int yIndex = 0; yIndex < dimensions.y; yIndex++)
            {
                for (int xIndex = 0; xIndex < dimensions.x; xIndex++)
                {
                    Vector3 coord = new Vector3(xIndex, yIndex, zIndex) + offset;
                    points[i] = coord;
                    i += 1;
                }
            }
        }
        int kernel = shader.FindKernel("GeneratePoints");

        shader.SetBuffer(kernel, "_Weights", _weightsBuffer);
        shader.SetBuffer(kernel, "_NoiseLayers", _noiseLayersBuffer);

        shader.SetInt("_NoiseLayersCount", noiseParameters.Length);
        shader.SetVector("offset", offset);
        shader.SetFloat("seed", this.seed);
        shader.SetVector("dimensions", dimensions);

        _noiseLayersBuffer.SetData(noiseParameters);

        int workgroupsX = Mathf.CeilToInt(dimensions.x / 8.0f);
        int workgroupsY = Mathf.CeilToInt(dimensions.y / 8.0f);
        int workgroupsZ = Mathf.CeilToInt(dimensions.z / 8.0f);
        shader.Dispatch(kernel, workgroupsX, workgroupsY, workgroupsZ);
        float[] weights = new float[(int)(dimensions.x * dimensions.y * dimensions.z)];

        _weightsBuffer.GetData(weights);
        return weights;
    }
}


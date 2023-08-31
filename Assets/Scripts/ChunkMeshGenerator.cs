using System;
using UnityEngine;
using System.Linq;

public struct Triangle
{
    public Vector3 a, b, c;
    public static int SizeOf => sizeof(float) * 3 * 3;
}
public class ChunkMeshGenerator
{
    private ComputeShader shader;
    private Vector3 dimensions;
    private float isoLevel = 0;

    public float[] _weights { get; private set; }

    ComputeBuffer _trianglesBuffer;
    ComputeBuffer _trianglesCountBuffer;
    ComputeBuffer _weightsBuffer;

    public ChunkMeshGenerator(Vector3 dimensions, float[] weights)
    {
        this.dimensions = dimensions;
        this._weights = weights;    // _weights.length = dimensions.x * dimensions.y * dimensions.z;
        this.shader = (ComputeShader)Resources.Load("Shaders/MarchingCubes");
        CreateBuffers();
    }

    ~ChunkMeshGenerator()
    {
        ReleaseBuffers();
    }

    private void CreateBuffers()
    {
        _trianglesBuffer = new ComputeBuffer(5 * _weights.Length, Triangle.SizeOf, ComputeBufferType.Append);
        _trianglesCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        _weightsBuffer = new ComputeBuffer(_weights.Length, sizeof(float));
    }

    public void ReleaseBuffers()
    {
        _trianglesBuffer.Release();
        _trianglesCountBuffer.Release();
        _weightsBuffer.Release();
    }

    public Mesh GenerateMesh()
    {
        int kernel = shader.FindKernel("CSMain");
        shader.SetBuffer(kernel, "_Triangles", _trianglesBuffer);
        shader.SetBuffer(kernel, "_Weights", _weightsBuffer);
        shader.SetFloat("_IsoLevel", isoLevel);
        shader.SetVector("dimensions", dimensions);
        _weightsBuffer.SetData(_weights);
        _trianglesBuffer.SetCounterValue(0);

        shader.Dispatch(kernel, Mathf.CeilToInt(dimensions.x / 8.0f), Mathf.CeilToInt(dimensions.y / 8.0f), Mathf.CeilToInt(dimensions.z / 8.0f));

        Triangle[] triangles = new Triangle[ReadTriangleCount()];
        _trianglesBuffer.GetData(triangles);

        return CreateMeshFromTriangles(triangles);
    }

    Mesh CreateMeshFromTriangles(Triangle[] triangles)
    {
        Vector3[] verts = new Vector3[triangles.Length * 3];
        int[] tris = new int[triangles.Length * 3];

        for (int i = 0; i < triangles.Length; i++)
        {
            int startIndex = i * 3;

            verts[startIndex] = triangles[i].a;
            verts[startIndex + 1] = triangles[i].b;
            verts[startIndex + 2] = triangles[i].c;

            tris[startIndex] = startIndex;
            tris[startIndex + 1] = startIndex + 1;
            tris[startIndex + 2] = startIndex + 2;
        }

        Mesh mesh = new Mesh()
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt16,
            vertices = verts,
            triangles = tris,
        };
        mesh.RecalculateNormals();
        return mesh;
    }

    int ReadTriangleCount()
    {
        int[] triCount = { 0 };
        ComputeBuffer.CopyCount(_trianglesBuffer, _trianglesCountBuffer, 0);
        _trianglesCountBuffer.GetData(triCount);
        return triCount[0];
    }
}


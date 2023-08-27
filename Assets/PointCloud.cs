using System;
using UnityEngine;
using System.Linq;

[Serializable]
public struct NoiseParams
{
    public int octaves;
    public float amplitude;
    public Vector3 offset;
    public float frequency;
    public float persistence;
    public float lacunarity;
};
public class PointCloud : MonoBehaviour
{
    public ComputeShader shader;
    public ComputeShader marchingShader;
    public NoiseParams noiseParameters;
    public Vector3Int dimensions = new Vector3Int(1, 1, 1);
    public float radius = 1f;

    [Range(0, 1)]
    public float isoLevel;
    private Vector4[,,] points;
    private Vector4[] flattenedPoints;
    private float[] _weights;

    ComputeBuffer pointsbuffer;
    ComputeBuffer _trianglesBuffer;
    ComputeBuffer _trianglesCountBuffer;
    ComputeBuffer _weightsBuffer;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    private void OnDestroy()
    {
        ReleaseBuffers();
    }


    void CreateBuffers()
    {
        _trianglesBuffer = new ComputeBuffer(5 * (dimensions.x * dimensions.y * dimensions.z), Triangle.SizeOf, ComputeBufferType.Append);
        _trianglesCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        _weightsBuffer = new ComputeBuffer(dimensions.x * dimensions.y * dimensions.z, sizeof(float));
        pointsbuffer = new ComputeBuffer(dimensions.x * dimensions.y * dimensions.z, sizeof(float) * 4);
    }

    void ReleaseBuffers()
    {
        _trianglesBuffer?.Release();
        _trianglesCountBuffer?.Release();
        _weightsBuffer?.Release();
        pointsbuffer?.Release();
    }

    public struct Triangle
    {
        public Vector3 a, b, c;
        public static int SizeOf => sizeof(float) * 3 * 3;
    }

    private Mesh CreateMesh()
    {
        int kernel = marchingShader.FindKernel("CSMain");
        marchingShader.SetBuffer(kernel, "_Triangles", _trianglesBuffer);
        marchingShader.SetBuffer(kernel, "_Weights", _weightsBuffer);
        marchingShader.SetBuffer(kernel, "_Points", pointsbuffer);
        marchingShader.SetFloat("_IsoLevel", this.isoLevel);
        marchingShader.SetVector("dimensions", new Vector3(dimensions.x, dimensions.y, dimensions.z));
        _weightsBuffer.SetData(_weights);
        _trianglesBuffer.SetCounterValue(0);
        pointsbuffer.SetData(flattenedPoints);
        
        marchingShader.Dispatch(kernel, Mathf.CeilToInt(dimensions.x / 8.0f), Mathf.CeilToInt(dimensions.y / 8.0f), Mathf.CeilToInt(dimensions.z / 8.0f));

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

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        return mesh;
    }

    public T[,,] Unflatten<T>(T[] arr, int xMax, int yMax, int zMax)
    {
        T[,,] result = new T[xMax, yMax, zMax];
        int i = 0;
        for (int z = 0; z < zMax; z++)
        {
            for (int y = 0; y < yMax; y++)
            {
                for (int x = 0; x < xMax; x++)
                {
                    result[x, y, z] = arr[i];
                    i += 1;
                }
            }
        }
        return result;
    }

    public T[] Flatten<T>(T[,,] arr, int xMax, int yMax, int zMax)
    {
        T[] result = new T[arr.Length];
        int i = 0;
        for (int z = 0; z < zMax; z++)
        {
            for (int y = 0; y < yMax; y++)
            {
                for (int x = 0; x < xMax; x++)
                {
                    result[i] = arr[x, y, z];
                    i += 1;
                }
            }
        }
        return result;
    }

    int ReadTriangleCount()
    {
        int[] triCount = { 0 };
        ComputeBuffer.CopyCount(_trianglesBuffer, _trianglesCountBuffer, 0);
        _trianglesCountBuffer.GetData(triCount);
        return triCount[0];
    }
    private void Setup()
    {
        meshFilter = gameObject.GetComponent<MeshFilter>();
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        points = new Vector4[dimensions.x, dimensions.y, dimensions.z];
        flattenedPoints = new Vector4[dimensions.x * dimensions.y * dimensions.z];
        _weights = new float[dimensions.x * dimensions.y * dimensions.z];

        Vector4[,,] temp = new Vector4[dimensions.x, dimensions.y, dimensions.z];
        for (int x = 0; x < dimensions.x; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int z = 0; z < dimensions.z; z++)
                {
                    temp[x, y, z] = new Vector4(x, y, z);
                }
            }
        }
        flattenedPoints = Flatten(temp, dimensions.x, dimensions.y, dimensions.z);
        flattenedPoints = GeneratePoints();
        _weights = flattenedPoints.Select(p => p.w).ToArray();
        points = Unflatten(flattenedPoints, dimensions.x, dimensions.y, dimensions.z);
        meshFilter.sharedMesh = CreateMesh();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        ReleaseBuffers();
        CreateBuffers();
        Setup();
    }

    private void Start()
    {
        ReleaseBuffers();
        CreateBuffers();
        Setup();
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        for (int x = 0; x < dimensions.x; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int z = 0; z < dimensions.z; z++)
                {
                    Vector4 point = points[x, y, z];
                    float weight = point.w;
                    Gizmos.color = Color.Lerp(Color.black, Color.white, weight);
                    Gizmos.DrawCube(point, Vector3.one * radius);
                }
            }
        }
    }

    private Vector4[] GeneratePoints()
    {
        int kernel = shader.FindKernel("GeneratePoints");

        shader.SetBuffer(kernel, "points", pointsbuffer);
        pointsbuffer.SetData(flattenedPoints);

        shader.SetInt("octaves", this.noiseParameters.octaves);
        shader.SetFloat("amplitude", this.noiseParameters.amplitude);
        shader.SetVector("offset", this.noiseParameters.offset);
        shader.SetFloat("frequency", this.noiseParameters.frequency);
        shader.SetFloat("persistence", this.noiseParameters.persistence);
        shader.SetFloat("lacunarity", this.noiseParameters.lacunarity);
        shader.SetVector("dimensions", new Vector3(dimensions.x, dimensions.y, dimensions.z));

        int workgroupsX = Mathf.CeilToInt(dimensions.x / 8.0f);
        int workgroupsY = Mathf.CeilToInt(dimensions.y / 8.0f);
        int workgroupsZ = Mathf.CeilToInt(dimensions.z / 8.0f);
        shader.Dispatch(kernel, workgroupsX, workgroupsY, workgroupsZ);
        Vector4[] data = new Vector4[dimensions.x * dimensions.y * dimensions.z];
        pointsbuffer.GetData(data);
        return data;
    }
}


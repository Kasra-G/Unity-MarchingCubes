using System;
using UnityEngine;
using System.Linq;
using UnityEngine.Profiling;

public class Chunk : MonoBehaviour
{
    public Mesh mesh { private set; get; }

    private NoiseParams[] noiseParameters;
    public bool isGenerated;
    public Vector3 dimensions;
    public Vector3 offset;
    [HideInInspector]
    public float[] weights;
    public float seed;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    public void Generate()
    {
        NoiseGenerator noiseGenerator = new NoiseGenerator(dimensions, offset, noiseParameters, this.seed);
        weights = noiseGenerator.GenerateSimplexScalarField();

        ChunkMeshGenerator chunkMeshGenerator = new ChunkMeshGenerator(dimensions, weights);
        mesh = chunkMeshGenerator.GenerateMesh();
        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
        chunkMeshGenerator.ReleaseBuffers();
        noiseGenerator.ReleaseBuffers();
        isGenerated = true;
    }


    public void Init(Vector3 dimensions, Vector3 offset, NoiseParams[] noiseParameters, float seed)
    {
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshFilter = gameObject.GetComponent<MeshFilter>();
        meshCollider = gameObject.GetComponent<MeshCollider>();
        this.dimensions = dimensions;
        this.offset = offset;
        this.noiseParameters = noiseParameters;
        this.seed = seed;
        this.isGenerated = false;
    }

    public void DestroyOrDisable()
    {
        if (Application.isPlaying)
        {
            mesh.Clear();
            gameObject.SetActive(false);
        }
        else
        {
            DestroyImmediate(gameObject, false);
        }
    }
}


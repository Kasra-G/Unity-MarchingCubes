using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ChunkGenerator : MonoBehaviour
{
    public NoiseParams[] noiseParameters;
    public Vector3Int chunkDimensions = new Vector3Int(1, 1, 1);
    public GameObject chunkPrefab;
    public GameObject chunksRoot;
    public float radius = 1f;
    public float seed;

    [Range(-1, 1)]
    public float isoLevel;
    public Vector3Int worldDimensions = new Vector3Int(1, 1, 1);
    public Vector3Int offset = Vector3Int.zero;

    private List<Chunk> chunks;

    private void Setup()
    {
        chunksRoot = GameObject.Find("Chunks");
        chunks = new List<Chunk>();
        int i = 0;
        for (int x = 0; x < worldDimensions.x; x++)
        {
            for (int y = 0; y < worldDimensions.y; y++)
            {
                for (int z = 0; z < worldDimensions.z; z++)
                {
                    Vector3 chunkOffset = new Vector3(x, y, z) + offset;
                    chunkOffset = new Vector3(chunkOffset.x * (chunkDimensions.x - 1), chunkOffset.y * (chunkDimensions.y - 1), chunkOffset.z * (chunkDimensions.z - 1));
                    GameObject chunkGameObject = Instantiate(chunkPrefab, chunkOffset, Quaternion.identity, chunksRoot.transform);
                    chunks.Add(chunkGameObject.GetComponent<Chunk>());
                    chunks[i].Init(chunkDimensions, chunkOffset, noiseParameters, this.seed);
                    i += 1;
                }
            }
        }
    }

    public void DestroyOrDisable()
    {
        foreach (Chunk chunk in chunks)
        {
            Destroy(chunk.gameObject);
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying || chunks == null)
        {
            return;
        }
        foreach (Chunk chunk in chunks)
        {
            Destroy(chunk.gameObject);
        }
        Setup();
    }

    private void Start()
    {
        Setup();
    }

    private void Update()
    {
        GenerateNewChunk(10);
    }

    private void GenerateNewChunk(int numChunks)
    {
        int count = 0;
        for (int i = 0; i < chunks.Count; i++)
        {
            if (!chunks[i].isGenerated)
            {
                chunks[i].Generate();
                count += 1;
                if (count >= numChunks)
                {
                    return;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        
        foreach (Chunk chunk in chunks)
        {
            int i = 0;
            for (int z = 0; z < chunk.dimensions.z; z++)
            {
                for (int y = 0; y < chunk.dimensions.y; y++)
                {
                    for (int x = 0; x < chunk.dimensions.x; x++)
                    {
                        Vector3 coord = new Vector3(x, y, z) + chunk.offset;
                        float weight = chunk.weights[i];

                        Gizmos.color = Color.Lerp(Color.red, Color.green, Mathf.Clamp01((weight + 1) / 2));
                        Gizmos.DrawCube(coord, Vector3.one * radius);
                        i += 1;
                    }
                }
            }
        }
    }
}


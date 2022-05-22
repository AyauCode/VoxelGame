using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainHandler : MonoBehaviour
{
    public bool generateNewTerrain = true;
    public static TerrainHandler instance;
    public GameObject chunkPrefab;
    public Transform viewer;
    public Vector3Int viewDist;
    public Vector3Int chunkDimensions;

    public Dictionary<Vector3Int, TerrainChunk> chunks = new Dictionary<Vector3Int, TerrainChunk>();
    public Dictionary<Vector3Int, TerrainChunk.SavedChunkData> chunkData = new Dictionary<Vector3Int, TerrainChunk.SavedChunkData>();

    Queue<TerrainChunk> chunkQueue = new Queue<TerrainChunk>();

    //public TextAsset textureAtlasData;

    public int seed;
    public float scale;
    public float baseRoughness;
    public float roughness;
    public float persistence;
    public float strength;
    public float recede;
    public int layers;
    public float cutoff;
    NoiseSettings noiseSettings;

    public float chunkQueueTime = 0.25f;
    float realChunkQueueTime = 0;
    float countdown;
    public void Start()
    {
        instance = this;
        noiseSettings = CreateNoiseSettings();
        UpdateTerrain();
        while(chunkQueue.Count > 0)
        {
            DoChunkQueue();
        }
        realChunkQueueTime = chunkQueueTime;
    }
    public TerrainChunk GetTerrainChunkContainingPoint(Vector3 point)
    {
        int x = Mathf.FloorToInt(point.x / chunkDimensions.x);
        int y = Mathf.FloorToInt(point.y / chunkDimensions.y);
        int z = Mathf.FloorToInt(point.z / chunkDimensions.z);

        Vector3Int chunkPos = new Vector3Int(x, y, z);
        return chunks.ContainsKey(chunkPos) ? chunks[chunkPos] : null;
    }
    [HideInInspector]
    public List<TerrainChunk> chunksToRegenerate;
    public void DestroyBlock(Vector3 pos)
    {
        TerrainChunk initialChunk = GetTerrainChunkContainingPoint(pos);
        chunksToRegenerate.Add(initialChunk);

        foreach(Vector3 dir in CustomMath.directions)
        {
            TerrainChunk adjChunk = GetTerrainChunkContainingPoint(pos + dir);
            if(adjChunk != initialChunk)
            {
                chunksToRegenerate.Add(adjChunk);
            }
        }

        TerrainChunk.SavedChunkData savedData = null;
        foreach (TerrainChunk tc in chunksToRegenerate)
        {
            if (chunkData.ContainsKey(tc.chunkCoord))
            {
                savedData = chunkData[tc.chunkCoord];
                savedData.AddByte(tc.GetLocalPosition(Vector3Int.FloorToInt(pos)), 0);
            }
            else
            {
                savedData = new TerrainChunk.SavedChunkData(tc.chunkCoord);
                savedData.AddByte(tc.GetLocalPosition(Vector3Int.FloorToInt(pos)), 0);
                chunkData.Add(tc.chunkCoord, savedData);
            }

            tc.GenerateChunk(tc.chunkCoord, new Vector3(tc.chunkCoord.x * chunkDimensions.x, tc.chunkCoord.y * chunkDimensions.y, tc.chunkCoord.z * chunkDimensions.z), noiseSettings, savedData);
        }
    }
    public void PlaceBlock(Vector3 pos)
    {
        TerrainChunk initialChunk = GetTerrainChunkContainingPoint(pos);
        chunksToRegenerate.Add(initialChunk);

        foreach (Vector3 dir in CustomMath.directions)
        {
            TerrainChunk adjChunk = GetTerrainChunkContainingPoint(pos + dir);
            if (adjChunk != initialChunk)
            {
                chunksToRegenerate.Add(adjChunk);
            }
        }

        TerrainChunk.SavedChunkData savedData = null;
        foreach (TerrainChunk tc in chunksToRegenerate)
        {
            if (chunkData.ContainsKey(tc.chunkCoord))
            {
                savedData = chunkData[tc.chunkCoord];
                savedData.AddByte(tc.GetLocalPosition(Vector3Int.FloorToInt(pos)), 1);
            }
            else
            {
                savedData = new TerrainChunk.SavedChunkData(tc.chunkCoord);
                savedData.AddByte(tc.GetLocalPosition(Vector3Int.FloorToInt(pos)), 1);
                chunkData.Add(tc.chunkCoord, savedData);
            }

            tc.GenerateChunk(tc.chunkCoord, new Vector3(tc.chunkCoord.x * chunkDimensions.x, tc.chunkCoord.y * chunkDimensions.y, tc.chunkCoord.z * chunkDimensions.z), noiseSettings, savedData);
        }
    }
    public TerrainChunk.SavedChunkData GetSavedData(Vector3Int terrainChunkPos)
    {
        if (chunkData.ContainsKey(terrainChunkPos))
        {
            return chunkData[terrainChunkPos];
        }
        return null;
    }
    public NoiseSettings CreateNoiseSettings()
    {
        return new NoiseSettings(seed, scale, baseRoughness, roughness, persistence, strength, recede, layers, cutoff);
    }
    private void Update()
    {
        if (viewer != null)
        {
            UpdateTerrain();

            for(int i = chunksToRegenerate.Count - 1; i >= 0; i--)
            {
                if (chunksToRegenerate[i].generated == true)
                {
                    chunksToRegenerate.RemoveAt(i);
                }
            }
        }
    }
    List<Vector3Int> removeKeys = new List<Vector3Int>();
    Vector3Int chunkCoord = Vector3Int.zero;
    void UpdateTerrain()
    {
        /*
         * Set all chunks to be removed
         * TODO: Find better way of doing this, its slow to have to set every chunk to be removed each time
         */
        removeKeys.Clear();
        foreach (KeyValuePair<Vector3Int, TerrainChunk> entry in chunks)
        {
            removeKeys.Add(entry.Key);
        }

        int currentX = Mathf.RoundToInt(viewer.position.x / chunkDimensions.x);
        int currentY = Mathf.RoundToInt(viewer.position.y / chunkDimensions.y);
        int currentZ = Mathf.RoundToInt(viewer.position.z / chunkDimensions.z);


        //Loop over all view distance in x and z
        for (int x = -viewDist.x; x < viewDist.x; x++)
        {
            for (int y = -viewDist.y; y < viewDist.y; y++)
            {
                for (int z = -viewDist.z; z < viewDist.z; z++)
                {
                    //Get current chunk coord
                    chunkCoord.x = currentX + x;
                    chunkCoord.y = currentY + y;
                    chunkCoord.z = currentZ + z;

                    //If chunk exists, then remove it from the remove list, otherwise make a new chunk object
                    if (chunks.ContainsKey(chunkCoord))
                    {
                        TerrainChunk terrainChunk = chunks[chunkCoord];
                        terrainChunk.WaitForByteArray();
                        removeKeys.Remove(chunkCoord);
                    }
                    else if(generateNewTerrain)
                    {
                        //Create a new chunk object and parent it to TerrainHandler GameObject
                        GameObject chunkObject = Instantiate(chunkPrefab);
                        chunkObject.transform.position = new Vector3(chunkCoord.x * chunkDimensions.x, chunkCoord.y * chunkDimensions.y, chunkCoord.z * chunkDimensions.z);
                        chunkObject.transform.SetParent(gameObject.transform, false);
                        chunkObject.SetActive(true);
                        TerrainChunk terrainChunk = chunkObject.GetComponent<TerrainChunk>();
                        //Add component to chunk dictionary and set its dimensions
                        chunks.Add(chunkCoord, terrainChunk);

                        terrainChunk.chunkCoord = chunkCoord;
                        terrainChunk.chunkSize = chunkDimensions;

                        if (!chunkQueue.Contains(terrainChunk))
                        {
                            chunkQueue.Enqueue(terrainChunk);
                        }

                        //Generate the chunk mesh
                    }
                }
            }
        }
        
        //Loop over all chunks still in remove list and remove them
        foreach (Vector3Int key in removeKeys)
        {
            GameObject chunkObj = chunks[key].gameObject;
            Destroy(chunkObj);

            chunks.Remove(key);
        }

        DoChunkQueue();
    }
    void DoChunkQueue()
    {
        countdown -= Time.deltaTime;
        if (countdown <= 0)
        {
            if (chunkQueue.Count > 0)
            {
                TerrainChunk tc = chunkQueue.Dequeue();
                if (chunks.ContainsKey(tc.chunkCoord))
                {
                    tc.GenerateChunk(tc.chunkCoord, new Vector3(tc.chunkCoord.x * chunkDimensions.x, tc.chunkCoord.y * chunkDimensions.y, tc.chunkCoord.z * chunkDimensions.z), noiseSettings, GetSavedData(tc.chunkCoord));
                }
            }
            countdown = realChunkQueueTime;
        }
    }
}
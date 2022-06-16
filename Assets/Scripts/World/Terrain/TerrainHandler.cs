using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using UnityEngine.Profiling;
using TMPro;
using System.Collections.Concurrent;

public class TerrainHandler : MonoBehaviour
{
    public static TerrainHandler instance;
    public GameObject chunkPrefab;
    public Transform viewer;
    public Vector3Int viewDist;
    public Vector3Int chunkDimensions;

    public Queue<GameObject> chunkPool = new Queue<GameObject>();
    /*
     * Dictionary of currently loaded chunks
     */
    public static Dictionary<Vector3Int, TerrainChunk> chunks = new Dictionary<Vector3Int, TerrainChunk>();
    /*
     * Dictionary of chunkData (this is used to saved placed and destroyed blocks)
     * (So when the chunk is unloaded and the player returns the edits to the terrain will still be there)
     */
    public Dictionary<Vector3Int, TerrainChunk.SavedChunkData> chunkData = new Dictionary<Vector3Int, TerrainChunk.SavedChunkData>();

    /*
     * Chunk generation queue (used to slow down speed of chunk loading for performance reasons)
     */
    Queue<TerrainChunk> chunkQueue = new Queue<TerrainChunk>();
    /*
     * Time between dequeues from the chunk generation queue
     */
    public float chunkQueueTime = 0.25f;

    /*
     * All values set in editor will be passed into the noise settings struct
     */
    [Header("NOISE SETTINGS")]
    NoiseProfile[] noiseProfiles;
    float freq;
    FastNoiseLite noise;

    [Header("EDITOR VARIABLES")]
    public Vector3 displayChunkPos;
    public Vector3Int displayChunkSize;

    [Header("DEBUG")]
    public TextMeshProUGUI textMesh;

    Thread chunkGenerateThread;

    public static ConcurrentQueue<Vector3Int> meshQueue = new ConcurrentQueue<Vector3Int>();
    static ConcurrentQueue<TerrainChunk> generateQueue = new ConcurrentQueue<TerrainChunk>();

    public void Awake()
    {
        instance = this;
        chunkGenerateThread = new Thread(GenerateChunks);
        chunkGenerateThread.Start();

        TerrainChunk.InitFaces();
    }
    public static int completedChunkCount = 0;
    public static int completedMeshCount = 0;
    public int maxChunks;
    public void Init(Transform player)
    {
        this.viewer = player;
        int startChunks = (viewDist.x * 2 + 1) * (viewDist.y * 2 + 1) * (viewDist.z * 2 + 1);
        NoiseSettings settings = CreateNoiseSettings();
        for(int i = 0; i < startChunks; i++)
        {
            //Create a new chunk object from prefab
            GameObject chunkObject = Instantiate(chunkPrefab);
            chunkObject.transform.SetParent(gameObject.transform, true);
            chunkObject.SetActive(false);
            chunkObject.GetComponent<TerrainChunk>().InstantiateChunkData(chunkDimensions.x * chunkDimensions.y * chunkDimensions.z, settings);
            chunkPool.Enqueue(chunkObject);
        }
        maxChunks = viewDist.x * 2 * viewDist.y * 2 * viewDist.z * 2;
        //Run the chunk generaiton loop once on startup to make sure chunks are loaded in when the player spawns
        UpdateTerrain();

    }
    /// <summary>
    /// Get a loaded terrain chunk containing the 3D point
    /// </summary>
    /// <param name="point">A 3D point in world space</param>
    /// <returns>A loaded TerrainChunk containing the point, if not chunk exists returns null</returns>
    public TerrainChunk GetTerrainChunkContainingPoint(Vector3 point)
    {
        /*
         * Calculate the x,y,z index of a chunk that contains the given point
         */
        int x = Mathf.FloorToInt(point.x / chunkDimensions.x);
        int y = Mathf.FloorToInt(point.y / chunkDimensions.y);
        int z = Mathf.FloorToInt(point.z / chunkDimensions.z);

        Vector3Int chunkPos = new Vector3Int(x, y, z);
        //If this point is within a loaded chunk return that chunk, otherwise return null
        return chunks.ContainsKey(chunkPos) ? chunks[chunkPos] : null;
    }
    //List to hold chunks that need to have their mesh recalculated on block update
    //(This is to make sure if a player places/destroy a block on a chunk border the surrounding chunks will be updated)
    [HideInInspector]
    public List<TerrainChunk> chunksToRegenerate;
    /// <summary>
    /// Destroys the block at the given position and updates necessary chunk meshes
    /// </summary>
    /// <param name="pos">A 3D point in World Space</param>
    public void DestroyBlock(Vector3 pos)
    {
        //Get terrain chunk containing the block to destroy
        TerrainChunk initialChunk = GetTerrainChunkContainingPoint(pos);
        //No need to check for null as a block destroy command cant be sent to an unloaded chunk
        //Add this chunk to the list to recalculate its mesh
        chunksToRegenerate.Add(initialChunk);

        //Loop through all directions
        foreach(Vector3 dir in CustomMath.directions)
        {
            //Get the 6 blocks surrrounding the block to destroy
            TerrainChunk adjChunk = GetTerrainChunkContainingPoint(pos + dir);
            //If terrain chunk returned is not the current chunk then this block is on a border
            if(adjChunk != initialChunk)
            {
                //Add this neighboring chunk to the list to recalculate its mesh
                chunksToRegenerate.Add(adjChunk);
            }
        }

        TerrainChunk.SavedChunkData savedData = null;
        //Loop over all chunks that need to be regenerated
        foreach (TerrainChunk tc in chunksToRegenerate)
        {
            //If the chunk to be regenerated has existing save data get the data object from the chunk
            if (chunkData.ContainsKey(tc.chunkCoord))
            {
                savedData = chunkData[tc.chunkCoord];
                //Set the block byte value at the given position to 0 (i.e. air) and store it in the saved data
                savedData.AddByte(tc.GetLocalPosition(Vector3Int.FloorToInt(pos)), 0);
            }
            else
            {
                //If we are here no save data has been made for this chunk yet, create one
                savedData = new TerrainChunk.SavedChunkData(tc.chunkCoord);
                //Set the block byte value at the given position to 0 (i.e. air) and store it in the saved data
                savedData.AddByte(tc.GetLocalPosition(Vector3Int.FloorToInt(pos)), 0);
                //Add this new data to the data dictionary for future reference
                chunkData.Add(tc.chunkCoord, savedData);
            }

            //Regenerate the chunk mesh
            //(passing in chunkCoord(an x,y,z index of the chunk), its world space position, the current noiseSettings, and its savedData)
            tc.PrepareChunk(tc.chunkCoord, tc.chunkWorldPos, savedData);
            tc.GenerateChunk();
        }
    }
    /// <summary>
    /// Places a block at the given position and updates necessary chunk meshes
    /// </summary>
    /// <param name="pos">A 3D point in World Space</param>
    public void PlaceBlock(Vector3 pos)
    {
        //Get the chunk that the block will be placed in
        TerrainChunk initialChunk = GetTerrainChunkContainingPoint(pos);
        //NOTE: No need to check for null as a block place update will never be sent to a chunk that isnt loaded
        //Add this chunk to the list to regenerate its mesh
        chunksToRegenerate.Add(initialChunk);

        //Loop through all directions
        foreach (Vector3 dir in CustomMath.directions)
        {
            //Get the 6 blocks surrrounding the position a block will be placed at
            TerrainChunk adjChunk = GetTerrainChunkContainingPoint(pos + dir);
            //If terrain chunk returned is not the current chunk then this block is on a border
            if (adjChunk != initialChunk)
            {
                //Add this neighboring chunk to the list to recalculate its mesh
                chunksToRegenerate.Add(adjChunk);
            }
        }

        TerrainChunk.SavedChunkData savedData = null;
        //Loop over all chunks to regenerate
        foreach (TerrainChunk tc in chunksToRegenerate)
        {
            //If chunk to regenerate has existing saved data get that data
            if (chunkData.ContainsKey(tc.chunkCoord))
            {
                savedData = chunkData[tc.chunkCoord];
                //Set the block byte value at the position to place to 1 (i.e. a solid block) and store it in the saved data
                savedData.AddByte(tc.GetLocalPosition(Vector3Int.FloorToInt(pos)), 1);
            }
            else
            {
                //If we are here the chunk to regenerate does not have existing save data, create one
                savedData = new TerrainChunk.SavedChunkData(tc.chunkCoord);
                //Set the block byte value at the position to place to 1 (i.e. a solid block) and store it in the saved data
                savedData.AddByte(tc.GetLocalPosition(Vector3Int.FloorToInt(pos)), 1);
                //Add this saved data to the dictionary for future reference
                chunkData.Add(tc.chunkCoord, savedData);
            }

            //Regenerate the chunk mesh
            //(passing in chunkCoord(an x,y,z index of the chunk), its world space position, the current noiseSettings, and its savedData)
            tc.PrepareChunk(tc.chunkCoord, tc.chunkWorldPos, savedData);
            tc.GenerateChunk();
        }
    }
    /// <summary>
    /// Get the saved data for the given chunk position
    /// </summary>
    /// <param name="terrainChunkPos">A 3D index in local chunk space</param>
    /// <returns>The saved data of the chunk, if no data exists return null</returns>
    public TerrainChunk.SavedChunkData GetSavedData(Vector3Int terrainChunkPos)
    {
        if (chunkData.ContainsKey(terrainChunkPos))
        {
            return chunkData[terrainChunkPos];
        }
        return null;
    }
    /// <summary>
    /// Create a new NoiseSettings struct (simply a holder for values)
    /// </summary>
    /// <returns>A NoiseSettings struct with the values from the TerrainHandler instance</returns>
    public NoiseSettings CreateNoiseSettings()
    {
        noise = new FastNoiseLite(1);

        return new NoiseSettings(noise);
    }
    Vector3Int queueChunk;
    int maxMesh;
    bool first = true;
    private void Update()
    {
        //If we have a player transform
        if (viewer != null)
        {
            textMesh.text = "Noise: " + completedChunkCount + "/" + maxChunks + "\nMesh: " + completedMeshCount + "/" + maxMesh;
            if (generateQueue.Count == 0 && meshQueue.TryDequeue(out queueChunk))
            {
                if (first) { maxMesh = meshQueue.Count + 1; first = false; }
                if (chunks.ContainsKey(queueChunk))
                {
                    chunks[queueChunk].FinishMesh();
                    completedMeshCount++;
                }
            }
            /*if (meshQueue.TryDequeue(out queueChunk))
            {
                if (chunks.ContainsKey(queueChunk))
                {
                    chunks[queueChunk].FinishMesh();
                }
            }*/
            //Update the terrain
            //(i.e. add/remove chunks to be loaded based on the viewer position)
            //UpdateTerrain();

            /*Loop over the current chunks that need regenerating
             *If the chunk has finished generating its mesh, remove it from the list
             *NOTE: This is done after updating the terrain to make sure there is no issues with accessing lists while modifying them
             *(i.e. all chunk load/unload calculations should be settled by this point)
             */
            for (int i = chunksToRegenerate.Count - 1; i >= 0; i--)
            {
                if (chunksToRegenerate[i].generated == true)
                {
                    chunksToRegenerate.RemoveAt(i);
                }
            }
        }
    }
    //A list of chunk indices to remove
    List<Vector3Int> removeKeys = new List<Vector3Int>();
    //A chunk index vector
    //(not a huge need to be outside of the UpdateTerrain method, only slightly more performant since we dont have to make a new Vector3Int each run)
    Vector3Int chunkCoord = Vector3Int.zero;
    Vector3 chunkWorldPos = Vector3.zero;
    /// <summary>
    /// Update the currently loaded TerrainChunks, load/unload chunks when they enter/leave the viewers view distance
    /// </summary>
    void UpdateTerrain()
    {
        //Assume all chunks are outside of the view range and well need to be unloaded
        foreach (KeyValuePair<Vector3Int, TerrainChunk> entry in chunks)
        {
            //Add their chunk index to the list to remove
            removeKeys.Add(entry.Key);
        }

        //Calculate the approximate chunk index the players position resides in
        //NOTE: This doesnt have to be exactly correct, only a general area is needed
        int currentX = Mathf.RoundToInt(viewer.position.x / chunkDimensions.x);
        int currentY = Mathf.RoundToInt(viewer.position.y / chunkDimensions.y);
        int currentZ = Mathf.RoundToInt(viewer.position.z / chunkDimensions.z);
        
        //Loop over all chunk indices within view from (-viewDist to viewDist in each direction)
        for (int x = -viewDist.x; x < viewDist.x; x++)
        {
            for (int y = -viewDist.y; y < viewDist.y; y++)
            {
                for (int z = -viewDist.z; z < viewDist.z; z++)
                {
                    //Get current chunk index
                    chunkCoord.Set(currentX + x, currentY + y, currentZ + z);
                    //If chunk exists (i.e. the chunk mesh has been generated), then remove it from the remove list as it is within the players view
                    if (chunks.ContainsKey(chunkCoord))
                    {
                        TerrainChunk terrainChunk = chunks[chunkCoord];
                        //terrainChunk.WaitForByteArray();
                        removeKeys.Remove(chunkCoord);
                    }
                    else
                    {
                        //Create a new chunk object from prefab
                        GameObject chunkObject;
                        if(chunkPool.Count > 0)
                        {
                            chunkObject = chunkPool.Dequeue();
                        }
                        else
                        {
                            chunkObject = Instantiate(chunkPrefab);
                        }
                        //Ger the TerrainChunk component from the newly created chunk object
                        TerrainChunk terrainChunk = chunkObject.GetComponent<TerrainChunk>();
                        terrainChunk.gameObject.transform.position = new Vector3(chunkCoord.x * chunkDimensions.x, chunkCoord.y * chunkDimensions.y, chunkCoord.z * chunkDimensions.z);

                        //Add new TerrainChunk component to chunk dictionary (key = chunk index, value = terrain chunk component)
                        chunks.Add(chunkCoord, terrainChunk);

                        //Set the TerrainChunks chunk index and world scale
                        //(this is used in a few calculations later so a dictionary key lookup does not have to be done each time)
                        terrainChunk.chunkCoord = chunkCoord;
                        terrainChunk.chunkSize = chunkDimensions;

                        chunkWorldPos.Set(chunkCoord.x * chunkDimensions.x, chunkCoord.y * chunkDimensions.y, chunkCoord.z * chunkDimensions.z);
                        
                        terrainChunk.PrepareChunk(chunkCoord, chunkWorldPos, GetSavedData(chunkCoord));
                        generateQueue.Enqueue(terrainChunk);
                    }
                }
            }
        }

        Vector3Int key;
        GameObject chunkObj;
        //Loop over all chunks still in remove list and remove them (i.e. they are out of the view distance)
        for(int i = removeKeys.Count-1; i >= 0; i--)
        {
            //Get the chunk game object from the TerrainChunk component in the dictionary
            key = removeKeys[i];
            chunkObj = chunks[key].gameObject;

            chunkObj.SetActive(false);

            chunkPool.Enqueue(chunkObj);
            
            chunks.Remove(key);
            removeKeys.RemoveAt(i);
        }
    }
    public static void GenerateChunks()
    {
        while (true)
        {
            if(generateQueue.TryDequeue(out TerrainChunk chunk))
            {
                chunk.GenerateChunk();
                completedChunkCount++;
            }
        }
    }
}
public struct NoiseSettings
{
    FastNoiseLite fastNoise;
    public NoiseSettings(FastNoiseLite noise)
    {
        this.fastNoise = noise;
    }
}
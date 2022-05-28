using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class TerrainHandler : MonoBehaviour
{
    public bool generateNewTerrain = true;
    public static TerrainHandler instance;
    public GameObject chunkPrefab;
    public Transform viewer;
    public Vector3Int viewDist;
    public Vector3Int chunkDimensions;

    /*
     * Dictionary of currently loaded chunks
     */
    public Dictionary<Vector3Int, TerrainChunk> chunks = new Dictionary<Vector3Int, TerrainChunk>();
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
    public int seed;
    public float frequency;
    public float strength;
    public float recede;
    public float cutoff;
    public FastNoiseLite.FractalType fractalType;
    public int octaves;
    public float lacunarity;
    public float gain;
    public float weightedStrength;
    public FastNoiseLite.DomainWarpType warpType;
    public float domainWarpAmplitude;

    NoiseSettings noiseSettings;
    FastNoiseLite noise;

    [Header("EDITOR VARIABLES")]
    public Vector3 displayChunkPos;
    public Vector3Int displayChunkSize;

    float realChunkQueueTime = 0;
    float countdown;

    public void Awake()
    {
        instance = this;
    }
    public void Init(Transform player)
    {
        this.viewer = player;
        //Construct noise setitngs struct
        noiseSettings = CreateNoiseSettings();
        //Run the chunk generaiton loop once on startup to make sure chunks are loaded in when the player spawns
        UpdateTerrain();
        //Spawn all the chunks that were added to the generation queue (no delay, again to make sure some chunks are loaded when the player spawns)
        while (chunkQueue.Count > 0)
        {
            DoChunkQueue();
        }
        realChunkQueueTime = chunkQueueTime;
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
            tc.GenerateChunk(tc.chunkCoord, new Vector3(tc.chunkCoord.x * chunkDimensions.x, tc.chunkCoord.y * chunkDimensions.y, tc.chunkCoord.z * chunkDimensions.z), noiseSettings, savedData);
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
            tc.GenerateChunk(tc.chunkCoord, new Vector3(tc.chunkCoord.x * chunkDimensions.x, tc.chunkCoord.y * chunkDimensions.y, tc.chunkCoord.z * chunkDimensions.z), noiseSettings, savedData);
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
        noise = new FastNoiseLite(seed);
        noise.SetFrequency(frequency);

        noise.SetFractalType(fractalType);
        noise.SetFractalOctaves(octaves);
        noise.SetFractalLacunarity(lacunarity);
        noise.SetFractalGain(gain);
        noise.SetFractalWeightedStrength(weightedStrength);

        noise.SetDomainWarpType(warpType);
        noise.SetDomainWarpAmp(domainWarpAmplitude);

        FastNoiseLite caveNoise = new FastNoiseLite(seed);
        caveNoise.SetFractalType(FastNoiseLite.FractalType.Ridged);
        caveNoise.SetFrequency(0.005f);
        caveNoise.SetFractalOctaves(octaves);
        caveNoise.SetFractalLacunarity(lacunarity);
        caveNoise.SetFractalGain(gain);
        caveNoise.SetFractalWeightedStrength(weightedStrength);
        noise.SetDomainWarpType(warpType);
        noise.SetDomainWarpAmp(domainWarpAmplitude);

        return new NoiseSettings(noise, caveNoise, cutoff, strength, recede);
    }
    private void Update()
    {
        //If we have a player transform
        if (viewer != null)
        {
            //Update the terrain
            //(i.e. add/remove chunks to be loaded based on the viewer position)
            UpdateTerrain();

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
    /// <summary>
    /// Update the currently loaded TerrainChunks, load/unload chunks when they enter/leave the viewers view distance
    /// </summary>
    void UpdateTerrain()
    {
        //Clear any chunks in the list to remove
        removeKeys.Clear();
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
                    chunkCoord.x = currentX + x;
                    chunkCoord.y = currentY + y;
                    chunkCoord.z = currentZ + z;

                    //If chunk exists (i.e. the chunk mesh has been generated), then remove it from the remove list as it is within the players view
                    if (chunks.ContainsKey(chunkCoord))
                    {
                        TerrainChunk terrainChunk = chunks[chunkCoord];
                        //Tell the chunk to check if its mesh generation job is complete
                        //This is to do with multithreading (the chunk must wait to update until the mesh has been generated)
                        terrainChunk.WaitForByteArray();
                        removeKeys.Remove(chunkCoord);
                    }
                    else if(generateNewTerrain)
                    {
                        //Create a new chunk object from prefab
                        GameObject chunkObject = Instantiate(chunkPrefab);
                        //Set the chunk game object world space position (chunk index * world space scale)
                        chunkObject.transform.position = new Vector3(chunkCoord.x * chunkDimensions.x, chunkCoord.y * chunkDimensions.y, chunkCoord.z * chunkDimensions.z);
                        //Parent the chunk game obejct to the TerrainHandler game object for organization purposes (however keep its current position in world space)
                        chunkObject.transform.SetParent(gameObject.transform, false);
                        //Make sure the chunk game object is activated
                        chunkObject.SetActive(true);

                        //Ger the TerrainChunk component from the newly created chunk object
                        TerrainChunk terrainChunk = chunkObject.GetComponent<TerrainChunk>();

                        //Add new TerrainChunk component to chunk dictionary (key = chunk index, value = terrain chunk component)
                        chunks.Add(chunkCoord, terrainChunk);

                        //Set the TerrainChunks chunk index and world scale
                        //(this is used in a few calculations later so a dictionary key lookup does not have to be done each time)
                        terrainChunk.chunkCoord = chunkCoord;
                        terrainChunk.chunkSize = chunkDimensions;

                        //If the current chunk generation queue does not have this chunk waiting to generated add the chunk to the queue
                        if (!chunkQueue.Contains(terrainChunk))
                        {
                            chunkQueue.Enqueue(terrainChunk);
                        }
                    }
                }
            }
        }
        //Loop over all chunks still in remove list and remove them (i.e. they are out of the view distance)
        foreach (Vector3Int key in removeKeys)
        {
            //Get the chunk game object from the TerrainChunk component in the dictionary
            GameObject chunkObj = chunks[key].gameObject;
            //Destroy the game object
            Destroy(chunkObj);

            chunks.Remove(key);
        }

        //Iterate over the chunks waiting to begin generation
        DoChunkQueue();
    }
    /// <summary>
    /// Dequeue chunks in mesh generation queue and start its mesh generation every chunkQueueTime time step
    /// </summary>
    void DoChunkQueue()
    {
        //Tick countdown down (using Time.deltaTime for correct time steps)
        countdown -= Time.deltaTime;
        //If countdown has finished continue into if
        if (countdown <= 0)
        {
            //If there are chunks remaining in the queue to generate
            if (chunkQueue.Count > 0)
            {
                //Dequeue the chunk
                TerrainChunk tc = chunkQueue.Dequeue();
                //Verify the chunk is still within the viewers view distance, if it is continue into the if
                if (chunks.ContainsKey(tc.chunkCoord))
                {
                    //Generate the chunk mesh
                    //(passing in chunkCoord(an x,y,z index of the chunk), its world space position, the current noiseSettings, and its savedData if it exists)
                    tc.GenerateChunk(tc.chunkCoord, new Vector3(tc.chunkCoord.x * chunkDimensions.x, tc.chunkCoord.y * chunkDimensions.y, tc.chunkCoord.z * chunkDimensions.z), noiseSettings, GetSavedData(tc.chunkCoord));
                }
            }
            //Reset the timer
            countdown = realChunkQueueTime;
        }
    }
}
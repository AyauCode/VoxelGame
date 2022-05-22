using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.ComponentModel;

public class TerrainChunk : MonoBehaviour
{
    public Vector3Int chunkCoord;
    public Vector3 chunkWorldPos;
    public Vector3Int chunkSize;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;
    Mesh mesh;

    BackgroundWorker meshWorker;
    BackgroundWorker byteArrayWorker;

    ChunkData chunkData;

    public bool generated = false;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
    }
    public Vector3Int GetLocalPosition(Vector3 pos)
    {
        return Vector3Int.FloorToInt(pos - this.chunkWorldPos);
    }
    public byte GetBlockByteValue(Vector3 pos, bool isLocalPoint)
    {
        if (isLocalPoint)
        {
            byte outByte = chunkData.GetByteValue(GetByteArrayIndex(pos, chunkSize));

            if (chunkData.savedData != null && chunkData.savedData.HasByte(Vector3Int.FloorToInt(pos)))
            {
                outByte = chunkData.savedData.GetByte(Vector3Int.FloorToInt(pos));
            }
            return outByte;
        }
        else
        {
            //Debug.Log("Chunk: " + chunkWorldPos + " Tried to Get: " + GetLocalPosition(pos));
            Vector3Int localPos = GetLocalPosition(pos);
            byte outByte = chunkData.GetByteValue(GetByteArrayIndex(localPos, chunkSize));

            if(chunkData.savedData != null && chunkData.savedData.HasByte(localPos))
            {
                outByte = chunkData.savedData.GetByte(localPos);
            }
            return outByte;
        }
    }
    public void WaitForByteArray()
    {
        if (!generated && byteArrayWorker != null && !byteArrayWorker.IsBusy && chunkData != null)
        {
            meshWorker = new BackgroundWorker();
            meshWorker.DoWork += new DoWorkEventHandler(GenerateMesh);
            meshWorker.RunWorkerAsync(chunkData);

            byteArrayWorker.Dispose();
            byteArrayWorker = null;
        }
        if (!generated && meshWorker != null && !meshWorker.IsBusy && chunkData != null)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = chunkData.vertexArray;
            mesh.triangles = chunkData.triangleArray;

            mesh.RecalculateNormals();

            UpdateMesh(mesh);
            generated = true;

            meshWorker.Dispose();
            meshWorker = null;
        }
    }
    public void ClearChunk()
    {
        if (byteArrayWorker != null)
        {
            byteArrayWorker.CancelAsync();
        }
        if (meshWorker != null)
        {
            meshWorker.CancelAsync();
        }

        generated = false;

        if (chunkData == null) return;
        chunkData.Clear();
    }
    public void GenerateChunk(Vector3Int chunkCoord, Vector3 chunkWorldPos, NoiseSettings settings, SavedChunkData savedData)
    {
        ClearChunk();

        byteArrayWorker = new BackgroundWorker();
        byteArrayWorker.DoWork += new DoWorkEventHandler(GenerateByteArray);

        this.chunkCoord = chunkCoord;
        this.chunkWorldPos = chunkWorldPos;
        chunkData = new ChunkData(chunkWorldPos, chunkSize, settings, chunkSize.x * chunkSize.y * chunkSize.z, savedData);

        //byteArrayThread.Start(chunkData);
        byteArrayWorker.RunWorkerAsync(chunkData);
    }
    public static void GenerateByteArray(object sender, DoWorkEventArgs e)
    {
        ChunkData chunkData = (ChunkData)e.Argument;

        for (int i = 0; i < chunkData.chunkSize.x; i++)
        {
            for(int j = 0; j < chunkData.chunkSize.y; j++)
            {
                for (int k = 0; k < chunkData.chunkSize.z; k++)
                {
                    Vector3 pos = new Vector3(i, j, k);
                    int index = GetByteArrayIndex(pos, chunkData.chunkSize);
                    chunkData.SetByteValue(index, GenerateBlock(chunkData.chunkWorldPos + pos, chunkData.settings));
                    if (((BackgroundWorker)sender).CancellationPending)
                    {
                        return;
                    }
                }
            }
        }
    }
    static void GenerateMesh(object sender, DoWorkEventArgs e)
    {
        ChunkData chunkData = (ChunkData)e.Argument;

        for (int i = 0; i < chunkData.chunkSize.x; i++)
        {
            for (int k = 0; k < chunkData.chunkSize.z; k++)
            {
                for (int j = 0; j < chunkData.chunkSize.y; j++)
                {
                    Vector3 pos = new Vector3(i, j, k);
                    if (chunkData.savedData != null && chunkData.savedData.HasByte(Vector3Int.FloorToInt(pos)) && chunkData.savedData.GetByte(Vector3Int.FloorToInt(pos)) == 0)
                    {
                        continue;
                    }
                    foreach (Vector3 dir in CustomMath.directions)
                    {
                        Vector3 surrounding = pos + dir;
                        Vector3Int surroundingInt = Vector3Int.FloorToInt(surrounding);

                        if(chunkData.byteArr[GetByteArrayIndex(pos, chunkData.chunkSize)] == 1 || (chunkData.savedData != null && chunkData.savedData.HasByte(Vector3Int.FloorToInt(pos)) && chunkData.savedData.GetByte(Vector3Int.FloorToInt(pos)) == 1))
                        {
                            if (surrounding.x < chunkData.chunkSize.x && surrounding.x >= 0 && surrounding.y < chunkData.chunkSize.y && surrounding.y >= 0 && surrounding.z < chunkData.chunkSize.z && surrounding.z >= 0)
                            {
                                if (chunkData.byteArr[GetByteArrayIndex(surrounding, chunkData.chunkSize)] == 0)
                                {
                                    Vector3[] wlDir = CustomMath.directionDictionary[dir];
                                    AddQuad(chunkData.vertices, chunkData.triangles, pos + wlDir[2], wlDir[0], wlDir[1], dir);
                                }
                                else if((chunkData.savedData != null && chunkData.savedData.HasByte(surroundingInt) && chunkData.savedData.GetByte(surroundingInt) == 0))
                                {
                                    Vector3[] wlDir = CustomMath.directionDictionary[dir];
                                    AddQuad(chunkData.vertices, chunkData.triangles, pos + wlDir[2], wlDir[0], wlDir[1], dir);
                                }
                            }
                            else if(GenerateBlock(chunkData.chunkWorldPos + surrounding, chunkData.settings) == 0 || (chunkData.savedData != null && chunkData.savedData.HasByte(surroundingInt) && chunkData.savedData.GetByte(surroundingInt) == 0))
                            {
                                Vector3[] wlDir = CustomMath.directionDictionary[dir];
                                AddQuad(chunkData.vertices, chunkData.triangles, pos + wlDir[2], wlDir[0], wlDir[1], dir);
                            }
                        }

                        if (((BackgroundWorker)sender).CancellationPending)
                        {
                            return;
                        }
                    }
                }
            }
        }
        chunkData.vertexArray = chunkData.vertices.ToArray();
        chunkData.triangleArray = chunkData.triangles.ToArray();
    }
    void UpdateMesh(Mesh mesh)
    {
        this.mesh = mesh;
        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
    }
    public static void AddQuad(List<Vector3> vertices, List<int> triangles, Vector3 pos, Vector3 widthDir, Vector3 lengthDir, Vector3 normal)
    {
        //Calculate top and bottom left, right vertex positions based on given direction vectors
        Vector3 vBottomLeft = Vector3.zero, vBottomRight = Vector3.zero, vTopLeft = Vector3.zero, vTopRight = Vector3.zero;

        vBottomLeft = pos;
        vBottomRight = pos + widthDir;
        vTopLeft = pos + lengthDir;
        vTopRight = pos + widthDir + lengthDir;

        //If normal vector is left or forward flip the triangle to render on correct side
        int vIndex = vertices.Count;
        if (normal == Vector3.left || normal == Vector3.forward || normal == Vector3.down)
        {
            triangles.Add(vIndex);
            triangles.Add(vIndex + 1);
            triangles.Add(vIndex + 2);

            triangles.Add(vIndex + 2);
            triangles.Add(vIndex + 1);
            triangles.Add(vIndex + 3);
        }
        else
        {
            triangles.Add(vIndex);
            triangles.Add(vIndex + 2);
            triangles.Add(vIndex + 1);

            triangles.Add(vIndex + 2);
            triangles.Add(vIndex + 3);
            triangles.Add(vIndex + 1);
        }

        /*
         * Add the vertices and uvs to their respective lists
         */
        vertices.Add(vBottomLeft);
        vertices.Add(vBottomRight);
        vertices.Add(vTopLeft);
        vertices.Add(vTopRight);
    }
    public static byte GenerateBlock(Vector3 pos, NoiseSettings settings)
    {
        /*float value1 = pos.y-GetNoiseValue(pos, featureSize, seed)*100;
        float value2 = Mathf.Abs(GetNoiseValue(pos, 50, seed+2000));
        byte output = 0;
        if (value1 < 0.5)
        {
            output = 1;
        }
        if(value2 > caveCutoff && output == 1)
        {
            output = 0;
        }*/
        byte output = 0;
        float val = LayeredNoise(pos,settings) - pos.y;
        if(val > settings.cutoff)
        {
            output = 1;
        }
        NoiseSettings caveSettings = settings;
        caveSettings.scale = 10f;
        val = Mathf.Abs(LayeredNoise(pos,caveSettings));
        if(output == 1 && val < 0.1)
        {
            output = 0;
        }
        return output;
    }
    public float GetNoiseValue(Vector3 pos, float featureSize, int seed)
    {
        return PerlinNoise3D((seed + pos.x) / featureSize, (seed + pos.y) / featureSize, (seed + pos.z) / featureSize);
    }

    public static int GetByteArrayIndex(Vector3 pos, Vector3Int chunkSize)
    {
        /* This index is based on the loops in the byte array generation (NOTE: If chunk width = height = depth this equation can be changed) */
        return (int)pos.x + chunkSize.x * ((int)pos.y + chunkSize.y * (int)pos.z);
    }
    public static float PerlinNoise3D(float x, float y, float z)
    {
        float xy = Mathf.PerlinNoise(x, y);
        float xz = Mathf.PerlinNoise(x, z);
        float yz = Mathf.PerlinNoise(y, z);
        float yx = Mathf.PerlinNoise(y, x);
        float zx = Mathf.PerlinNoise(z, x);
        float zy = Mathf.PerlinNoise(z, y);

        return (xy + xz + yz + yx + zx + zy) / 6;
    }
    public static float LayeredNoise(Vector3 pos, NoiseSettings settings)
    {
        float noiseValue = 0;
        float frequency = settings.baseRoughness;
        float amplitude = 1;
        for(int i = 0; i < settings.layers; i++)
        {
            Vector3 samplePos = new Vector3(pos.x/settings.scale * frequency + settings.seed, pos.y/settings.scale * frequency + settings.seed, pos.z/settings.scale * frequency + settings.seed);
            float v = PerlinNoise3D(samplePos.x, samplePos.y, samplePos.z);
            noiseValue += v * amplitude;
            frequency *= settings.roughness;
            amplitude *= settings.persistence;
        }

        noiseValue = Mathf.Max(0, noiseValue - settings.recede);
        return noiseValue * settings.strength;
    }
    class ChunkData
    {
        public List<Vector3> vertices = new List<Vector3>();
        public List<int> triangles = new List<int>();

        public Vector3[] vertexArray;
        public int[] triangleArray;

        public Vector3 chunkWorldPos;
        public Vector3Int chunkSize;
        public NoiseSettings settings;
        public byte[] byteArr;

        public SavedChunkData savedData;
        public ChunkData(Vector3 chunkWorldPos,  Vector3Int chunkSize, NoiseSettings noiseSettings, int byteArraySize, SavedChunkData savedData)
        {
            this.chunkWorldPos = chunkWorldPos;
            this.chunkSize = chunkSize;
            this.settings = noiseSettings;
            this.byteArr = new byte[byteArraySize];
            this.savedData = savedData;
        }
        public byte GetByteValue(int i)
        {
            return byteArr[i];
        }
        public void SetByteValue(int i, byte b)
        {
            this.byteArr[i] = b;
        }
        public void Clear()
        {
            vertices.Clear();
            triangles.Clear();
        }
    }
    public class SavedChunkData
    {
        Dictionary<Vector3Int, byte> storedBytes = new Dictionary<Vector3Int, byte>();
        //byte[] storedBytes;
        Vector3Int chunkPos;
        public SavedChunkData(Vector3Int chunkPos)
        {
            this.chunkPos = chunkPos;
            //this.storedBytes = new byte[chunkSize.x * chunkSize.y * chunkSize.z];
        }
        public void AddByte(Vector3Int pos, byte b)
        {
            if (storedBytes.ContainsKey(pos))
            {
                storedBytes[pos] = b;
            }
            else
            {
                storedBytes.Add(pos, b);
            }
        }
        public byte GetByte(Vector3Int pos)
        {
            if (HasByte(pos))
                return storedBytes[pos];
            return 0;
        }
        public bool HasByte(Vector3Int pos)
        {
            return storedBytes.ContainsKey(pos);
        }
        /*public void AddByte(Vector3Int pos, byte b)
        {
            storedBytes[GetByteArrayIndex(pos, chunkSize)] = b;
        }
        public void GetByte(Vector3Int pos)*/
        public Vector3Int GetChunkPos()
        {
            return this.chunkPos;
        }
    }
}

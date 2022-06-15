using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.ComponentModel;

[CustomEditor(typeof(TerrainHandler))]
public class NoiseEditor : Editor
{
    MeshPreview meshPreview;
    Mesh chunkMesh;
    TerrainChunk.ChunkData chunkData;
    public void OnDisable()
    {
        if (meshPreview != null)
        {
            meshPreview.Dispose();
        }
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUI.BeginChangeCheck();

        TerrainHandler terrainHandler = (TerrainHandler)target;
        if(GUILayout.Button("Generate Mesh"))
        {
            if(meshPreview != null)
            {
                meshPreview.Dispose();
            }
            System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);

            chunkData = new TerrainChunk.ChunkData(Vector3Int.zero, terrainHandler.displayChunkPos, terrainHandler.displayChunkSize, terrainHandler.displayChunkSize.x * terrainHandler.displayChunkSize.y * terrainHandler.displayChunkSize.z, null);

            double startTime = (System.DateTime.UtcNow - epochStart).TotalMilliseconds;

            TerrainChunk.GenerateByteArray(chunkData);
            double byteTime = (System.DateTime.UtcNow - epochStart).TotalMilliseconds;

            TerrainChunk.GenerateMeshGreedyFace(chunkData);
            double endTime = (System.DateTime.UtcNow - epochStart).TotalMilliseconds;

            Mesh chunkMesh = new Mesh();
            chunkMesh.vertices = chunkData.vertexArray;
            chunkMesh.triangles = chunkData.triangleArray;
            chunkMesh.RecalculateNormals();

            meshPreview = new MeshPreview(chunkMesh);

            Debug.Log("Generated <color=orange>Byte Array</color> in <color=green>" + (byteTime - startTime) + " </color>ms");
            Debug.Log("Generated <color=red>Mesh</color> in <color=green>" + (endTime - byteTime) + " </color>ms");
        }
    }
    public override bool HasPreviewGUI()
    {
        return true;
    }
    public override void OnPreviewGUI(Rect r, GUIStyle background)
    {
        base.OnPreviewGUI(r, background);
        if (meshPreview != null)
        {
            meshPreview.OnPreviewGUI(r, background);
        }
    }
    public override void OnPreviewSettings()
    {
        base.OnPreviewSettings();
        if(meshPreview != null)
        {
            meshPreview.OnPreviewSettings();
        }
    }
}

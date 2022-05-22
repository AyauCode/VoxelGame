using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudHandler : MonoBehaviour
{
    public int horizontalStackSize = 20;
    public float cloudHeight;
    public Mesh quadMesh;
    public Material cloudMat;
    float offset;

    public int layer;
    public Camera gameCamera;
    public Light worldLight;
    public Matrix4x4 matrix;
    public bool castShadows = true;
    public bool lockToCameraHorizontal = true;

    void Update()
    {
        if (lockToCameraHorizontal)
        {
            this.transform.position = new Vector3(gameCamera.transform.position.x, this.transform.position.y, gameCamera.transform.position.z);
        }
        cloudMat.SetFloat("_MiddleYValue", transform.position.y);
        cloudMat.SetFloat("_CloudHeight", cloudHeight);

        cloudMat.SetVector("_LightDir", worldLight.gameObject.transform.forward);
        cloudMat.SetColor("_LightColor", worldLight.color);

        offset = cloudHeight / horizontalStackSize / 2f;
        Vector3 startPosition = transform.position + (Vector3.up * (offset * horizontalStackSize / 2f));

        for (int i = 0; i < horizontalStackSize; i++)
        {
            matrix = Matrix4x4.TRS(startPosition - (Vector3.up * offset * i), transform.rotation, transform.localScale);
            
            Graphics.DrawMesh(quadMesh, matrix, cloudMat, layer, null, 0, null, castShadows, false, false); // otherwise just draw it now
        }
    }
}

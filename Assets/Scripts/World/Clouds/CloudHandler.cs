using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudHandler : MonoBehaviour
{
    /*
     * Code obtained from: https://www.youtube.com/watch?v=LLUUIAKFgWg
     */
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
        /*
         * This fixes the clouds positions to the cameras x and z position but keeps its y unchanged, this allows the clouds to seem as though they move with the camera
         */
        if (lockToCameraHorizontal)
        {
            this.transform.position = new Vector3(gameCamera.transform.position.x, this.transform.position.y, gameCamera.transform.position.z);
        }
        /*
         * Pass the midpoint of the cloud volumes and cloud quad step size to the shader
         */
        cloudMat.SetFloat("_MiddleYValue", transform.position.y);
        cloudMat.SetFloat("_CloudHeight", cloudHeight);

        /*
         * Pass in the main directional light to the shader (attempting to do directional lighting)
         */
        cloudMat.SetVector("_LightDir", worldLight.gameObject.transform.forward);
        cloudMat.SetColor("_LightColor", worldLight.color);

        /*
         * Calculate offset to move each quad upward based on how many quads there are
         */
        offset = cloudHeight / horizontalStackSize / 2f;
        /*
         * Calculate initial position of first quad
         */
        Vector3 startPosition = transform.position + (Vector3.up * (offset * horizontalStackSize / 2f));

        /*
         * Loop over the number of cloud quads given creating a transformation matrix for each quad and using Graphcs.DrawMesh
         * (This is more efficient than creating a bunch of new game objects that have a quad mesh in a mesh renderer)
         */
        for (int i = 0; i < horizontalStackSize; i++)
        {
            //Calculate the Transformation,Rotation, and Scale (rotation, and scale are unchanged between each quad)
            matrix = Matrix4x4.TRS(startPosition - (Vector3.up * offset * i), transform.rotation, transform.localScale);

            /*
             * Draw the given quadMesh, with calculated transformation matrix,
             * applying the cloud material connected to the cloud shader graph
             * Pass in layer(this doesnt really change anything except grouping of objects)
             * Pass null in for the Camera (this is due to an issue with URP and Graphics.DrawMesh() passing null draws this mesh to every camera in the game)
             * (If only the main game camera is passed in DrawMesh does not function correctly)
             * Pass 0 as there are no submeshes, null for material property block as it is not instanced,
             * Pass in the value controllable in editor to cast shadows, clouds should not receive shadows,
             * and light probes are not used in this project
             */
            Graphics.DrawMesh(quadMesh, matrix, cloudMat, layer, null, 0, null, castShadows, false, false);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class WaterBall : NetworkBehaviour
{
    public LiquidHandler liquidHandler;
    public float amt;

    private void Start()
    {
        this.liquidHandler = TerrainHandler.instance.gameObject.GetComponent<LiquidHandler>();
    }
    [ServerCallback]
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Terrain")
        {
            Vector3Int waterPos = Vector3Int.FloorToInt(this.transform.position);

            SpawnWaterClient(waterPos, amt);
        }
    }
    [ClientRpc]
    private void SpawnWaterClient(Vector3Int pos, float amt)
    {
        liquidHandler.SpawnWater(pos, amt);
        GameObject.Destroy(this.gameObject);
    }

}

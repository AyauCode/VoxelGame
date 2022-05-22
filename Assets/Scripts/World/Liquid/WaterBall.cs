using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterBall : MonoBehaviour
{
    public LiquidHandler liquidHandler;
    public float amt;

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Terrain")
        {
            Vector3Int waterPos = Vector3Int.FloorToInt(this.transform.position);
            liquidHandler.SpawnWater(waterPos, amt);
            GameObject.Destroy(this.gameObject);
        }
    }
}

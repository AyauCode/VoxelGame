using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterCell
{
    LiquidHandler liquidHandler;
    public float mass;
    public float newMass;
    public GameObject waterPrefab, waterObject;
    public Vector3Int pos;
    public WaterCell(LiquidHandler liquidHandler, GameObject waterPrefab, Vector3Int pos)
    {
        this.liquidHandler = liquidHandler;
        this.waterPrefab = waterPrefab;
        this.pos = pos;
    }
    public void DestroyWaterObject()
    {
        if(waterObject != null)
        {
            GameObject.Destroy(waterObject);
        }
        waterObject = null;
    }
    public void WaterFill(float amt)
    {
        waterObject.transform.localScale = new Vector3(1, amt, 1);
        UpdateWaterBlockPos();
    }
    public void UpdateWaterBlockPos()
    {
        waterObject.transform.position = (Vector3)pos + new Vector3(0.5f, waterObject.GetComponent<MeshRenderer>().bounds.size.y / 2f, 0.5f);
    }
    public void TryInstantiateWaterObject()
    {
        if(waterObject == null)
        {
            waterObject = GameObject.Instantiate(waterPrefab);
            waterObject.transform.SetParent(liquidHandler.gameObject.transform);
        }
    }
}

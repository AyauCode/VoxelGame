using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiquidHandler : MonoBehaviour
{
    public TerrainHandler terrainHandler;
    public GameObject waterPrefab;
    public bool simulate = false;
    [Range(0,10)]
    public float stepDelay = 1;


    readonly float MaxMass = 1.0f; //The normal, un-pressurized mass of a full water cell
    readonly float MaxCompress = 0.02f; //How much excess water a cell can store, compared to the cell above it
    readonly float MinMass = 0.0001f;  //Ignore cells that are almost dry

    readonly float MinFlow = .01f;
    readonly float MaxSpeed = 1f;

    Dictionary<Vector3Int, WaterCell> waterCells = new Dictionary<Vector3Int, WaterCell>();
    // Update is called once per frame
    private void Start()
    {
        countdown = stepDelay;
    }
    void Update()
    {
        if (simulate && terrainHandler.chunksToRegenerate.Count == 0)
        {
            countdown -= Time.deltaTime;
            if (countdown <= 0)
            {
                SimulateWater();
                GenerateWaterMesh();
                countdown = stepDelay;
            }
        }
    }
    public void SpawnWater(Vector3Int pos, float amt)
    {
        if (waterCells.ContainsKey(pos))
        {
            waterCells[pos].mass += amt;
        }
        else
        {
            WaterCell newCell = new WaterCell(this, waterPrefab, pos);
            newCell.mass = amt;
            waterCells.Add(newCell.pos, newCell);
        }
    }
    /*public void SpawnWaterBlob(int amt)
    {
        if (chunkWorldPos == Vector3.zero)
            this.massArr[GetByteArrayIndex(new Vector3(8, 10, 8), chunkSize)] = amt;
    }*/
    readonly float MinDraw = 0.01f;
    readonly float MaxDraw = 1.1f;
    List<Vector3Int> waterToRemove = new List<Vector3Int>();

    float countdown;
    public void GenerateWaterMesh()
    {
        waterToRemove.Clear();

        foreach (KeyValuePair<Vector3Int,WaterCell> entry in waterCells)
        {
            Vector3Int pos = entry.Key;
            Vector3Int above = pos + new Vector3Int(0, 1, 0);
            Vector3Int below = pos + new Vector3Int(0, -1, 0);
            WaterCell cell = entry.Value;
            if(cell.mass <= MinDraw)
            {
                cell.DestroyWaterObject();
                if(cell.mass <= MinMass)
                {
                    waterToRemove.Add(pos);
                }
            }
            else
            {
                cell.TryInstantiateWaterObject();
                cell.WaterFill(Mathf.Clamp(cell.mass, 0, 1));

                if (waterCells.ContainsKey(above) && waterCells[above].mass > MinDraw)
                {
                    cell.WaterFill(1);
                }
            }
        }
        foreach(Vector3Int pos in waterToRemove)
        {
            waterCells.Remove(pos);
        }
    }
    public WaterCell TryNewCell(Vector3 pos)
    {
        Vector3Int newCellPos = Vector3Int.FloorToInt(pos);
        if (waterCells.ContainsKey(newCellPos))
        {
            return waterCells[newCellPos];
        }
        else if (waterToAdd.ContainsKey(newCellPos))
        {
            return waterToAdd[newCellPos];
        }
        else
        {
            WaterCell newWaterCell = new WaterCell(this, waterPrefab, newCellPos);
            waterToAdd.Add(newCellPos,newWaterCell);
            return newWaterCell;
        }
    }
    Dictionary<Vector3Int, WaterCell> waterToAdd = new Dictionary<Vector3Int, WaterCell>();
    public void SimulateWater()
    {
        waterToAdd.Clear();

        float Flow = 0;
        float remaining_mass;

        Vector3 pos = Vector3.zero, nextPos = Vector3.zero;
        //Calculate and apply flow for each block
        foreach (KeyValuePair<Vector3Int, WaterCell> entry in waterCells)
        {
            pos = entry.Key;
            WaterCell currentCell = entry.Value;

            //Custom push-only flow
            Flow = 0;
            remaining_mass = entry.Value.mass;

            //-------------------------------------------------------
            if (remaining_mass <= 0) continue;

            //The block below this one
            nextPos.x = pos.x;
            nextPos.y = pos.y - 1;
            nextPos.z = pos.z;
            TerrainChunk nextChunk = terrainHandler.GetTerrainChunkContainingPoint(nextPos);
            if (nextChunk != null && nextChunk.generated && nextChunk.GetBlockByteValue(nextPos,false) != 1)
            {
                WaterCell nextCell = TryNewCell(nextPos);
                Flow = get_stable_state_b(remaining_mass + nextCell.mass) - nextCell.mass;
                if (Flow > MinFlow)
                {
                    Flow *= 0.5f; //leads to smoother flow
                }
                Flow = Mathf.Clamp(Flow, 0, Mathf.Min(MaxSpeed, remaining_mass));

                currentCell.newMass -= Flow;
                nextCell.newMass += Flow;

                remaining_mass -= Flow;
            }

            //-------------------------------------------------------
            if (remaining_mass <= 0) continue;

            nextPos.x = pos.x - 1;
            nextPos.y = pos.y;
            nextPos.z = pos.z;
            //Left
            nextChunk = terrainHandler.GetTerrainChunkContainingPoint(nextPos);
            if (nextChunk != null && nextChunk.generated && nextChunk.GetBlockByteValue(nextPos, false) != 1)
            {
                WaterCell nextCell = TryNewCell(nextPos);
                //Equalize the amount of water in this block and it's neighbour
                Flow = (currentCell.mass - nextCell.mass) / 6;
                if (Flow > MinFlow) { Flow *= 0.5f; }
                Flow = Mathf.Clamp(Flow, 0, remaining_mass);

                currentCell.newMass -= Flow;
                nextCell.newMass += Flow;

                remaining_mass -= Flow;
            }

            //-------------------------------------------------------
            if (remaining_mass <= 0) continue;

            nextPos.x = pos.x;
            nextPos.y = pos.y;
            nextPos.z = pos.z + 1;
            //Forward
            nextChunk = terrainHandler.GetTerrainChunkContainingPoint(nextPos);
            if (nextChunk != null && nextChunk.generated && nextChunk.GetBlockByteValue(nextPos, false) != 1)
            {
                WaterCell nextCell = TryNewCell(nextPos);
                //Equalize the amount of water in this block and it's neighbour
                Flow = (currentCell.mass - nextCell.mass) / 6;
                if (Flow > MinFlow) { Flow *= 0.5f; }
                Flow = Mathf.Clamp(Flow, 0, remaining_mass);

                currentCell.newMass -= Flow;
                nextCell.newMass += Flow;

                remaining_mass -= Flow;
            }

            //-------------------------------------------------------
            if (remaining_mass <= 0) continue;

            nextPos.x = pos.x + 1;
            nextPos.y = pos.y;
            nextPos.z = pos.z;
            //Right
            nextChunk = terrainHandler.GetTerrainChunkContainingPoint(nextPos);
            if (nextChunk != null && nextChunk.generated && nextChunk.GetBlockByteValue(nextPos, false) != 1)
            {
                WaterCell nextCell = TryNewCell(nextPos);
                //Equalize the amount of water in this block and it's neighbour
                Flow = (currentCell.mass - nextCell.mass) / 6;
                if (Flow > MinFlow) { Flow *= 0.5f; }
                Flow = Mathf.Clamp(Flow, 0, remaining_mass);

                currentCell.newMass -= Flow;
                nextCell.newMass += Flow;
                remaining_mass -= Flow;
            }

            //-------------------------------------------------------
            if (remaining_mass <= 0) continue;

            nextPos.x = pos.x;
            nextPos.y = pos.y;
            nextPos.z = pos.z - 1;
            //Backward
            nextChunk = terrainHandler.GetTerrainChunkContainingPoint(nextPos);
            if (nextChunk != null && nextChunk.generated && nextChunk.GetBlockByteValue(nextPos, false) != 1)
            {
                WaterCell nextCell = TryNewCell(nextPos);
                //Equalize the amount of water in this block and it's neighbour
                Flow = (currentCell.mass - nextCell.mass) / 6;
                if (Flow > MinFlow) { Flow *= 0.5f; }
                Flow = Mathf.Clamp(Flow, 0, remaining_mass);

                currentCell.newMass -= Flow;
                nextCell.newMass += Flow;

                remaining_mass -= Flow;
            }

            //-------------------------------------------------------
            if (remaining_mass <= 0) continue;

            nextPos.x = pos.x;
            nextPos.y = pos.y + 1;
            nextPos.z = pos.z;
            //Up. Only compressed water flows upwards.
            nextChunk = terrainHandler.GetTerrainChunkContainingPoint(nextPos);
            if (nextChunk != null && nextChunk.generated && nextChunk.GetBlockByteValue(nextPos, false) != 1)
            {
                WaterCell nextCell = TryNewCell(nextPos);
                Flow = remaining_mass - get_stable_state_b(remaining_mass + nextCell.mass);
                if (Flow > MinFlow) { Flow *= 0.5f; }
                Flow = Mathf.Clamp(Flow, 0, Mathf.Min(MaxSpeed, remaining_mass));

                currentCell.newMass -= Flow;
                nextCell.newMass += Flow;

                remaining_mass -= Flow;
            }
        }

        /*
        for (int x = 1; x < chunkSize.x - 1; x++)
        {
            for (int y = 1; y < chunkSize.y - 1; y++)
            {
                for (int z = 1; z < chunkSize.z - 1; z++)
                {
                    pos.x = x;
                    pos.y = y;
                    pos.z = z;

                    Vector3 basePos = pos;
                    //Skip inert ground blocks
                    if (chunkData.GetByteValue(GetByteArrayIndex(pos, chunkSize)) == 1) continue;

                    //Custom push-only flow
                    Flow = 0;
                    remaining_mass = massArr[GetByteArrayIndex(pos, chunkSize)];

                    //-------------------------------------------------------
                    if (remaining_mass <= 0) continue;

                    //The block below this one
                    pos.x = x;
                    pos.y = y - 1;
                    pos.z = z;
                    if (chunkData.GetByteValue(GetByteArrayIndex(pos, chunkSize)) != 1 && y - 1 != 0)
                    {
                        Flow = get_stable_state_b(remaining_mass + massArr[GetByteArrayIndex(pos, chunkSize)]) - massArr[GetByteArrayIndex(pos, chunkSize)];
                        if (Flow > MinFlow)
                        {
                            Flow *= 0.5f; //leads to smoother flow
                        }
                        Flow = Mathf.Clamp(Flow, 0, Mathf.Min(MaxSpeed, remaining_mass));


                        newMassArr[GetByteArrayIndex(basePos, chunkSize)] -= Flow;
                        newMassArr[GetByteArrayIndex(pos, chunkSize)] += Flow;

                        remaining_mass -= Flow;
                    }

                    //-------------------------------------------------------
                    if (remaining_mass <= 0) continue;

                    pos.x = x - 1;
                    pos.y = y;
                    pos.z = z;
                    //Left
                    if (chunkData.GetByteValue(GetByteArrayIndex(pos, chunkSize)) != 1 && x - 1 != 0)
                    {
                        //Equalize the amount of water in this block and it's neighbour
                        Flow = (massArr[GetByteArrayIndex(basePos, chunkSize)] - massArr[GetByteArrayIndex(pos, chunkSize)]) / 6;
                        if (Flow > MinFlow) { Flow *= 0.5f; }
                        Flow = Mathf.Clamp(Flow, 0, remaining_mass);

                        newMassArr[GetByteArrayIndex(basePos, chunkSize)] -= Flow;
                        newMassArr[GetByteArrayIndex(pos, chunkSize)] += Flow;

                        remaining_mass -= Flow;
                    }
                    //-------------------------------------------------------
                    if (remaining_mass <= 0) continue;

                    pos.x = x;
                    pos.y = y;
                    pos.z = z + 1;
                    //Forward
                    if (chunkData.GetByteValue(GetByteArrayIndex(pos, chunkSize)) != 1 && z + 1 != chunkSize.z - 1)
                    {
                        //Equalize the amount of water in this block and it's neighbour
                        Flow = (massArr[GetByteArrayIndex(basePos, chunkSize)] - massArr[GetByteArrayIndex(pos, chunkSize)]) / 6;
                        if (Flow > MinFlow) { Flow *= 0.5f; }
                        Flow = Mathf.Clamp(Flow, 0, remaining_mass);

                        newMassArr[GetByteArrayIndex(basePos, chunkSize)] -= Flow;
                        newMassArr[GetByteArrayIndex(pos, chunkSize)] += Flow;

                        remaining_mass -= Flow;
                    }

                    //-------------------------------------------------------
                    if (remaining_mass <= 0) continue;

                    pos.x = x + 1;
                    pos.y = y;
                    pos.z = z;
                    //Right
                    if (chunkData.GetByteValue(GetByteArrayIndex(pos, chunkSize)) != 1 && x + 1 != chunkSize.x - 1)
                    {
                        //Equalize the amount of water in this block and it's neighbour
                        Flow = (massArr[GetByteArrayIndex(basePos, chunkSize)] - massArr[GetByteArrayIndex(pos, chunkSize)]) / 6;
                        if (Flow > MinFlow) { Flow *= 0.5f; }
                        Flow = Mathf.Clamp(Flow, 0, remaining_mass);

                        newMassArr[GetByteArrayIndex(basePos, chunkSize)] -= Flow;
                        newMassArr[GetByteArrayIndex(pos, chunkSize)] += Flow;
                        remaining_mass -= Flow;
                    }

                    //-------------------------------------------------------
                    if (remaining_mass <= 0) continue;

                    pos.x = x;
                    pos.y = y;
                    pos.z = z - 1;
                    //Backward
                    if (chunkData.GetByteValue(GetByteArrayIndex(pos, chunkSize)) != 1 && z - 1 != 0)
                    {
                        //Equalize the amount of water in this block and it's neighbour
                        Flow = (massArr[GetByteArrayIndex(basePos, chunkSize)] - massArr[GetByteArrayIndex(pos, chunkSize)]) / 6;
                        if (Flow > MinFlow) { Flow *= 0.5f; }
                        Flow = Mathf.Clamp(Flow, 0, remaining_mass);

                        newMassArr[GetByteArrayIndex(basePos, chunkSize)] -= Flow;
                        newMassArr[GetByteArrayIndex(pos, chunkSize)] += Flow;

                        remaining_mass -= Flow;
                    }

                    //-------------------------------------------------------
                    if (remaining_mass <= 0) continue;

                    pos.x = x;
                    pos.y = y + 1;
                    pos.z = z;
                    //Up. Only compressed water flows upwards.
                    if (chunkData.GetByteValue(GetByteArrayIndex(pos, chunkSize)) != 1 && y + 1 != chunkSize.y - 1)
                    {
                        Flow = remaining_mass - get_stable_state_b(remaining_mass + massArr[GetByteArrayIndex(pos, chunkSize)]);
                        if (Flow > MinFlow) { Flow *= 0.5f; }
                        Flow = Mathf.Clamp(Flow, 0, Mathf.Min(MaxSpeed, remaining_mass));

                        newMassArr[GetByteArrayIndex(basePos, chunkSize)] -= Flow;
                        newMassArr[GetByteArrayIndex(pos, chunkSize)] += Flow;

                        remaining_mass -= Flow;
                    }
                }

            }
        }*/

        foreach(KeyValuePair<Vector3Int, WaterCell> entry in waterToAdd)
        {
            waterCells.Add(entry.Key, entry.Value);
        }
        foreach (KeyValuePair<Vector3Int, WaterCell> entry in waterCells)
        {
            entry.Value.mass = entry.Value.newMass;
        }
        /*//Copy the new mass values to the mass array
        for (int x = 1; x < chunkSize.x - 1; x++)
        {
            for (int y = 1; y < chunkSize.y - 1; y++)
            {
                for (int z = 1; z < chunkSize.z - 1; z++)
                {
                    Vector3 currentPos = new Vector3(x, y, z);
                    massArr[GetByteArrayIndex(currentPos, chunkSize)] = newMassArr[GetByteArrayIndex(currentPos, chunkSize)];
                }
            }
        }*/

        /*for (int x = 1; x < chunkSize.x - 1; x++)
        {
            for (int y = 1; y < chunkSize.y - 1; y++)
            {
                for (int z = 1; z < chunkSize.z - 1; z++)
                {
                    Vector3 currentPos = new Vector3(x, y, z);

                    //Skip ground blocks
                    if (chunkData.byteArr[GetByteArrayIndex(currentPos, chunkSize)] == 1) continue;
                    //Flag/unflag water blocks
                    if (massArr[GetByteArrayIndex(currentPos, chunkSize)] > MinMass)
                    {
                        chunkData.byteArr[GetByteArrayIndex(currentPos, chunkSize)] = 2;
                    }
                    else
                    {
                        chunkData.byteArr[GetByteArrayIndex(currentPos, chunkSize)] = 0;
                    }
                }
            }
        }*/
        /*
        //Remove any water that has left the map
        for (int x = 0; x < map_width + 2; x++)
        {
            mass[x][0] = 0;
            mass[x][map_height + 1] = 0;
        }
        for (int y = 1; y < map_height + 1; y++)
        {
            mass[0][y] = 0;
            mass[map_width + 1][y] = 0;
        }*/
    }
    //Returns the amount of water that should be in the bottom cell.
    float get_stable_state_b(float total_mass)
    {
        if (total_mass <= 1)
        {
            return 1;
        }
        else if (total_mass < 2 * MaxMass + MaxCompress)
        {
            return (MaxMass * MaxMass + total_mass * MaxCompress) / (MaxMass + MaxCompress);
        }
        else
        {
            return (total_mass + MaxCompress) / 2;
        }
    }
}

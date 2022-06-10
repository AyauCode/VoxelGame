using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

static class CustomMath
{
    /*
     * Custom dictionary that maps a given normal to the directions that a quad needs to be constructed with given normal
     */
    public static readonly Dictionary<Vector3, Vector3[]> directionDictionary = new Dictionary<Vector3, Vector3[]> {
        { Vector3.up, new Vector3[]{ Vector3.right, Vector3.forward, Vector3.up} },
        { Vector3.down, new Vector3[] { Vector3.right, Vector3.forward, Vector3.zero } },
        { Vector3.left, new Vector3[] { Vector3.forward, Vector3.up , Vector3.zero} },
        { Vector3.right, new Vector3[] { Vector3.forward, Vector3.up , Vector3.right} },
        { Vector3.forward, new Vector3[] { Vector3.right, Vector3.up , Vector3.forward} },
        { Vector3.back, new Vector3[] { Vector3.right, Vector3.up , Vector3.zero} },
    };
    public static readonly Dictionary<Vector3, Vector3Int[]> intDirectionDictionary = new Dictionary<Vector3, Vector3Int[]> {
        { Vector3.up, new Vector3Int[]{ Vector3Int.right, Vector3Int.forward, Vector3Int.up} },
        { Vector3.down, new Vector3Int[] { Vector3Int.right, Vector3Int.forward, Vector3Int.zero } },
        { Vector3.left, new Vector3Int[] { Vector3Int.forward, Vector3Int.up , Vector3Int.zero} },
        { Vector3.right, new Vector3Int[] { Vector3Int.forward, Vector3Int.up , Vector3Int.right} },
        { Vector3.forward, new Vector3Int[] { Vector3Int.right, Vector3Int.up , Vector3Int.forward} },
        { Vector3.back, new Vector3Int[] { Vector3Int.right, Vector3Int.up , Vector3Int.zero} },
    };
    /*
     * Custom array of all the directions to easily loop over
     */
    public static readonly Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
    public static readonly Vector3Int[] intDirections = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };
    public static readonly int NUMDIRECTIONS = 6;
}

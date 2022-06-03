using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NoiseProfile", menuName = "ScriptableObjects/NoiseProfile")]
public class NoiseProfile : ScriptableObject
{
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
}

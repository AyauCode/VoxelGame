using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct NoiseSettings
{
    /*
     * Simple storage struct with values to easily pass around
     */
    public int seed;
    public float scale;
    public float caveScale;
    public float baseRoughness;
    public float roughness;
    public float persistence;
    public float strength;
    public float recede;
    public int layers;
    public float cutoff;
    public float caveCutoff;
    public NoiseSettings(int seed, float scale, float caveScale, float baseRoughness, float roughness, float persistence, float strength, float recede, int layers, float cutoff, float caveCutoff)
    {
        this.seed = seed;
        this.scale = scale;
        this.caveScale = caveScale;
        this.baseRoughness = baseRoughness;
        this.roughness = roughness;
        this.persistence = persistence;
        this.strength = strength;
        this.recede = recede;
        this.layers = layers;
        this.cutoff = cutoff;
        this.caveCutoff = caveCutoff;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct NoiseSettings
{
    public int seed;
    public float scale;
    public float baseRoughness;
    public float roughness;
    public float persistence;
    public float strength;
    public float recede;
    public int layers;
    public float cutoff;
    public NoiseSettings(int seed, float scale, float baseRoughness, float roughness, float persistence, float strength, float recede, int layers, float cutoff)
    {
        this.seed = seed;
        this.scale = scale;
        this.baseRoughness = baseRoughness;
        this.roughness = roughness;
        this.persistence = persistence;
        this.strength = strength;
        this.recede = recede;
        this.layers = layers;
        this.cutoff = cutoff;
    }
}

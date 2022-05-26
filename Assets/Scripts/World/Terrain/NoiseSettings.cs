using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct NoiseSettings
{
    /*
     * Simple storage struct with values to easily pass around
     */
    public FastNoiseLite fastNoise, caveNoise;
    public float cutoff;
    public float strength;
    public float recede;
    public NoiseSettings(FastNoiseLite fastNoise, FastNoiseLite caveNoise, float cutoff, float strength, float recede)
    {
        this.fastNoise = fastNoise;
        this.caveNoise = caveNoise;
        this.cutoff = cutoff;
        this.strength = strength;
        this.recede = recede;
    }
}

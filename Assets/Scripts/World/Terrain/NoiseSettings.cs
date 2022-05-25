using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct NoiseSettings
{
    /*
     * Simple storage struct with values to easily pass around
     */
    public FastNoiseLite fastNoise;
    public float cutoff;
    public float strength;
    public float recede;
    public NoiseSettings(FastNoiseLite fastNoise, float cutoff, float strength, float recede)
    {
        this.fastNoise = fastNoise;
        this.cutoff = cutoff;
        this.strength = strength;
        this.recede = recede;
    }
}

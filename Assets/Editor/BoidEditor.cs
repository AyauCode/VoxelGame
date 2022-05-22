using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BoidHandler))]
public class BoidEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        BoidHandler boidHandler = (BoidHandler)target;
        if(GUILayout.Button("Reset Boids"))
        {
            boidHandler.ResetBoids();
        }
    }
}

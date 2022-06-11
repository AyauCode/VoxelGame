using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FPSUI : MonoBehaviour
{
    public string baseString;
    public TextMeshProUGUI textMesh;

    int m_frameCounter = 0;
    float m_timeCounter = 0.0f;
    float m_lastFramerate = 0.0f;
    public float m_refreshTime = 0.5f;


    void Update()
    {
        if (m_timeCounter < m_refreshTime)
        {
            m_timeCounter += Time.deltaTime;
            m_frameCounter++;
        }
        else
        {
            m_lastFramerate = (float)m_frameCounter / m_timeCounter;
            m_frameCounter = 0;
            m_timeCounter = 0.0f;
        }
        this.textMesh.text = baseString + (int)m_lastFramerate;
    }
}

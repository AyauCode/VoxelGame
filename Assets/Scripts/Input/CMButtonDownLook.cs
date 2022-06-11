using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

public class CMButtonDownLook : MonoBehaviour
{
    InputAction qPress, ePress;
    bool eDown, qDown;
    void Start()
    {
        CinemachineCore.GetInputAxis = GetAxisCustom;
    }
    public void Init(InputAction qPress, InputAction ePress)
    {
        this.qPress = qPress;
        this.ePress = ePress;
        this.ePress.Enable();
        this.qPress.Enable();

        this.qPress.performed += QPressDown;
        this.qPress.canceled += QPressUp;

        this.ePress.performed += EPressDown;
        this.ePress.canceled += EPressUp;
    }
    float lerpSpeed = 0.125f;
    float dir;
    public float GetAxisCustom(string axisName)
    {
        if (axisName == "MyLook")
        {
            if (eDown)
            {
                return -1;
            }
            if (qDown)
            {
                return 1;
            }
        }
        return 0;
    }
    public void QPressDown(InputAction.CallbackContext context)
    {
        qDown = true;
    }
    public void QPressUp(InputAction.CallbackContext context)
    {
        qDown = false;
    }
    public void EPressDown(InputAction.CallbackContext context)
    {
        eDown = true;
    }
    public void EPressUp(InputAction.CallbackContext context)
    {
        eDown = false;
    }
}
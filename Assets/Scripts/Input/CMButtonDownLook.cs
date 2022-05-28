using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

public class CMButtonDownLook : MonoBehaviour
{
    InputAction mouseAction, buttonPress;
    Vector2 mouseDelta;
    bool buttonDown;
    void Start()
    {
        CinemachineCore.GetInputAxis = GetAxisCustom;
    }
    public void Init(InputAction mouseAction, InputAction buttonPress)
    {
        this.mouseAction = mouseAction;
        this.mouseAction.Enable();
        this.buttonPress = buttonPress;
        this.buttonPress.Enable();

        this.mouseAction.performed += OnMouseMove;
        this.buttonPress.performed += ButtonPressDown;
        this.buttonPress.canceled += ButtonPressUp;
    }
    public float GetAxisCustom(string axisName)
    {
        if (axisName == "MyLook")
        {
            if (buttonDown)
            {
                return mouseDelta.x;
            }
            else
            {
                return 0;
            }
        }
        return 0;
    }
    public void OnMouseMove(InputAction.CallbackContext context)
    {
        mouseDelta = context.ReadValue<Vector2>();
    }
    public void ButtonPressDown(InputAction.CallbackContext context)
    {
        buttonDown = true;
    }
    public void ButtonPressUp(InputAction.CallbackContext context)
    {
        buttonDown = false;
    }
}
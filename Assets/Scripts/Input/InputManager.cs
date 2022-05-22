using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField]
    PlayerHandler playerHandler;
    private CustomInputActions inputScheme;
    void Awake()
    {
        inputScheme = new CustomInputActions();
        playerHandler.Init(inputScheme.User.Move,inputScheme.User.Sprint, inputScheme.User.Mouse, inputScheme.User.Jump, inputScheme.User.Escape, inputScheme.User.MouseLeftClick, inputScheme.User.MouseRightClick, inputScheme.User.MouseMiddleClick);
    }
}

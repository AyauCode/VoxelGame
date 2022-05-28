using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;
    [SerializeField]
    PlayerHandler playerHandler;
    [SerializeField]
    CMButtonDownLook cmLook;
    private CustomInputActions inputScheme;
    public void Awake()
    {
        instance = this;
    }
    public void Init(PlayerHandler playerHandler)
    {
        inputScheme = new CustomInputActions();
        playerHandler.Init(inputScheme.User.Move, inputScheme.User.Sprint, inputScheme.User.Mouse, inputScheme.User.Jump, inputScheme.User.Escape, inputScheme.User.MouseLeftClick, inputScheme.User.MouseRightClick, inputScheme.User.MouseMiddleClick, inputScheme.User.RPress);
        cmLook.Init(inputScheme.User.Mouse, inputScheme.User.MouseMiddleClick);
    }
}

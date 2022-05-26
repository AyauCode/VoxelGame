using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHandler : MonoBehaviour
{
    public GameObject playerObject;
    public CharacterController playerController;
    public Camera playerCamera;
    public Transform groundCheck;

    [SerializeField]
    float moveSpeed, sprintSpeed, maxReach = 5f, mouseSensitivity = 100f, jumpHeight, groundCheckRadius;
    [SerializeField]
    float waterThrowAmt,waterThrowForce;
    [SerializeField]
    GameObject waterBallPrefab;
    [SerializeField]
    LayerMask terrainLayer;

    private InputAction moveAction, sprintAction, mouseAction, escapeAction, jumpAction, mouseLeftClickAction, mouseRightClickAction, mouseMiddleClickAction;
    private Vector2 moveDir, mouseDelta;

    float rotX;
    bool escapeToggle;
    bool isGrounded;
    float currentSpeed;

    Vector3 velocity;
    float yGrav;

    private void Start()
    {
        //Lock cursor to window and set the faux xRotation to be used when using the mouse to look around
        Cursor.lockState = CursorLockMode.Locked;
        rotX = this.transform.eulerAngles.x;
        currentSpeed = moveSpeed;

        yGrav = Physics.gravity.y;
    }
    public void Init(InputAction moveAction, InputAction sprintAction, InputAction mouseAction, InputAction jumpAction, InputAction escapeAction, InputAction mouseLeftClickAction, InputAction mouseRightClickAction, InputAction mouseMiddleClickAction)
    {
        //I know this looks horrible but its simply enabling all these input actions
        this.moveAction = moveAction;
        moveAction.Enable();
        this.sprintAction = sprintAction;
        sprintAction.Enable();
        this.mouseAction = mouseAction;
        mouseAction.Enable();
        this.escapeAction = escapeAction;
        escapeAction.Enable();
        this.jumpAction = jumpAction;
        jumpAction.Enable();
        this.mouseLeftClickAction = mouseLeftClickAction;
        mouseLeftClickAction.Enable();
        this.mouseRightClickAction = mouseLeftClickAction;
        mouseRightClickAction.Enable();
        this.mouseMiddleClickAction = mouseLeftClickAction;
        mouseMiddleClickAction.Enable();

        //Add the specific function to be called when input action is performed
        moveAction.performed += OnMove;
        sprintAction.performed += Sprint;
        sprintAction.canceled += StopSprint;
        mouseAction.performed += OnMouseMove;
        escapeAction.performed += OnEscape;
        jumpAction.performed += Jump;
        mouseLeftClickAction.performed += LeftMouseClick;
        mouseRightClickAction.performed += RightMouseClick;
        mouseMiddleClickAction.performed += MiddleMouseClick;
    }
    public void Update()
    {
        //Cast a sphere around a given position, with radius, checking to see if the player is touching the terrain
        //(The groundCheck transform is located at the bottom of the player model)
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, terrainLayer);
        if (!escapeToggle)
        {
            //If we are touching the ground and moving downwards stop our movement down
            if(isGrounded && velocity.y < 0)
            {
                //NOTE: Velocity is not set to 0 as this can cause the player to float above the ground, this makes sure the player collides with the ground
                velocity.y = -2;
            }

            //mouseDelta is updated whenever the mouse is moved, calculated a new mouseDelta * mouseSensitivity and multiply by Time.deltaTime to make it framerate independent
            float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
            float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

            //If our mouse is moving up or down rotate the camera along its x axis (this will cause it to look up and down)
            rotX -= mouseY;
            //Clamp between -90 and 90 so you dont "break your neck"
            rotX = Mathf.Clamp(rotX, -90f, 90f);

            //Rotate the camera only along its x axis (the player model should not move up and down as this would look odd in game)
            playerCamera.transform.localRotation = Quaternion.Euler(new Vector3(rotX, -8.5f, 0));
            //Rotate the player object around its current up vector by the delta X of the mouse
            playerObject.transform.Rotate(Vector3.up * mouseX);

            //MoveDir is a value set when WASD is pressed, moveDir has values between -1 and 1 based on the key pressed
            float xDir = moveDir.x;
            float zDir = moveDir.y;

            //Calculate the amount to move the player
            //(we want to move forward/backward in the direction theyre facing, and left/right relative to the way they are rotated)
            Vector3 moveAmt = transform.right * xDir + transform.forward * zDir;

            //Unity's player controller takes care of some movement aspects and collisions for us so we dont have to
            //The move function takes its current position and moves it by the given amount (multiplying by Time.deltaTime to make this framerate independent)
            playerController.Move(moveAmt * currentSpeed * Time.deltaTime);

            //Add gravity to the players velocity (as unity's player controllers do not have rigidbodies so we cant use Unity's physics system)
            //(Multiply by Time.deltaTime 
            velocity.y += yGrav * Time.deltaTime;

            //Move the player by gravity
            playerController.Move(velocity * Time.deltaTime);
        }

    }
    public void OnMove(InputAction.CallbackContext context)
    {
        moveDir = context.ReadValue<Vector2>();
    }
    public void OnMouseMove(InputAction.CallbackContext context)
    {
        mouseDelta = context.ReadValue<Vector2>();
    }
    public void OnEscape(InputAction.CallbackContext context)
    {
        escapeToggle = !escapeToggle;
        if (escapeToggle)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
    public void Sprint(InputAction.CallbackContext context)
    {
        currentSpeed = sprintSpeed;

    }
    public void StopSprint(InputAction.CallbackContext context)
    {
        currentSpeed = moveSpeed;
    }
    public void Jump(InputAction.CallbackContext context)
    {
        if (isGrounded)
        {
            //Equation derived from laws of motion
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * yGrav);
            //Set isGrounded = false (as we are now jumping)
            isGrounded = false;
        }
    }
    public void LeftMouseClick(InputAction.CallbackContext context)
    {
        DestroyBlock();
    }
    public void RightMouseClick(InputAction.CallbackContext context)
    {
        PlaceBlock();
    }
    public void MiddleMouseClick(InputAction.CallbackContext context)
    {
        ThrowWater(waterThrowAmt);
    }
    public void DestroyBlock()
    {
        RaycastHit hit;
        //Cast a ray from the player in the direction player is facing, if it hits specificially the terrain continue
        if(Physics.Raycast(new Ray(playerCamera.transform.position,  playerCamera.transform.forward), out hit, maxReach, terrainLayer))
        {
            //Calculate a point inside the block you want to destroy
            //(since the ray hit will be on the outside of the block in order to destroy the right block we must move the hit point just a bit forward)
            Vector3Int hitPos = Vector3Int.FloorToInt(hit.point + playerCamera.transform.forward * .01f);
            //Destroy block at position
            TerrainHandler.instance.DestroyBlock(hitPos);
        }
    }
    public void PlaceBlock()
    {
        RaycastHit hit;
        //Cast a ray from the player in the direction player is facing, if it hits specificially the terrain continue
        if (Physics.Raycast(new Ray(playerCamera.transform.position, playerCamera.transform.forward), out hit, maxReach, terrainLayer))
        {
            //Calculate a point slightly outside the block you want to place on
            //(since the ray hit will be on the outside of the block in order to place in the right we must move the hit point just a bit backwards)
            Vector3Int hitPos = Vector3Int.FloorToInt(hit.point - playerCamera.transform.forward * .01f);
            
            if (!gameObject.GetComponent<BoxCollider>().bounds.Contains(hitPos + new Vector3(0.5f,0.5f,0.5f)))
            {
                //Place block at position
                TerrainHandler.instance.PlaceBlock(hitPos);
            }
        }
    }
    public void ThrowWater(float amt)
    {
        GameObject waterBall = Instantiate(waterBallPrefab);
        waterBall.SetActive(true);
        waterBall.transform.position = playerCamera.transform.position + playerCamera.transform.forward;
        waterBall.GetComponent<WaterBall>().amt = amt;
        waterBall.GetComponent<Rigidbody>().AddForce(playerCamera.transform.forward * waterThrowForce);
    }
}

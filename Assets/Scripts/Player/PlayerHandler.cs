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
    float waterThrowForce, waterThrowAmt;
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
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, terrainLayer);
        if (!escapeToggle)
        {
            if(isGrounded && velocity.y < 0)
            {
                velocity.y = -2;
            }

            float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
            float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

            rotX -= mouseY;
            rotX = Mathf.Clamp(rotX, -90f, 90f);
            playerCamera.transform.localRotation = Quaternion.Euler(new Vector3(rotX, 0, 0));
            playerObject.transform.Rotate(Vector3.up * mouseX);

            float xDir = moveDir.x;
            float zDir = moveDir.y;

            Vector3 moveAmt = transform.right * xDir + transform.forward * zDir;

            playerController.Move(moveAmt * currentSpeed * Time.deltaTime);

            velocity.y += yGrav * Time.deltaTime;

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
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * yGrav);
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
        if(Physics.Raycast(new Ray(playerCamera.transform.position,  playerCamera.transform.forward), out hit, maxReach, terrainLayer))
        {
            Vector3Int hitPos = Vector3Int.FloorToInt(hit.point + playerCamera.transform.forward * .01f);
            TerrainHandler.instance.DestroyBlock(hitPos);
        }
    }
    public void PlaceBlock()
    {
        RaycastHit hit;
        if (Physics.Raycast(new Ray(playerCamera.transform.position, playerCamera.transform.forward), out hit, maxReach, terrainLayer))
        {
            Vector3Int hitPos = Vector3Int.FloorToInt(hit.point - playerCamera.transform.forward * .01f);;
            TerrainHandler.instance.PlaceBlock(hitPos);
        }
    }
    public void ThrowWater(float amt)
    {
        GameObject waterBall = Instantiate(waterBallPrefab);
        waterBall.SetActive(true);
        waterBall.transform.position = playerCamera.transform.position + playerObject.transform.forward * 2;
        waterBall.GetComponent<WaterBall>().amt = amt;
        waterBall.GetComponent<Rigidbody>().AddForce(playerCamera.transform.forward * waterThrowForce);
    }
}

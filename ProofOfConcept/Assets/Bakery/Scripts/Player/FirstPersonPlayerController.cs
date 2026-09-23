using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Basic single-player WASD, mouse look and grounded Space jump.
// Inventory and interaction remain separate components.
[RequireComponent(typeof(CharacterController))]
public class FirstPersonPlayerController : MonoBehaviour
{
    [SerializeField] private Transform playerCamera;
    [Min(0f)][SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float gravity = -20f;
    [Min(0f)][SerializeField] private float jumpHeight = 1.2f;
#if ENABLE_INPUT_SYSTEM
    [Tooltip("Degrees per mouse pixel for the new Input System.")]
    [SerializeField] private float mouseSensitivity = 0.12f;
#else
    [Tooltip("Sensitivity for the legacy Mouse X/Y axes.")]
    [SerializeField] private float legacyMouseSensitivity = 2f;
#endif

    private CharacterController controller;
    private float pitch;
    private int ignoreInputThroughFrame;
    private float verticalSpeed;
    private readonly HashSet<Object> uiOwners = new HashSet<Object>();
    public bool IsUIOpen => uiOwners.Count > 0;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null)
        {
            Debug.LogError("Assign the player's child camera to FirstPersonPlayerController.", this);
            enabled = false;
            return;
        }
        pitch = Mathf.DeltaAngle(0f, playerCamera.localEulerAngles.x);
    }

    private void Start() { RefreshCursor(); }

    public void AcquireUI(Object owner)
    {
        if (owner == null) return;
        uiOwners.Add(owner);
        RefreshCursor();
    }

    public void ReleaseUI(Object owner)
    {
        if (!uiOwners.Remove(owner)) return;
        ignoreInputThroughFrame = Time.frameCount + 1;
        RefreshCursor();
    }

    private void RefreshCursor()
    {
        Cursor.lockState = IsUIOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = IsUIOpen;
    }

    private void Update()
    {
        // Recover if a UI owner was destroyed without releasing its lock.
        if (uiOwners.RemoveWhere(owner => owner == null) > 0) RefreshCursor();
        if (IsUIOpen || !Application.isFocused || Time.timeScale <= 0f || Time.frameCount <= ignoreInputThroughFrame) return;

        // Escape releases the cursor in free play; click the Game view to recapture it.
        if (EscapePressed())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (LeftMousePressed()) RefreshCursor();
            return;
        }

        Vector2 move = ReadMovement();
        Vector2 look = ReadLook();
        transform.Rotate(0f, look.x, 0f);
        pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
        playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
        // Only a fresh Space press while grounded starts a jump.
        // The UI/cursor/pause checks above also block jumping.
        if (controller.isGrounded && JumpPressed() && jumpHeight > 0f && gravity < 0f)
            verticalSpeed = Mathf.Sqrt(-2f * gravity * jumpHeight);
        verticalSpeed += gravity * Time.deltaTime;
        Vector3 velocity = (transform.right * move.x + transform.forward * move.y) * moveSpeed;
        velocity.y = verticalSpeed;
        // Move supplies collision handling; gravity is applied explicitly above.
        CollisionFlags collisions = controller.Move(velocity * Time.deltaTime);
        // Stop upward velocity on a ceiling instead of hanging against it.
        if ((collisions & CollisionFlags.Above) != 0 && verticalSpeed > 0f)
            verticalSpeed = 0f;
    }

    private Vector2 ReadMovement()
    {
        Vector2 move = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return move;
        move.x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
        move.y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
#else
        move.x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
        move.y = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
#endif
        return Vector2.ClampMagnitude(move, 1f);
    }

    private Vector2 ReadLook()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current == null ? Vector2.zero : Mouse.current.delta.ReadValue() * mouseSensitivity;
#else
        return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * legacyMouseSensitivity;
#endif
    }

    private static bool JumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    public static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private static bool LeftMousePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        // On return, click to recapture in free play. Open UIs keep the cursor free.
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}

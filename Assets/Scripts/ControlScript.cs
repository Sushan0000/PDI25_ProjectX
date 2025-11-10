using UnityEngine;
using UnityEngine.InputSystem;   // new system

[RequireComponent(typeof(CharacterController))]
public class ControlScript : MonoBehaviour
{
    [Header("Player Movement")]
    public float playerSpeed = 1.9f;
    public float currentSpeed = 0f;
    public float sprintSpeed = 3.8f;
    public float currentSprintSpeed = 0f;

    [Header("Player Camera")]
    public Transform playerCamera;
    [Header("Player Animator and Gravity")]
    public CharacterController cC;
    public float gravity = -9.81f;
    [Header("Player Jumping and Velocity")]
    public float jumpHeight = 1f;
    public float turnCalmTime = 0.1f;
    float turnCalmVelocity;
    Vector3 velocity;
    public Transform surfaceCheck;
    bool onSurface;
    public float surfaceDistance = 0.4f;
    public LayerMask surfaceMask;
    void Awake()
    {
        if (!cC) cC = GetComponent<CharacterController>();
    }

    void Update()
    {
        onSurface = Physics.CheckSphere(surfaceCheck.position, surfaceDistance, surfaceMask);
        if (onSurface && velocity.y < 0f)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;
        cC.Move(velocity * Time.deltaTime);

        PlayerMove();

        Jump();

        Sprint();
    }

    void PlayerMove()
    {
        if (Keyboard.current == null) return;

        float h = (Keyboard.current.aKey.isPressed ? -1f : 0f) +
                  (Keyboard.current.dKey.isPressed ? 1f : 0f);
        float v = (Keyboard.current.sKey.isPressed ? -1f : 0f) +
                  (Keyboard.current.wKey.isPressed ? 1f : 0f);

        Vector3 direction = new(h, 0f, v);
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        if (direction.sqrMagnitude >= 0.01f)
        {
    float yaw = playerCamera ? playerCamera.eulerAngles.y : transform.eulerAngles.y;
    float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + yaw;
    float smoothedYaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnCalmVelocity, turnCalmTime);
    transform.rotation = Quaternion.Euler(0f, smoothedYaw, 0f);

    Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
    cC.Move(sprintSpeed * Time.deltaTime * moveDir);

            currentSpeed = playerSpeed;
        }
    }

    void Jump()
    {
        if (Keyboard.current.spaceKey.isPressed)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void Sprint()
    {
    if (Keyboard.current == null) return;

    var kb = Keyboard.current;

    // Read movement keys (WASD + Arrows)
    float h = 0f, v = 0f;
    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h -= 1f;
    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
    if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  v -= 1f;
    if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    v += 1f;

    Vector3 input = new(h, 0f, v);
    if (input.sqrMagnitude > 1f) input.Normalize();

    bool hasInput = input.sqrMagnitude > 0f;
    bool sprinting = kb.leftShiftKey.isPressed && hasInput;
    if (!sprinting) return;

    // Camera-relative direction
    float yaw = playerCamera ? playerCamera.eulerAngles.y : transform.eulerAngles.y;
    float targetAngle = Mathf.Atan2(input.x, input.z) * Mathf.Rad2Deg + yaw;
    float smoothedYaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnCalmVelocity, turnCalmTime);
    transform.rotation = Quaternion.Euler(0f, smoothedYaw, 0f);

    Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

    // Move
    cC.Move(moveDir * sprintSpeed * Time.deltaTime);
    currentSpeed = sprintSpeed;

    }

}

using UnityEngine;
using UnityEngine.InputSystem; // New Input System

[RequireComponent(typeof(CharacterController))]
public class ControlScript : MonoBehaviour
{
    [Header("Movement")]
    [Min(0f)]
    [SerializeField]
    private float walkSpeed = 2.5f;

    [Min(0f)]
    [SerializeField]
    private float sprintSpeed = 5.0f;

    [Min(0f)]
    [SerializeField]
    private float accel = 12f;

    [Min(0f)]
    [SerializeField]
    private float decel = 14f;

    [Range(0f, 1f)]
    [SerializeField]
    private float airControl = 0.35f;

    [Header("Turning")]
    [Min(0f)]
    [SerializeField]
    private float turnSmoothTime = 0.08f;

    [Header("Jump & Gravity")]
    [SerializeField]
    private float gravity = -9.81f;

    [SerializeField]
    private float jumpHeight = 1.9f;

    [SerializeField]
    private float coyoteTime = 0.1f;

    [SerializeField]
    private float jumpBuffer = 0.12f;

    [SerializeField]
    private float jumpCooldown = 0.1f;

    [Header("References")]
    [SerializeField]
    private Transform playerCamera; // camera / Cinemachine target

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private Transform groundProbe;

    [SerializeField]
    private float probeRadius = 0.25f;

    [SerializeField]
    private LayerMask groundMask = ~0;

    [Header("Cursor Lock")]
    [SerializeField]
    private bool lockCursorOnStart = true;

    [SerializeField]
    private Key cursorToggleKey = Key.Escape;

    // runtime
    private CharacterController characterController;
    private Vector3 velocity;
    private float horizontalSpeed;
    private float yawVelocity; // for SmoothDampAngle

    private float coyoteTimer;
    private float bufferTimer;
    private float cooldownTimer;

    private bool isGrounded;
    private bool cursorLocked;

    // animator hashes (names must match Animator parameters)
    private static readonly int MoveXId = Animator.StringToHash("moveX");
    private static readonly int MoveYId = Animator.StringToHash("moveY");
    private static readonly int SprintId = Animator.StringToHash("Sprint");
    private static readonly int JumpId = Animator.StringToHash("Jump");
    private static readonly int CrouchId = Animator.StringToHash("Crouch");

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (!characterController)
        {
            Debug.LogError($"{nameof(ControlScript)} requires a CharacterController on {name}.");
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        if (lockCursorOnStart)
        {
            LockCursor();
        }

        if (!playerCamera && !Camera.main)
        {
            Debug.LogWarning(
                $"{nameof(ControlScript)} on {name} has no playerCamera and no Camera.main; using own transform as reference."
            );
        }

        if (!groundProbe)
        {
            Debug.LogWarning(
                $"{nameof(ControlScript)} on {name} has no Ground Probe assigned; using CharacterController.isGrounded only."
            );
        }
    }

    private void Update()
    {
        HandleCursorLock();

        var keyboard = Keyboard.current;
        if (keyboard == null || characterController == null)
            return;

        // ----- GROUND CHECK -----
        isGrounded = IsGrounded();

        // ----- INPUT (WASD + arrows) -----
        float h = 0f;
        float v = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            h -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            h += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            v -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            v += 1f;

        Vector2 rawInput = new Vector2(h, v);
        bool hasInput = rawInput.sqrMagnitude >= 0.01f;

        bool wantsSprint = keyboard.leftShiftKey.isPressed && hasInput && isGrounded;
        bool wantsCrouch = keyboard.cKey.isPressed && isGrounded;

        // ----- CAMERA-RELATIVE MOVE + ROTATION -----
        Transform cam = playerCamera
            ? playerCamera
            : (Camera.main ? Camera.main.transform : transform);

        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // movement direction in world space, relative to camera
        Vector3 moveDir = camForward * v + camRight * h;
        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        // rotate character toward movement direction (handles diagonals)
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            float targetYaw = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            float smoothedYaw = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetYaw,
                ref yawVelocity,
                Mathf.Max(0.001f, turnSmoothTime)
            );
            transform.rotation = Quaternion.Euler(0f, smoothedYaw, 0f);
        }

        // ----- SPEED & ACCEL/DECEL -----
        float targetSpeed = hasInput ? (wantsSprint ? sprintSpeed : walkSpeed) : 0f;

        if (wantsCrouch)
        {
            // simple crouch: slow movement
            targetSpeed = Mathf.Min(targetSpeed, 1.5f);
            wantsSprint = false;
        }

        float accelRate = isGrounded ? accel : accel * airControl;
        float decelRate = isGrounded ? decel : decel * airControl;
        float rate = targetSpeed > horizontalSpeed ? accelRate : decelRate;

        horizontalSpeed = Mathf.MoveTowards(horizontalSpeed, targetSpeed, rate * Time.deltaTime);

        // ----- JUMP TIMING (coyote + buffer + cooldown) -----
        if (isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer = Mathf.Max(0f, coyoteTimer - Time.deltaTime);

        if (keyboard.spaceKey.wasPressedThisFrame)
            bufferTimer = jumpBuffer;
        else
            bufferTimer = Mathf.Max(0f, bufferTimer - Time.deltaTime);

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (bufferTimer > 0f && coyoteTimer > 0f && cooldownTimer <= 0f && !wantsCrouch)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            bufferTimer = 0f;
            coyoteTimer = 0f;
            cooldownTimer = jumpCooldown;

            if (animator)
            {
                animator.ResetTrigger(JumpId);
                animator.SetTrigger(JumpId);
            }
        }

        // ----- GRAVITY -----
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f; // small downward push to keep grounded

        velocity.y += gravity * Time.deltaTime;

        // ----- MOVE CHARACTER -----
        Vector3 horizontalVelocity = moveDir * horizontalSpeed;
        characterController.Move((horizontalVelocity + velocity) * Time.deltaTime);

        // ----- ANIMATOR (locomotion + sprint + crouch) -----
        if (animator)
        {
            // normalize input for blend tree so diagonals stay length 1
            Vector2 animInput = rawInput;
            if (animInput.sqrMagnitude > 1f)
                animInput.Normalize();

            const float smoothTime = 0.1f;

            float currentX = animator.GetFloat(MoveXId);
            float currentY = animator.GetFloat(MoveYId);

            float targetX = animInput.x;
            float targetY = animInput.y;

            animator.SetFloat(MoveXId, Mathf.Lerp(currentX, targetX, Time.deltaTime / smoothTime));
            animator.SetFloat(MoveYId, Mathf.Lerp(currentY, targetY, Time.deltaTime / smoothTime));

            bool sprinting = wantsSprint && hasInput;
            animator.SetBool(SprintId, sprinting);
            animator.SetBool(CrouchId, wantsCrouch);
        }
    }

    private bool IsGrounded()
    {
        if (characterController != null && characterController.isGrounded)
            return true;

        if (!groundProbe)
            return false;

        return Physics.CheckSphere(
            groundProbe.position,
            probeRadius,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

    // ----- CURSOR LOCK -----
    private void HandleCursorLock()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard[cursorToggleKey].wasPressedThisFrame)
        {
            if (cursorLocked)
                UnlockCursor();
            else
                LockCursor();
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorLocked = true;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorLocked = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!groundProbe)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundProbe.position, probeRadius);
    }
#endif
}

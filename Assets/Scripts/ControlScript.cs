using UnityEngine;
using UnityEngine.InputSystem;

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

    [Min(0f)]
    [SerializeField]
    private float aimTurnSmoothTime = 0.2f;

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
    private Transform playerCamera; // optional, will fall back to Camera.main

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private Transform groundProbe;

    [SerializeField]
    private float probeRadius = 0.25f;

    [SerializeField]
    private LayerMask groundMask = ~0;

    [SerializeField]
    private ThirdPersonCameraController aimController;

    [Header("Cursor Lock")]
    [SerializeField]
    private bool lockCursorOnStart = true;

    [SerializeField]
    private Key cursorToggleKey = Key.Escape;

    private CharacterController characterController;
    private Vector3 velocity;
    private float horizontalSpeed;
    private float yawVelocity;
    private float coyoteTimer;
    private float bufferTimer;
    private float cooldownTimer;
    private bool isGrounded;
    private bool cursorLocked;

    // Animator hashes
    private static readonly int MoveXId = Animator.StringToHash("moveX");
    private static readonly int MoveYId = Animator.StringToHash("moveY");
    private static readonly int SprintId = Animator.StringToHash("Sprint");
    private static readonly int JumpId = Animator.StringToHash("Jump");
    private static readonly int CrouchId = Animator.StringToHash("Crouch");

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (!characterController)
            Debug.LogError($"{nameof(ControlScript)} requires a CharacterController on {name}.");

        if (!aimController)
            aimController = Object.FindFirstObjectByType<ThirdPersonCameraController>();
        if (animator != null)
            animator.applyRootMotion = false;

        if (lockCursorOnStart)
            LockCursor();

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

        // ----- Ground check -----
        isGrounded = IsGrounded();

        // ----- Input (WASD + arrows) -----
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

        // Aim state: single source of truth from camera controller
        bool isAiming = aimController != null && aimController.IsAiming;

        // ----- Active camera -----
        Transform cam;
        Camera mainCam = Camera.main;
        if (mainCam != null)
            cam = mainCam.transform;
        else if (playerCamera != null)
            cam = playerCamera;
        else
            cam = transform;

        // ----- Camera-relative axes -----
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * v + camRight * h;
        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        // Direction to face
        Vector3 lookDir = moveDir;

        // While aiming and not moving, face straight along camera forward
        if (isAiming && !hasInput)
            lookDir = camForward;

        // ----- Rotate -----
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            float targetYaw = Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg;
            float smoothTime = isAiming ? aimTurnSmoothTime : turnSmoothTime;

            float smoothedYaw = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetYaw,
                ref yawVelocity,
                Mathf.Max(0.001f, smoothTime)
            );

            transform.rotation = Quaternion.Euler(0f, smoothedYaw, 0f);
        }

        // ----- Speed & accel/decel -----
        float targetSpeed = hasInput ? (wantsSprint ? sprintSpeed : walkSpeed) : 0f;

        if (wantsCrouch)
        {
            targetSpeed = Mathf.Min(targetSpeed, 1.5f);
            wantsSprint = false;
        }

        float accelRate = isGrounded ? accel : accel * airControl;
        float decelRate = isGrounded ? decel : decel * airControl;
        float rate = targetSpeed > horizontalSpeed ? accelRate : decelRate;

        horizontalSpeed = Mathf.MoveTowards(horizontalSpeed, targetSpeed, rate * Time.deltaTime);

        // ----- Jump timing (coyote + buffer + cooldown) -----
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

        // ----- Gravity -----
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;

        // ----- Move Character -----
        Vector3 horizontalVelocity = moveDir * horizontalSpeed;
        characterController.Move((horizontalVelocity + velocity) * Time.deltaTime);

        // ----- Animator -----
        if (animator)
        {
            Vector2 animInput = rawInput;
            if (animInput.sqrMagnitude > 1f)
                animInput.Normalize();

            const float smoothTime = 0.1f;
            float currentX = animator.GetFloat(MoveXId);
            float currentY = animator.GetFloat(MoveYId);

            animator.SetFloat(
                MoveXId,
                Mathf.Lerp(currentX, animInput.x, Time.deltaTime / smoothTime)
            );
            animator.SetFloat(
                MoveYId,
                Mathf.Lerp(currentY, animInput.y, Time.deltaTime / smoothTime)
            );

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
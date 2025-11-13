using UnityEngine;
using UnityEngine.InputSystem; // new input

[RequireComponent(typeof(CharacterController))]
public class ControlScript : MonoBehaviour
{
    [Header("Movement")]
    [Min(0f)]
    public float walkSpeed = 2.5f;

    [Min(0f)]
    public float sprintSpeed = 5.0f;

    [Min(0f)]
    public float accel = 12f;

    [Min(0f)]
    public float decel = 14f;

    [Range(0f, 1f)]
    public float airControl = 0.35f;

    [Header("Turning")]
    [Min(0f)]
    public float turnSmoothTime = 0.08f;

    [Header("Jump & Gravity")]
    public float gravity = -9.81f;
    public float jumpHeight = 1.9f;
    public float coyoteTime = 0.1f;
    public float jumpBuffer = 0.12f;
    public float jumpCooldown = 0.1f;

    [Header("References")]
    public Transform playerCamera; // camera / Cinemachine target
    public Animator animator;
    public Transform groundProbe;
    public float probeRadius = 0.25f;
    public LayerMask groundMask = ~0;

    [Header("Cursor Lock")]
    public bool lockCursorOnStart = true;
    public Key cursorToggleKey = Key.Escape;

    // runtime
    CharacterController cc;
    Vector3 vel;
    float speedNow;
    float yawVel; // for SmoothDampAngle
    float coyoteTimer,
        bufferTimer,
        cooldownTimer;
    bool grounded;
    bool cursorLocked;

    // animator hashes (names must match Animator parameters)
    static readonly int MoveXId = Animator.StringToHash("moveX");
    static readonly int MoveYId = Animator.StringToHash("moveY");
    static readonly int SprintId = Animator.StringToHash("Sprint");
    static readonly int JumpId = Animator.StringToHash("Jump");
    static readonly int CrouchId = Animator.StringToHash("Crouch");

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (animator)
            animator.applyRootMotion = false;
        if (lockCursorOnStart)
            LockCursor();
    }

    void Update()
    {
        HandleCursorLock();
        if (Keyboard.current == null)
            return;

        var kb = Keyboard.current;

        // ----- GROUND CHECK -----
        grounded = IsGrounded();

        // ----- INPUT (WASD + arrows) -----
        float h = 0f,
            v = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
            h -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
            h += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
            v -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
            v += 1f;

        Vector2 rawInput = new Vector2(h, v);
        bool hasInput = rawInput.sqrMagnitude >= 0.01f;

        bool wantsSprint = kb.leftShiftKey.isPressed && hasInput && grounded;
        bool wantsCrouch = kb.cKey.isPressed && grounded;

        // ----- CAMERA-RELATIVE MOVE + ROTATION -----
        Transform cam = playerCamera
            ? playerCamera
            : (Camera.main ? Camera.main.transform : transform);

        Vector3 camFwd = cam.forward;
        Vector3 camRight = cam.right;

        camFwd.y = 0f;
        camRight.y = 0f;
        camFwd.Normalize();
        camRight.Normalize();

        // movement direction in world space, relative to camera
        Vector3 moveDir = camFwd * v + camRight * h;
        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        // rotate character toward movement direction (handles diagonals)
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            float targetYaw = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            float smoothedYaw = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetYaw,
                ref yawVel,
                Mathf.Max(0.001f, turnSmoothTime)
            );
            transform.rotation = Quaternion.Euler(0f, smoothedYaw, 0f);
        }

        // ----- SPEED & ACCEL/DECEL -----
        float targetSpeed = hasInput ? (wantsSprint ? sprintSpeed : walkSpeed) : 0f;
        if (wantsCrouch)
        {
            targetSpeed = 1.5f; // crouch speed
            wantsSprint = false;
        }

        float a = grounded ? accel : Mathf.Lerp(accel, accel * airControl, 1f);
        float d = grounded ? decel : Mathf.Lerp(decel, decel * airControl, 1f);
        float rate = targetSpeed > speedNow ? a : d;
        speedNow = Mathf.MoveTowards(speedNow, targetSpeed, rate * Time.deltaTime);

        // ----- JUMP TIMING (coyote + buffer + cooldown) -----
        if (grounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        if (kb.spaceKey.wasPressedThisFrame)
            bufferTimer = jumpBuffer;
        else
            bufferTimer -= Time.deltaTime;

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (bufferTimer > 0f && coyoteTimer > 0f && cooldownTimer <= 0f && !wantsCrouch)
        {
            vel.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
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
        if (grounded && vel.y < 0f)
            vel.y = -2f;
        vel.y += gravity * Time.deltaTime;

        // ----- MOVE CHARACTER -----
        Vector3 horizontal = moveDir * speedNow;
        cc.Move((horizontal + vel) * Time.deltaTime);

        // ----- ANIMATOR (locomotion + sprint + crouch) -----
        if (animator)
        {
            // normalize input for blend tree so diagonals stay length 1
            Vector2 animInput = rawInput;
            if (animInput.sqrMagnitude > 1f)
                animInput.Normalize();

            const float smooth = 0.1f;
            float curX = animator.GetFloat(MoveXId);
            float curY = animator.GetFloat(MoveYId);

            float targetX = animInput.x;
            float targetY = animInput.y;

            animator.SetFloat(MoveXId, Mathf.Lerp(curX, targetX, Time.deltaTime / smooth));
            animator.SetFloat(MoveYId, Mathf.Lerp(curY, targetY, Time.deltaTime / smooth));

            bool sprinting = wantsSprint && hasInput;
            animator.SetBool(SprintId, sprinting);
            animator.SetBool(CrouchId, wantsCrouch);
        }
    }

    bool IsGrounded()
    {
        if (cc.isGrounded)
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
    void HandleCursorLock()
    {
        var kb = Keyboard.current;
        if (kb == null)
            return;

        if (kb[cursorToggleKey].wasPressedThisFrame)
        {
            if (cursorLocked)
                UnlockCursor();
            else
                LockCursor();
        }
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorLocked = true;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorLocked = false;
    }
}

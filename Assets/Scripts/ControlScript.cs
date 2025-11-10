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
    public float accel = 12f; // to target speed

    [Min(0f)]
    public float decel = 14f; // to zero

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
    public Transform playerCamera;
    public Animator animator;
    public Transform groundProbe; // optional; if null uses capsule foot
    public float probeRadius = 0.25f;
    public LayerMask groundMask = ~0;

    // runtime
    CharacterController cc;
    Vector3 vel; // y used for vertical; horizontal is computed
    float speedNow; // smoothed horizontal scalar
    float yawVel; // for SmoothDampAngle
    float coyoteTimer,
        bufferTimer,
        cooldownTimer;
    bool grounded;

    // animator param names
    static readonly int IdleId = Animator.StringToHash("Idle");
    static readonly int WalkId = Animator.StringToHash("Walk");
    static readonly int SprintId = Animator.StringToHash("Sprint");
    static readonly int JumpId = Animator.StringToHash("Jump");

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (animator)
            animator.applyRootMotion = false;
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        // ground check
        grounded = IsGrounded();

        // input (WASD + arrows)
        var kb = Keyboard.current;
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

        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f)
            input.Normalize();
        bool hasInput = input.sqrMagnitude >= 0.01f;

        // sprint gate: shift + input + grounded
        bool wantsSprint = kb.leftShiftKey.isPressed && hasInput && grounded;

        // camera-relative move dir
        float camYaw = playerCamera ? playerCamera.eulerAngles.y : transform.eulerAngles.y;
        Vector3 moveDir = hasInput
            ? Quaternion.Euler(0f, Mathf.Atan2(input.x, input.z) * Mathf.Rad2Deg + camYaw, 0f)
                * Vector3.forward
            : Vector3.zero;

        // rotate toward move
        if (hasInput)
        {
            float targetYaw = Mathf.Atan2(input.x, input.z) * Mathf.Rad2Deg + camYaw;
            float smoothed = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetYaw,
                ref yawVel,
                turnSmoothTime
            );
            transform.rotation = Quaternion.Euler(0f, smoothed, 0f);
        }

        // choose target speed
        float targetSpeed = hasInput ? (wantsSprint ? sprintSpeed : walkSpeed) : 0f;

        // speed smoothing, reduced in air
        float a = grounded ? accel : Mathf.Lerp(accel, accel * airControl, 1f);
        float d = grounded ? decel : Mathf.Lerp(decel, decel * airControl, 1f);
        float rate = targetSpeed > speedNow ? a : d;
        speedNow = Mathf.MoveTowards(speedNow, targetSpeed, rate * Time.deltaTime);

        // jump buffer + coyote + cooldown
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

        if (bufferTimer > 0f && coyoteTimer > 0f && cooldownTimer <= 0f)
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

        // gravity and stick
        if (grounded && vel.y < 0f)
            vel.y = -2f;
        vel.y += gravity * Time.deltaTime;

        // single Move
        Vector3 horizontal = moveDir * speedNow;
        cc.Move((horizontal + vel) * Time.deltaTime);

        // animator once
        if (animator)
        {
            bool walking = hasInput && !wantsSprint && grounded && speedNow > 0.01f;
            bool idle = !hasInput && grounded && speedNow <= 0.01f;

            animator.SetBool(SprintId, wantsSprint && grounded);
            animator.SetBool(WalkId, walking);
            animator.SetBool(IdleId, idle);
            // do not reset Jump here
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
}


// using UnityEngine;
// using UnityEngine.InputSystem; // new system

// [RequireComponent(typeof(CharacterController))]
// public class ControlScript : MonoBehaviour
// {
//     [Header("Player Movement")]
//     public float playerSpeed = 2f;
//     public float currentSpeed = 0f;
//     public float sprintSpeed = 3f;
//     public float currentSprintSpeed = 0f;

//     [Header("Player Camera")]
//     public Transform playerCamera;

//     [Header("Player Animator and Gravity")]
//     public CharacterController cC;
//     public float gravity = -9.81f;
//     public Animator animator;

//     [Header("Player Jumping and Velocity")]
//     public float jumpHeight = 1.9f;
//     public float turnCalmTime = 0.1f;
//     float turnCalmVelocity;
//     Vector3 velocity;
//     public Transform surfaceCheck;
//     bool onSurface;
//     public float surfaceDistance = 0.4f;
//     public LayerMask surfaceMask;

//     void Awake()
//     {
//         if (!cC)
//             cC = GetComponent<CharacterController>();
//         animator.applyRootMotion = false;
//     }

//     void Update()
//     {
//         onSurface = Physics.CheckSphere(surfaceCheck.position, surfaceDistance, surfaceMask);
//         if (onSurface && velocity.y < 0f)
//         {
//             velocity.y = -2f;
//         }
//         velocity.y += gravity * Time.deltaTime;
//         cC.Move(velocity * Time.deltaTime);

//         PlayerMove();

//         Jump();

//         Sprint();
//     }

//     void PlayerMove()
//     {
//         if (Keyboard.current == null || cC == null)
//             return;
//         var kb = Keyboard.current;

//         // WASD + Arrows
//         float h = 0f,
//             v = 0f;
//         if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
//             h -= 1f;
//         if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
//             h += 1f;
//         if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
//             v -= 1f;
//         if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
//             v += 1f;

//         Vector3 input = new(h, 0f, v);
//         if (input.sqrMagnitude > 1f)
//             input.Normalize();

//         bool hasInput = input.sqrMagnitude >= 0.01f;

//         // Animator state (no jump trigger here)
//         if (animator)
//         {
//             animator.SetBool("Walk", hasInput && onSurface);
//             animator.SetBool("Idle", !hasInput && onSurface);
//             animator.SetBool("Sprint", false);
//             animator.SetBool("Aim", false);
//         }

//         // no horizontal move when no input; still allow vertical handled elsewhere
//         if (!hasInput)
//             return;

//         // Camera-relative yaw
//         float yaw = playerCamera ? playerCamera.eulerAngles.y : transform.eulerAngles.y;

//         // Face move direction smoothly
//         float targetAngle = Mathf.Atan2(input.x, input.z) * Mathf.Rad2Deg + yaw;
//         float smoothYaw = Mathf.SmoothDampAngle(
//             transform.eulerAngles.y,
//             targetAngle,
//             ref turnCalmVelocity,
//             turnCalmTime
//         );
//         transform.rotation = Quaternion.Euler(0f, smoothYaw, 0f);

//         // Move in camera-forward direction
//         Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
//         cC.Move(playerSpeed * Time.deltaTime * moveDir);

//         currentSpeed = playerSpeed;
//         Jump();
//     }

//     void Jump()
//     {
//         if (Keyboard.current == null)
//             return;

//         // ground stick
//         if (onSurface && velocity.y < 0f)
//             velocity.y = -2f;

//         // one–shot jump
//         if (onSurface && Keyboard.current.spaceKey.wasPressedThisFrame)
//         {
//             velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

//             if (animator)
//             {
//                 // do NOT reset in an else-branch every frame
//                 animator.ResetTrigger("Jump"); // optional safety
//                 animator.SetTrigger("Jump");
//             }
//         }
//     }

//     void Sprint()
//     {
//         if (Keyboard.current == null || cC == null)
//             return;
//         var kb = Keyboard.current;

//         // sprint key + forward key + grounded (mirrors the picture)
//         bool sprinting = kb.leftShiftKey.isPressed && onSurface;

//         // read movement like the picture (axes-equivalent via keys)
//         float h = 0f,
//             v = 0f;
//         if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
//             h -= 1f;
//         if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
//             h += 1f;
//         if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
//             v -= 1f;
//         if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
//             v += 1f;

//         Vector3 direction = new Vector3(h, 0f, v);
//         if (direction.sqrMagnitude > 1f)
//             direction.Normalize();

//         bool hasDir = direction.magnitude >= 0.1f;
//         bool grounded = onSurface;

//         if (sprinting && hasDir)
//         {
//             if (animator)
//             {
//                 animator.SetBool("Sprint", true);
//                 animator.SetBool("Idle", false);
//                 animator.SetBool("Walk", false);
//                 animator.SetBool("Aim", false);
//             }

//             float yaw = playerCamera ? playerCamera.eulerAngles.y : transform.eulerAngles.y;
//             float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + yaw;
//             float angle = Mathf.SmoothDampAngle(
//                 transform.eulerAngles.y,
//                 targetAngle,
//                 ref turnCalmVelocity,
//                 turnCalmTime
//             );
//             transform.rotation = Quaternion.Euler(0f, angle, 0f);

//             Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
//             cC.Move(moveDirection.normalized * sprintSpeed * Time.deltaTime);
//             currentSpeed = sprintSpeed;
//         }
//         else
//         {
//             if (animator)
//             {
//                 animator.SetBool("Sprint", sprinting);
//                 animator.SetBool("Walk", hasDir && grounded && !sprinting);
//                 animator.SetBool("Idle", !hasDir && grounded);
//                 animator.ResetTrigger("Jump"); // to reset jump if needed
//             }
//         }
//     }
// }

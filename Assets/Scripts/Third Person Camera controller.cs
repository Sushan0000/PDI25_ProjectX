using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField]
    private GameObject thirdPersonCamera;

    [SerializeField]
    private GameObject aimCamera;

    [Header("UI (optional)")]
    [SerializeField]
    private GameObject thirdPersonCanvas;

    [SerializeField]
    private GameObject aimCanvas;

    [Header("Animation")]
    [SerializeField]
    private Animator animator;
    private static readonly int AimHash = Animator.StringToHash("Aim");

    private bool isAiming;
    public bool IsAiming => isAiming;

    private void Awake()
    {
        // Set the initial state on startup.
        // The simplified SetAimState no longer needs "forceRefresh".
        SetAimState(false);
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        bool aimHeld = Mouse.current.rightButton.isPressed;

        // This is already very efficient. No changes needed.
        if (aimHeld != isAiming)
            SetAimState(aimHeld);
    }

    /// <summary>
    /// Sets the aim state for cameras, UI, and animation.
    /// This is now simplified and just sets the state directly.
    /// </summary>
    private void SetAimState(bool aiming)
    {
        isAiming = aiming;

        // --- Set State Directly ---
        // Calling SetActive() is cheap. We don't need to check the
        // current state first. This is simpler and more robust.

        if (aimCamera)
            aimCamera.SetActive(aiming);

        if (thirdPersonCamera)
            thirdPersonCamera.SetActive(!aiming);

        // --- UI ---
        if (aimCanvas)
            aimCanvas.SetActive(aiming);

        if (thirdPersonCanvas)
            thirdPersonCanvas.SetActive(!aiming);

        // --- Animator ---
        if (animator)
            animator.SetBool(AimHash, aiming);
    }
} // using UnityEngine;
// using UnityEngine.InputSystem; // new input system

// public class ThirdPersonCameraController : MonoBehaviour
// {
//     [Header("Camera")]
//     [SerializeField]
//     private GameObject thirdPersonCamera;

//     [SerializeField]
//     private GameObject aimCamera;

//     [Header("UI (optional)")]
//     [SerializeField]
//     private GameObject thirdPersonCanvas;

//     [SerializeField]
//     private GameObject aimCanvas;

//     [Header("Animation")]
//     [SerializeField]
//     private Animator animator;

//     private static readonly int AimHash = Animator.StringToHash("Aim");

//     private bool isAiming;
//     public bool IsAiming => isAiming;

//     private void Awake()
//     {
//         SetAimState(false, true);
//     }

//     private void Update()
//     {
//         if (Mouse.current == null)
//             return;

//         bool aimHeld = Mouse.current.rightButton.isPressed;

//         if (aimHeld != isAiming)
//             SetAimState(aimHeld, false);
//     }

//     private void SetAimState(bool aiming, bool forceRefresh)
//     {
//         isAiming = aiming;

//         // cameras
//         if (aimCamera && (forceRefresh || aimCamera.activeSelf != aiming))
//             aimCamera.SetActive(aiming);

//         if (thirdPersonCamera && (forceRefresh || thirdPersonCamera.activeSelf == aiming))
//             thirdPersonCamera.SetActive(!aiming);

//         // UI
//         if (aimCanvas && (forceRefresh || aimCanvas.activeSelf != aiming))
//             aimCanvas.SetActive(aiming);

//         if (thirdPersonCanvas && (forceRefresh || thirdPersonCanvas.activeSelf == aiming))
//             thirdPersonCanvas.SetActive(!aiming);

//         // animator
//         if (animator)
//             animator.SetBool(AimHash, aiming);
//     }
// }

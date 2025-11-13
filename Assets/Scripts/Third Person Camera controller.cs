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

    [Header("Animation (optional)")]
    [SerializeField]
    private Animator animator;
    private static readonly int AimHash = Animator.StringToHash("Aim");

    private bool isAiming;
    public bool IsAiming => isAiming;

    private void Awake()
    {
        ValidateReferences();
        SetAimState(false);
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        bool aimHeld = mouse.rightButton.isPressed;
        if (aimHeld != isAiming)
        {
            SetAimState(aimHeld);
        }
    }

    private void SetAimState(bool aiming)
    {
        isAiming = aiming;

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

    private void ValidateReferences()
    {
        if (!thirdPersonCamera && !aimCamera)
        {
            Debug.LogWarning(
                $"{nameof(ThirdPersonCameraController)} on {name} has no cameras assigned."
            );
        }
    }
}

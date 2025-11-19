using Unity.Cinemachine; // Cinemachine 3 namespace
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("Cameras (Cinemachine Camera GameObjects)")]
    [SerializeField]
    private GameObject thirdPersonCamera; // Player Follow Camera

    [SerializeField]
    private GameObject aimCamera; // Player Aim Camera

    [Header("Cinemachine (optional but recommended)")]
    [Tooltip("PanTilt on the third-person camera (Player Follow Camera).")]
    [SerializeField]
    private CinemachinePanTilt thirdPersonPanTilt;

    [Tooltip("PanTilt on the aim camera (Player Aim Camera).")]
    [SerializeField]
    private CinemachinePanTilt aimPanTilt;

    [Header("UI (optional)")]
    [SerializeField]
    private GameObject thirdPersonCanvas;

    [SerializeField]
    private GameObject aimCanvas;

    [Header("Animation (optional)")]
    [SerializeField]
    private Animator animator;
    private static readonly int AimHash = Animator.StringToHash("Aim");

    [Header("Input (optional)")]
    [Tooltip("Input Action for aiming. If left empty, script falls back to Mouse Right Button.")]
    [SerializeField]
    private InputActionReference aimAction;

    [Tooltip("If true, aim only works when cursor is locked (in gameplay, not in menus).")]
    [SerializeField]
    private bool requireCursorLocked = true;

    private bool isAiming;
    public bool IsAiming => isAiming;

    private void Awake()
    {
        // Auto-hook Cinemachine components if not set in Inspector
        if (thirdPersonCamera && !thirdPersonPanTilt)
            thirdPersonPanTilt = thirdPersonCamera.GetComponent<CinemachinePanTilt>();

        if (aimCamera && !aimPanTilt)
            aimPanTilt = aimCamera.GetComponent<CinemachinePanTilt>();

        if (aimAction && aimAction.action != null)
            aimAction.action.Enable();

        ValidateReferences();
        SetAimState(false, true); // start in hip-fire mode
    }

    private void OnDestroy()
    {
        if (aimAction && aimAction.action != null)
            aimAction.action.Disable();
    }

    private void Update()
    {
        bool aimHeld = ReadAimInput();
        if (aimHeld != isAiming)
        {
            SetAimState(aimHeld, false);
        }
    }

    private bool ReadAimInput()
    {
        bool pressed = false;

        // Prefer Input Action if provided
        if (aimAction && aimAction.action != null && aimAction.action.enabled)
        {
            pressed = aimAction.action.IsPressed();
        }
        else
        {
            // Fallback: mouse right button
            var mouse = Mouse.current;
            if (mouse != null)
                pressed = mouse.rightButton.isPressed;
        }

        if (requireCursorLocked && Cursor.lockState != CursorLockMode.Locked)
            pressed = false;

        return pressed;
    }

    /// <summary>
    /// Central place that changes aim state, toggles cameras/UI, and keeps vcams in sync.
    /// </summary>
    private void SetAimState(bool aiming, bool forceInstant)
    {
        isAiming = aiming;

        // 1) Keep Cinemachine camera orientation in sync to avoid direction snap
        if (thirdPersonPanTilt && aimPanTilt)
        {
            if (aiming)
            {
                // Going into aim: copy Follow camera orientation into Aim camera
                aimPanTilt.PanAxis.Value = thirdPersonPanTilt.PanAxis.Value;
                aimPanTilt.TiltAxis.Value = thirdPersonPanTilt.TiltAxis.Value;
            }
            else
            {
                // Leaving aim: copy Aim camera orientation back to Follow camera
                thirdPersonPanTilt.PanAxis.Value = aimPanTilt.PanAxis.Value;
                thirdPersonPanTilt.TiltAxis.Value = aimPanTilt.TiltAxis.Value;
            }
        }

        // 2) Turn cameras on/off
        if (aimCamera)
            aimCamera.SetActive(aiming);

        if (thirdPersonCamera)
            thirdPersonCamera.SetActive(!aiming);

        // 3) UI crosshairs etc.
        if (aimCanvas)
            aimCanvas.SetActive(aiming);

        if (thirdPersonCanvas)
            thirdPersonCanvas.SetActive(!aiming);

        // 4) Animator "Aim" bool – Rifle script reads this
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

        if (aimCamera && !aimPanTilt)
        {
            Debug.LogWarning(
                $"{nameof(ThirdPersonCameraController)} on {name} has no CinemachinePanTilt on Aim Camera. Orientation sync disabled."
            );
        }

        if (thirdPersonCamera && !thirdPersonPanTilt)
        {
            Debug.LogWarning(
                $"{nameof(ThirdPersonCameraController)} on {name} has no CinemachinePanTilt on Third Person Camera. Orientation sync disabled."
            );
        }
    }
}
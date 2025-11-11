using UnityEngine;
using UnityEngine.InputSystem; // new input system

public class ThirdPersonCameracontroller : MonoBehaviour
{
    [Header("Camera Settings")]
    public GameObject AimCamera;
    public GameObject ThirdPersonCamera;

    void Update()
    {
        if (Mouse.current == null)
            return; // no mouse available
        bool aiming = Mouse.current.rightButton.isPressed;

        AimCamera.SetActive(aiming);
        ThirdPersonCamera.SetActive(!aiming);
    }
}

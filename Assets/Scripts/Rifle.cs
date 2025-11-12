using UnityEngine;
using UnityEngine.InputSystem; // new input system

public class Rifle : MonoBehaviour
{
    [Header("Rifle")]
    public Camera cam;
    public float giveDamage = 10f;
    public float shootingRange = 100f;

    public float fireRate = 10f; // bullets per second
    float nextFireTime = 0f;
    bool wasPressedLastFrame = false;

    void Update()
    {
        if (Mouse.current == null)
            return;

        bool isPressed = Mouse.current.leftButton.isPressed;

        // fire once on click
        if (isPressed && !wasPressedLastFrame)
        {
            Shoot();
            nextFireTime = Time.time + 1f / fireRate;
        }
        // fire continuously while held
        else if (isPressed && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + 1f / fireRate;
        }

        wasPressedLastFrame = isPressed;
    }

    void Shoot()
    {
        RaycastHit hitInfo;

        if (
            Physics.Raycast(
                cam.transform.position,
                cam.transform.forward,
                out hitInfo,
                shootingRange
            )
        )
        {
            Debug.Log(hitInfo.transform.name);

            Health objects = hitInfo.transform.GetComponent<Health>();
            if (objects != null)
            {
                objects.ObjectHitDamage(giveDamage);
            }
        }
    }
}

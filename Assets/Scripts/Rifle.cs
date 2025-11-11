using UnityEngine;
using UnityEngine.InputSystem;

public class Rifle : MonoBehaviour
{
    [Header("Rifle")]
    public Camera cam;
    public float damage = 10f;
    public float range = 100f;
    public LayerMask hitMask = ~0;

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            Shoot();
    }

    void Shoot()
    {
        if (!cam)
            return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (
            Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore)
        )
        {
            Debug.Log(hit.transform.name);
            // hit.transform.GetComponent<IDamageable>()?.ApplyDamage(damage);
        }
    }
}

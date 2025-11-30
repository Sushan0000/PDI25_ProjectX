using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ControlScript))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField]
    private float interactRadius = 2f;

    [SerializeField]
    private LayerMask interactLayers = ~0; // set in Inspector

    private ControlScript control;

    private void Awake()
    {
        control = GetComponent<ControlScript>();
    }

    private void Update()
    {
        // Only react when F is pressed this frame
        if (Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame)
            return;

        if (control == null)
            return;

        // Find all colliders around the player in the given radius, limited by layer mask
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            interactRadius,
            interactLayers,
            QueryTriggerInteraction.Collide
        );

        IInteractable closest = null;
        float closestDistSq = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            IInteractable interactable = col.GetComponentInParent<IInteractable>();
            if (interactable == null)
                continue;

            float distSq = (col.transform.position - transform.position).sqrMagnitude;
            if (distSq < closestDistSq)
            {
                closestDistSq = distSq;
                closest = interactable;
            }
        }

        // Interact with the closest interactable in range
        if (closest != null)
            closest.Interact(control);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
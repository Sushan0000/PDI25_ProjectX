using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerHealth : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Health health;

    [SerializeField]
    private ControlScript controlScript;

    [SerializeField]
    private Animator animator;

    // Add references to anything that reads input (weapon, camera, etc.)
    [Header("Other Scripts To Disable On Death")]
    [SerializeField]
    private MonoBehaviour[] extraScriptsToDisable;

    // e.g. drag your Rifle script here, maybe ThirdPersonCameraController if it reads input

    [Header("Animation")]
    [SerializeField]
    private string deathTriggerName = "Die";

    [SerializeField]
    private string deathBoolName = "IsDead"; // optional

    private bool isDead;

    private void Awake()
    {
        if (!health)
            health = GetComponent<Health>();

        if (!controlScript)
            controlScript = GetComponent<ControlScript>();

        if (!animator)
            animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (isDead)
            return;

        isDead = true;

        // stop movement
        if (controlScript)
            controlScript.enabled = false;

        // stop all other input / shooting scripts
        if (extraScriptsToDisable != null)
        {
            for (int i = 0; i < extraScriptsToDisable.Length; i++)
            {
                if (extraScriptsToDisable[i])
                    extraScriptsToDisable[i].enabled = false;
            }
        }

        // play death animation and lock in dead state
        if (animator && animator.runtimeAnimatorController != null)
        {
            if (!string.IsNullOrEmpty(deathTriggerName))
                animator.SetTrigger(deathTriggerName);

            if (!string.IsNullOrEmpty(deathBoolName))
                animator.SetBool(deathBoolName, true);
        }

        // mouse cursor free
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}

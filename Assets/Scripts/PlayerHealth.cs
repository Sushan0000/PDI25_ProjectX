// PlayerHealth.cs
using System.Collections;
using UnityEngine;

// Handles player death: listens to Health.OnDeath, disables controls, plays animation, unlocks cursor.
[RequireComponent(typeof(Health))]
public class PlayerHealth : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Health health; // Player Health component

    [SerializeField]
    private ControlScript controlScript; // Player movement / input script

    [SerializeField]
    private Animator animator; // Animator driving player animations

    [Header("UI")]
    [SerializeField]
    private GameOverUI gameOverUI;

    [Header("Game Over Delay")]
    [SerializeField]
    private float gameOverDelaySeconds = 2f;

    [Header("Other Scripts To Disable On Death")]
    [SerializeField]
    private MonoBehaviour[] extraScriptsToDisable;

    // Example: Rifle, camera controller, other input-driven scripts

    [Header("Animation")]
    [SerializeField]
    private string deathTriggerName = "Die"; // Trigger to start death animatione

    private bool isDead; // Ensures death logic runs only once

    // Cache references if not wired in the inspector.
    private void Awake()
    {
        if (!health)
            health = GetComponent<Health>();

        if (!controlScript)
            controlScript = GetComponent<ControlScript>();

        if (!animator)
            animator = GetComponentInChildren<Animator>();
    }

    // Subscribe to the Health death event.
    private void OnEnable()
    {
        if (health != null)
            health.OnDeath += HandleDeath;
    }

    // Unsubscribe to avoid callbacks on disabled/destroyed objects.
    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;
    }

    // Called once when Health reports that the player has died.
    private void HandleDeath()
    {
        if (isDead)
            return;

        isDead = true;

        // Stop player movement / input.
        if (controlScript)
            controlScript.enabled = false;

        // Disable any other scripts that read input (weapons, camera, etc.).
        if (extraScriptsToDisable != null)
        {
            for (int i = 0; i < extraScriptsToDisable.Length; i++)
            {
                if (extraScriptsToDisable[i])
                    extraScriptsToDisable[i].enabled = false;
            }
        }

        // Trigger death animation and set persistent dead flag on the animator.
        if (animator && animator.runtimeAnimatorController != null)
        {

            if (!string.IsNullOrEmpty(deathTriggerName))
                animator.SetTrigger(deathTriggerName);
        }

        // Unlock and show mouse cursor so the player can use menus after death.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnDeathAnimationFinished()
    {
        StartCoroutine(ShowGameOverAfterDelay());
    }

    private IEnumerator ShowGameOverAfterDelay()
    {
        yield return new WaitForSecondsRealtime(gameOverDelaySeconds);
        if (gameOverUI)
            gameOverUI.ShowGameOver();
    }
}

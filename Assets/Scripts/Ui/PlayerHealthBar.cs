// PlayerHealthBar.cs
using UnityEngine;
using UnityEngine.UI;

// Drives a UI Slider to display the player's current health.
public class PlayerHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Health playerHealth; // Player's Health component

    [SerializeField]
    private Slider slider; // UI Slider showing health

    private float maxHealth; // Cached slider max (initial health)

    // Auto-wire references if not set in the inspector.
    private void Awake()
    {
        // Try to find a Slider on this object or its children if not assigned.
        if (!slider)
            slider = GetComponentInChildren<Slider>();

        // Try to find the player's Health via ControlScript if not assigned.
        if (!playerHealth)
        {
            // Assumes your player has ControlScript + Health on the same GameObject.
            ControlScript player = Object.FindFirstObjectByType<ControlScript>();
            if (player)
                playerHealth = player.GetComponent<Health>();
        }
    }

    // Initialize slider range and initial value.
    private void Start()
    {
        if (!playerHealth || !slider)
        {
            Debug.LogError($"{nameof(PlayerHealthBar)}: Missing Health or Slider reference.", this);
            enabled = false; // Disable to avoid spam in Update
            return;
        }

        // Use current health as starting "max" for this bar.
        maxHealth = playerHealth.CurrentHealth;
        if (maxHealth <= 0f)
            maxHealth = 1f; // Avoid zero max value

        slider.minValue = 0f;
        slider.maxValue = maxHealth;
        slider.value = maxHealth;

        // This is a display-only bar.
        slider.interactable = false;
    }

    // Keep the slider value in sync with the player's health.
    private void Update()
    {
        if (!playerHealth || !slider)
            return;

        // Clamp to avoid weird values if health logic changes later (healing, etc.).
        float current = Mathf.Clamp(playerHealth.CurrentHealth, slider.minValue, slider.maxValue);
        slider.value = current;
    }
}

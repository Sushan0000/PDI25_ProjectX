using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Health playerHealth; // player's Health script

    [SerializeField]
    private Slider slider; // the UI Slider

    private float maxHealth;

    private void Awake()
    {
        // If not assigned, try to auto-find
        if (!slider)
            slider = GetComponentInChildren<Slider>(); // or GetComponent<Slider>() if script is on the Slider

        if (!playerHealth)
        {
            // assumes your player has ControlScript and Health on same object
            ControlScript player = Object.FindFirstObjectByType<ControlScript>();
            if (player)
                playerHealth = player.GetComponent<Health>();
        }
    }

    private void Start()
    {
        if (!playerHealth || !slider)
        {
            Debug.LogError($"{nameof(PlayerHealthBar)}: Missing references.", this);
            return;
        }

        maxHealth = playerHealth.CurrentHealth;
        if (maxHealth <= 0f)
            maxHealth = 1f;

        slider.minValue = 0f;
        slider.maxValue = maxHealth;
        slider.value = maxHealth;

        // this bar is only for display, so make it non-interactable
        slider.interactable = false;
    }

    private void Update()
    {
        if (!playerHealth || !slider)
            return;

        slider.value = playerHealth.CurrentHealth;
    }
}

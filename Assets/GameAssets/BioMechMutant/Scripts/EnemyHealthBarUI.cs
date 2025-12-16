using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private MechMutantEnemy enemy;

    [SerializeField]
    private Slider slider;

    [SerializeField]
    private Camera cam;

    private void Awake()
    {
        if (!enemy)
            enemy = GetComponentInParent<MechMutantEnemy>();

        if (!slider)
            slider = GetComponentInChildren<Slider>();

        if (!cam)
            cam = Camera.main;
    }

    private void Start()
    {
        if (!enemy || !slider)
        {
            Debug.LogError("EnemyHealthBarUI: Missing enemy or slider.", this);
            enabled = false;
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = enemy.MaxHealth;
        slider.value = enemy.CurrentHealth;
    }

    private void LateUpdate()
    {
        if (!enemy)
            return;

        // Update value
        slider.value = enemy.CurrentHealth;

        // Hide when dead (optional)
        if (enemy.IsDeadPublic || enemy.CurrentHealth <= 0f)
            gameObject.SetActive(false);

        // Face camera (billboard)
        if (cam)
        {
            Vector3 dir = transform.position - cam.transform.position;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}

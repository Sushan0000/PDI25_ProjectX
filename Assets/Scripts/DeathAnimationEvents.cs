using UnityEngine;

public class DeathAnimationEvents : MonoBehaviour
{
    [SerializeField]
    private PlayerHealth playerHealth;

    public void DeathAnimationFinished()
    {
        if (playerHealth)
            playerHealth.OnDeathAnimationFinished();
    }
}

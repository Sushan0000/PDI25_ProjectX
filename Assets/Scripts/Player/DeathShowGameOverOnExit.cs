using UnityEngine;

public class DeathShowGameOverOnExit : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var ph = animator.GetComponentInParent<PlayerHealth>();
        if (ph != null)
            ph.OnDeathAnimationFinished();
    }
}

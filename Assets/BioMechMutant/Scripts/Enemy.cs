using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class MechMutantEnemy : MonoBehaviour, IDamageable
{
    private enum State
    {
        Idle,
        Chasing,
        Attacking,
        Dead,
    }

    [Header("Target (Player)")]
    [SerializeField]
    private Transform target;

    [SerializeField]
    private string playerTag = "Player";

    [Header("Movement")]
    [SerializeField]
    private float detectionRadius = 15f;

    [SerializeField]
    private float loseInterestRadius = 20f;

    [SerializeField]
    private float fieldOfView = 120f;

    [SerializeField]
    private float stoppingDistance = 2f;

    [SerializeField]
    private float pathUpdateInterval = 0.1f;

    [SerializeField]
    private LayerMask obstacleMask;

    [Header("Attack")]
    [SerializeField]
    private float attackRange = 2f;

    [SerializeField]
    private float timeBetweenAttacks = 1.5f;

    [SerializeField]
    private int damagePerHit = 20;

    [SerializeField]
    private Transform attackPoint;

    [SerializeField]
    private float attackRadius = 1.5f;

    [SerializeField]
    private LayerMask playerLayer;

    // 0 = punch, 1 = swipe, 2 = jump attack
    [SerializeField]
    private int numberOfAttackTypes = 3;

    [Header("Health")]
    [SerializeField]
    private float maxHealth = 100f;

    [SerializeField]
    private float deathDestroyDelay = 5f;

    [Header("Animation")]
    [SerializeField]
    private Animator animator;

    // locomotion
    [SerializeField]
    private string MoveSpeedParam = "MoveSpeed"; // float

    // combat / death
    [SerializeField]
    private string isAttackingBoolParam = "isAttacking"; // bool

    [SerializeField]
    private string isDeadBoolParam = "isDead"; // bool

    // attack blend tree parameter (FLOAT)
    [SerializeField]
    private string attackIndexParam = "AttackIndex"; // float

    [SerializeField]
    private string attackTriggerParam = "Attack"; // trigger

    [SerializeField]
    private string hitTriggerParam = "Hit"; // trigger

    [SerializeField]
    private string dieTriggerParam = "Die"; // trigger

    // special idle triggers
    [SerializeField]
    private string roarTriggerParam = "Roar"; // trigger

    [SerializeField]
    private string flexTriggerParam = "Flex"; // trigger

    [SerializeField]
    private float idleSpecialMinDelay = 5f;

    [SerializeField]
    private float idleSpecialMaxDelay = 15f;

    [Header("Audio")]
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip[] idleClips;

    [SerializeField]
    private AudioClip[] chaseClips;

    [SerializeField]
    private AudioClip[] attackClips;

    [SerializeField]
    private AudioClip[] hurtClips;

    [SerializeField]
    private AudioClip[] deathClips;

    [Header("Death Animation State")]
    [SerializeField]
    private string deathStateName = "Death";

    private NavMeshAgent agent;
    private State state = State.Idle;
    private float currentHealth;
    private float lastAttackTime;
    private float nextPathUpdateTime;
    private float nextIdleSpecialTime;
    private int lastIdleVariant = -1; // 0 = plain idle, 1 = flex, 2 = roar

    private bool HasTarget => target != null;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        currentHealth = maxHealth;

        if (target == null && !string.IsNullOrEmpty(playerTag))
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
                target = playerObj.transform;
        }

        if (agent != null)
        {
            agent.stoppingDistance = stoppingDistance;
            agent.isStopped = false;
        }

        ScheduleNextIdleSpecial();
    }

    private void Update()
    {
        if (state == State.Dead)
            return;

        if (!HasTarget)
            TryFindTarget();

        switch (state)
        {
            case State.Idle:
                HandleIdle();
                break;
            case State.Chasing:
                HandleChasing();
                break;
            case State.Attacking:
                HandleAttacking();
                break;
        }

        UpdateAnimator();
    }

    private void TryFindTarget()
    {
        if (string.IsNullOrEmpty(playerTag))
            return;

        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
            target = playerObj.transform;
    }

    private void HandleIdle()
    {
        if (HasTarget && IsTargetInDetectionRange() && CanSeeTarget())
        {
            ChangeState(State.Chasing);
            return;
        }

        if (agent != null && !agent.isStopped)
            agent.isStopped = true;

        HandleIdleSpecialAnimations();
    }

    private void HandleChasing()
    {
        if (!HasTarget)
        {
            ChangeState(State.Idle);
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > loseInterestRadius || !IsTargetInDetectionRange() || !CanSeeTarget())
        {
            ChangeState(State.Idle);
            return;
        }

        if (distance <= attackRange)
        {
            ChangeState(State.Attacking);
            return;
        }

        if (agent == null)
            return;

        if (Time.time >= nextPathUpdateTime)
        {
            nextPathUpdateTime = Time.time + pathUpdateInterval;
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }

        FaceTarget();
    }

    private void HandleAttacking()
    {
        if (!HasTarget)
        {
            ChangeState(State.Idle);
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > attackRange + 0.5f)
        {
            ChangeState(State.Chasing);
            return;
        }

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }

        FaceTarget();

        if (Time.time >= lastAttackTime + timeBetweenAttacks)
        {
            lastAttackTime = Time.time;
            TriggerAttack();
        }
    }

    private void ChangeState(State newState)
    {
        if (state == newState)
            return;

        state = newState;

        switch (state)
        {
            case State.Idle:
                PlayRandomClip(idleClips);
                if (agent != null)
                    agent.isStopped = true;
                ScheduleNextIdleSpecial();
                break;

            case State.Chasing:
                PlayRandomClip(chaseClips);
                if (agent != null)
                    agent.isStopped = false;
                break;

            case State.Attacking:
                PlayRandomClip(attackClips);
                break;

            case State.Dead:
                if (agent != null)
                {
                    agent.isStopped = true;
                    agent.enabled = false;
                }

                Collider col = GetComponent<Collider>();
                if (col != null)
                    col.enabled = false;

                TriggerDeath();
                PlayRandomClip(deathClips);

                Destroy(gameObject, deathDestroyDelay);
                break;
        }
    }

    private bool IsTargetInDetectionRange()
    {
        if (!HasTarget)
            return false;

        float distance = Vector3.Distance(transform.position, target.position);
        return distance <= detectionRadius;
    }

    private bool CanSeeTarget()
    {
        if (!HasTarget)
            return false;

        Vector3 dirToTarget = (target.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToTarget);

        if (angle > fieldOfView * 0.5f)
            return false;

        float distance = Vector3.Distance(transform.position, target.position);

        if (
            Physics.Raycast(
                transform.position + Vector3.up,
                dirToTarget,
                out RaycastHit hit,
                distance,
                ~0
            )
        )
        {
            if (((1 << hit.collider.gameObject.layer) & obstacleMask) != 0)
                return false;

            if (hit.transform != target && !hit.transform.IsChildOf(target))
                return false;
        }

        return true;
    }

    private void FaceTarget()
    {
        if (!HasTarget)
            return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        float MoveSpeed = 0f;
        // Only drive locomotion speed when NOT attacking or dead
        if (agent != null && agent.enabled && state != State.Attacking && state != State.Dead)
            MoveSpeed = agent.velocity.magnitude;

        if (!string.IsNullOrEmpty(MoveSpeedParam))
            animator.SetFloat(MoveSpeedParam, MoveSpeed);

        if (!string.IsNullOrEmpty(isAttackingBoolParam))
            animator.SetBool(isAttackingBoolParam, state == State.Attacking);

        if (!string.IsNullOrEmpty(isDeadBoolParam))
            animator.SetBool(isDeadBoolParam, state == State.Dead);
    }

    private void TriggerAttack()
    {
        if (animator != null)
        {
            int attackIndexInt = Mathf.Clamp(
                Random.Range(0, numberOfAttackTypes),
                0,
                numberOfAttackTypes - 1
            );

            float attackIndex = attackIndexInt;

            if (!string.IsNullOrEmpty(attackIndexParam))
                animator.SetFloat(attackIndexParam, attackIndex); // BlendTree parameter

            if (!string.IsNullOrEmpty(attackTriggerParam))
                animator.SetTrigger(attackTriggerParam);
        }
        else
        {
            DoDamage();
        }
    }

    private void TriggerHitReaction()
    {
        if (state == State.Dead)
            return;

        if (animator == null || string.IsNullOrEmpty(hitTriggerParam))
            return;

        animator.SetTrigger(hitTriggerParam);
    }

    private void TriggerDeath()
    {
        if (animator == null)
            return;

        if (!string.IsNullOrEmpty(isDeadBoolParam))
            animator.SetBool(isDeadBoolParam, true);

        if (!string.IsNullOrEmpty(dieTriggerParam))
            animator.SetTrigger(dieTriggerParam);

        if (!string.IsNullOrEmpty(deathStateName))
        {
            animator.CrossFadeInFixedTime(deathStateName, 0.05f, 0, 0f);
        }
    }

    // idle / flex / roar random cycle while in Idle state
    private void HandleIdleSpecialAnimations()
    {
        if (animator == null)
            return;

        if (Time.time < nextIdleSpecialTime)
            return;

        // if somehow still moving, skip
        if (agent != null && agent.velocity.sqrMagnitude > 0.01f)
        {
            ScheduleNextIdleSpecial();
            return;
        }

        // 0 = plain idle (no trigger), 1 = flex, 2 = roar
        int variantCount = 3;
        int index;
        do
        {
            index = Random.Range(0, variantCount);
        } while (index == lastIdleVariant && variantCount > 1);

        lastIdleVariant = index;

        switch (index)
        {
            case 0:
                // plain idle: Locomotion blend tree already playing idle clip
                break;

            case 1:
                if (!string.IsNullOrEmpty(flexTriggerParam))
                    animator.SetTrigger(flexTriggerParam);
                break;

            case 2:
                if (!string.IsNullOrEmpty(roarTriggerParam))
                    animator.SetTrigger(roarTriggerParam);
                break;
        }

        ScheduleNextIdleSpecial();
    }

    private void ScheduleNextIdleSpecial()
    {
        idleSpecialMinDelay = Mathf.Max(0.1f, idleSpecialMinDelay);
        idleSpecialMaxDelay = Mathf.Max(idleSpecialMinDelay, idleSpecialMaxDelay);

        nextIdleSpecialTime = Time.time + Random.Range(idleSpecialMinDelay, idleSpecialMaxDelay);
    }

    private void PlayRandomClip(AudioClip[] clips)
    {
        if (audioSource == null || clips == null || clips.Length == 0)
            return;

        int index = Random.Range(0, clips.Length);
        AudioClip clip = clips[index];
        if (clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    // Called by animation event on the punch / swipe / jump attack clips.
    public void DoDamage()
    {
        if (state == State.Dead)
            return;

        if (!HasTarget)
            return;

        Vector3 origin =
            attackPoint != null
                ? attackPoint.position
                : transform.position + transform.forward * 1f + Vector3.up * 0.5f;

        Collider[] hits = Physics.OverlapSphere(
            origin,
            attackRadius,
            playerLayer,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];

            var damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.ApplyDamage(damagePerHit);
                continue;
            }

            var health = hit.GetComponent<Health>();
            if (health != null)
            {
                health.ApplyDamage(damagePerHit);
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (state == State.Dead)
            return;

        if (amount <= 0f)
            return;

        currentHealth -= amount;

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;

            PlayRandomClip(hurtClips);
            Die();
            return;
        }

        PlayRandomClip(hurtClips);
        TriggerHitReaction();
    }

    public void ApplyDamage(float amount)
    {
        TakeDamage(amount);
    }

    private void Die()
    {
        if (state == State.Dead)
            return;

        ChangeState(State.Dead);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, loseInterestRadius);

        Vector3 leftBoundary = Quaternion.Euler(0f, -fieldOfView * 0.5f, 0f) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0f, fieldOfView * 0.5f, 0f) * transform.forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position + Vector3.up, leftBoundary * detectionRadius);
        Gizmos.DrawRay(transform.position + Vector3.up, rightBoundary * detectionRadius);

        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(
                transform.position + transform.forward * 1f + Vector3.up * 0.5f,
                attackRadius
            );
        }
    }
}

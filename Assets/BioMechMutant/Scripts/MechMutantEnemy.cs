// MechMutantEnemy.cs
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMesh-driven melee enemy for FPS games with idle/chase/attack/dead states,
/// field-of-view detection, animation hooks, audio, and health via IDamageable.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Collider))]
public class MechMutantEnemy : MonoBehaviour, IDamageable
{
    /// <summary>
    /// High-level AI state for this enemy.
    /// </summary>
    private enum State
    {
        Idle,
        Chasing,
        Attacking,
        Dead,
    }

    // =========================================================
    // INSPECTOR FIELDS
    // =========================================================

    [Header("Target (Player)")]
    [SerializeField]
    private Transform target; // Current player transform to chase/attack

    [SerializeField]
    private string playerTag = "Player"; // Tag used to find the player at runtime

    [SerializeField]
    private float targetSearchInterval = 2f; // How often to search for the player if target is lost

    [Header("Movement")]
    [SerializeField]
    private float detectionRadius = 15f; // Distance at which the enemy starts tracking the player

    [SerializeField]
    private float loseInterestRadius = 20f; // Distance beyond which the enemy stops chasing

    [SerializeField]
    private float fieldOfView = 120f; // Vision cone angle in degrees

    [SerializeField]
    private float stoppingDistance = 2f; // Distance from player where agent stops moving

    [SerializeField]
    private float pathUpdateInterval = 0.1f; // Delay between NavMesh path updates

    [SerializeField]
    private float rotationSpeed = 10f; // How fast the enemy rotates towards the player

    [SerializeField]
    private LayerMask obstacleMask; // Layers that can block line-of-sight

    [Header("Attack")]
    [SerializeField]
    private float attackRange = 2f; // Distance at which the enemy will attack

    [SerializeField]
    private float attackExitBuffer = 0.5f; // Extra distance before enemy stops attacking and goes back to chase

    [SerializeField]
    private float timeBetweenAttacks = 1.5f; // Delay between consecutive attacks

    [SerializeField]
    private int damagePerHit = 20; // Damage applied per attack

    [SerializeField]
    private Transform attackPoint; // Origin point of attack overlap (e.g., hand/bone)

    [SerializeField]
    private float attackRadius = 1.5f; // Radius of the attack overlap sphere

    [SerializeField]
    private LayerMask playerLayer; // Layer(s) that represent the player

    [SerializeField]
    private int numberOfAttackTypes = 2; // Blend-tree attack variants (0..N-1)

    [Header("Health")]
    [SerializeField]
    private float maxHealth = 100f; // Maximum health

    [SerializeField]
    private float deathDestroyDelay = 5f; // Time after death before destroying this object

    [Header("Animation")]
    [SerializeField]
    private Animator animator; // Animator controlling this enemy

    [SerializeField]
    private string moveSpeedParam = "MoveSpeed"; // Animator float parameter: movement speed

    [SerializeField]
    private string isAttackingBoolParam = "isAttacking"; // Animator bool parameter: currently attacking

    [SerializeField]
    private string isDeadBoolParam = "isDead"; // Animator bool parameter: dead flag

    [SerializeField]
    private string attackIndexParam = "AttackIndex"; // Animator float parameter: which attack variant

    [SerializeField]
    private string attackTriggerParam = "Attack"; // Animator trigger: start attack

    [SerializeField]
    private string hitTriggerParam = "Hit"; // Animator trigger: hit reaction

    [SerializeField]
    private string dieTriggerParam = "Die"; // Animator trigger: start death animation

    [SerializeField]
    private string roarTriggerParam = "Roar"; // Animator trigger: roar idle variant

    [SerializeField]
    private string flexTriggerParam = "Flex"; // Animator trigger: flex idle variant

    [SerializeField]
    private float idleSpecialMinDelay = 3f; // Minimum time between special idle animations

    [SerializeField]
    private float idleSpecialMaxDelay = 10f; // Maximum time between special idle animations

    [Header("Audio")]
    [SerializeField]
    private AudioSource audioSource; // Single source for all enemy SFX

    [SerializeField]
    private AudioClip[] idleClips; // Random idle sounds

    [SerializeField]
    private AudioClip[] chaseClips; // Random chase sounds

    [SerializeField]
    private AudioClip[] attackClips; // Random attack sounds

    [SerializeField]
    private AudioClip[] hurtClips; // Random hurt sounds

    [SerializeField]
    private AudioClip[] deathClips; // Random death sounds

    [Header("Death Animation State")]
    [SerializeField]
    private string deathStateName = "Death"; // Animator state to blend into when dead

    // =========================================================
    // RUNTIME STATE
    // =========================================================

    private NavMeshAgent agent; // NavMesh agent handling pathfinding
    private State state = State.Idle; // Current AI state

    private float currentHealth; // Current health value
    private float lastAttackTime; // Time when last attack started
    private float nextPathUpdateTime; // Next time we are allowed to call SetDestination
    private float nextIdleSpecialTime; // Time at which we can play the next idle special
    private float nextTargetSearchTime; // Next time we are allowed to search for the player

    private int lastIdleVariant = -1; // Last idle special type used: 0 = plain, 1 = flex, 2 = roar

    // Computed properties for quick checks
    private bool HasTarget => target != null; // True when we have a valid player Transform
    private bool IsDead => state == State.Dead; // True when the enemy is in the dead state

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    /// <summary>
    /// Called when the component instance is first created.
    /// Caches NavMeshAgent and ensures animator and audioSource references are set.
    /// </summary>
    private void Awake()
    {
        // Cache agent so we do not call GetComponent every frame
        agent = GetComponent<NavMeshAgent>();

        // Auto-assign animator from children if not set in inspector
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Auto-assign AudioSource from this GameObject if not set in inspector
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Called before the first Update.
    /// Initializes health, NavMeshAgent values, finds initial target and schedules idle specials.
    /// </summary>
    private void Start()
    {
        // Ensure we never start with negative health
        currentHealth = Mathf.Max(0f, maxHealth);

        // Initialize attack timer to allow instant attack if needed
        lastAttackTime = -timeBetweenAttacks;

        // Allow immediate initial target search
        nextTargetSearchTime = Time.time;

        // Try to attach to player by tag if not wired in inspector
        if (!HasTarget && !string.IsNullOrEmpty(playerTag))
            FindTargetByTag();

        // Configure NavMeshAgent base settings
        if (agent != null)
        {
            agent.stoppingDistance = stoppingDistance;
            agent.isStopped = false;
        }

        // Schedule first special idle animation
        ScheduleNextIdleSpecial();
    }

    /// <summary>
    /// Called once per frame.
    /// Runs the state machine (Idle / Chasing / Attacking) and keeps animation / idle specials updated.
    /// </summary>
    private void Update()
    {
        // Once dead, do not execute any AI behavior
        if (IsDead)
            return;

        // If we lost the target (e.g. player re-spawn), periodically try to find a new one
        if (!HasTarget)
            TryFindTarget();

        // State-driven behavior
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

        // Update animator locomotion regardless of state
        UpdateAnimatorLocomotion();

        // Occasionally trigger special idle animations while idle
        TryPlayIdleSpecial();
    }

    /// <summary>
    /// Called by Unity when this object is selected in the editor.
    /// Draws gizmos for detection radius, FOV, and attack range for visual tuning.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Lose interest radius
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, loseInterestRadius);

        // FOV boundaries
        Vector3 leftBoundary = Quaternion.Euler(0f, -fieldOfView * 0.5f, 0f) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0f, fieldOfView * 0.5f, 0f) * transform.forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position + Vector3.up, leftBoundary * detectionRadius);
        Gizmos.DrawRay(transform.position + Vector3.up, rightBoundary * detectionRadius);

        // Attack range sphere from attack point (or fallback origin)
        Gizmos.color = Color.red;
        Vector3 attackOrigin =
            attackPoint != null
                ? attackPoint.position
                : transform.position + transform.forward * 1f + Vector3.up * 0.5f;

        Gizmos.DrawWireSphere(attackOrigin, attackRadius);
    }

    // =========================================================
    // TARGETING & DETECTION
    // =========================================================

    /// <summary>
    /// Periodically searches for the player by tag to avoid calling Find every frame.
    /// Updates the target reference when a player object is found.
    /// </summary>
    private void TryFindTarget()
    {
        // If we already have a target, no need to search
        if (HasTarget)
            return;

        // Respect search interval to avoid scanning every frame
        if (Time.time < nextTargetSearchTime)
            return;

        // Schedule next search time
        nextTargetSearchTime = Time.time + targetSearchInterval;

        // No tag configured, nothing to do
        if (string.IsNullOrEmpty(playerTag))
            return;

        FindTargetByTag();
    }

    /// <summary>
    /// Uses GameObject.FindGameObjectWithTag once to locate the player Transform and cache it.
    /// </summary>
    private void FindTargetByTag()
    {
        // Guard against invalid tag
        if (string.IsNullOrEmpty(playerTag))
            return;

        // Find first object with the matching tag
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
            target = playerObj.transform; // Cache transform for future use
    }

    /// <summary>
    /// Checks if the current target is within the detection radius distance.
    /// Returns false if there is no target or it is too far away.
    /// </summary>
    private bool IsTargetInDetectionRange()
    {
        if (!HasTarget)
            return false;

        // Straight-line distance check
        float distance = Vector3.Distance(transform.position, target.position);
        return distance <= detectionRadius;
    }

    /// <summary>
    /// Performs a field-of-view angle check and a raycast to ensure we have line of sight to the target.
    /// Returns true only if the target is in front and not blocked by an obstacle.
    /// </summary>
    private bool CanSeeTarget()
    {
        if (!HasTarget)
            return false;

        // Normalized direction from enemy to target used for angle and raycast
        Vector3 toTarget = (target.position - transform.position).normalized;

        // Angle between enemy forward and direction to target
        float angle = Vector3.Angle(transform.forward, toTarget);

        // Target outside vision cone
        if (angle > fieldOfView * 0.5f)
            return false;

        // Distance to target used as max ray length
        float distance = Vector3.Distance(transform.position, target.position);

        // Raycast to detect obstacles between enemy and target
        if (
            Physics.Raycast(
                transform.position + Vector3.up, // Slightly above ground to reduce hitting floor
                toTarget,
                out RaycastHit hit,
                distance,
                ~0, // All layers
                QueryTriggerInteraction.Ignore
            )
        ) // Ignore trigger colliders (e.g., volumes)
        {
            // If we hit something in the obstacle mask, the view is blocked
            if (((1 << hit.collider.gameObject.layer) & obstacleMask) != 0)
                return false;

            // If the hit object is not the target or its child, view is considered blocked
            if (hit.transform != target && !hit.transform.IsChildOf(target))
                return false;
        }

        // Passed angle and obstruction checks
        return true;
    }

    // =========================================================
    // STATE MACHINE: IDLE / CHASE / ATTACK
    // =========================================================

    /// <summary>
    /// Handles behavior while the enemy is idle.
    /// If the player is detected and visible, switches to chasing; otherwise stops movement.
    /// </summary>
    private void HandleIdle()
    {
        // If we have a visible target within detection radius, start chasing
        if (HasTarget && IsTargetInDetectionRange() && CanSeeTarget())
        {
            ChangeState(State.Chasing);
            return;
        }

        // Ensure agent is stopped in idle state
        if (agent != null && !agent.isStopped)
            agent.isStopped = true;
    }

    /// <summary>
    /// Handles behavior while the enemy is chasing the player.
    /// Loses interest if the player goes too far or breaks line of sight; starts attacking when close enough.
    /// </summary>
    private void HandleChasing()
    {
        // If we lost the target (destroyed or missing), go back to idle
        if (!HasTarget)
        {
            ChangeState(State.Idle);
            return;
        }

        // Current straight-line distance to target
        float distance = Vector3.Distance(transform.position, target.position);

        // Stop chasing if too far away or cannot see the player anymore
        if (distance > loseInterestRadius || !IsTargetInDetectionRange() || !CanSeeTarget())
        {
            ChangeState(State.Idle);
            return;
        }

        // When close enough, switch from chase to attack state
        if (distance <= attackRange)
        {
            ChangeState(State.Attacking);
            return;
        }

        // Continue chasing via NavMeshAgent
        if (agent != null)
        {
            if (agent.isStopped)
                agent.isStopped = false;

            // Throttle path updates to reduce CPU cost
            if (Time.time >= nextPathUpdateTime)
            {
                nextPathUpdateTime = Time.time + pathUpdateInterval;
                agent.SetDestination(target.position);
            }
        }
    }

    /// <summary>
    /// Handles behavior while the enemy is in attacking mode.
    /// Keeps facing the player, stops moving, and triggers attacks at fixed time intervals.
    /// </summary>
    private void HandleAttacking()
    {
        // If target becomes null, fallback to idle
        if (!HasTarget)
        {
            ChangeState(State.Idle);
            return;
        }

        // Check if the player has moved out of attack range
        float distance = Vector3.Distance(transform.position, target.position);

        // If the player goes beyond attack range + buffer, resume chasing
        if (distance > attackRange + attackExitBuffer)
        {
            ChangeState(State.Chasing);
            return;
        }

        // Stop NavMesh movement to keep enemy in place while attacking
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }

        // Continuously face the player while attacking
        FaceTarget();

        // Trigger attacks only after enough time has passed since last attack
        if (Time.time >= lastAttackTime + timeBetweenAttacks)
        {
            lastAttackTime = Time.time;
            TriggerAttack();
        }
    }

    /// <summary>
    /// Switches the enemy to a new state and performs one-time actions for that state
    /// such as playing audio, controlling NavMeshAgent and handling death.
    /// </summary>
    private void ChangeState(State newState)
    {
        // Prevent redundant transitions
        if (state == newState)
            return;

        state = newState;

        switch (state)
        {
            case State.Idle:
                // Play idle audio and stop movement
                PlayRandomClip(idleClips);
                if (agent != null)
                    agent.isStopped = true;

                // Schedule an idle special once we return to idle
                ScheduleNextIdleSpecial();
                break;

            case State.Chasing:
                // Play chase audio and allow movement
                PlayRandomClip(chaseClips);
                if (agent != null)
                    agent.isStopped = false;
                break;

            case State.Attacking:
                // Play attack audio once when entering attack state
                PlayRandomClip(attackClips);
                break;

            case State.Dead:
                // Final state transition: stop movement, disable collision and trigger death visuals
                HandleDeathStateTransition();
                break;
        }
    }

    /// <summary>
    /// Performs all setup required when the enemy dies:
    /// stops and disables the NavMeshAgent, disables the collider,
    /// triggers death animations and schedules object destruction.
    /// </summary>
    private void HandleDeathStateTransition()
    {
        // Disable NavMesh behavior for dead body
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // Disable main collider so dead enemy no longer interacts physically
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // Play death animation and audio
        TriggerDeath();
        PlayRandomClip(deathClips);

        // Remove enemy from scene after a delay
        Destroy(gameObject, deathDestroyDelay);
    }

    // =========================================================
    // MOVEMENT & ANIMATION
    // =========================================================

    /// <summary>
    /// Rotates the enemy on the horizontal plane so it faces the target.
    /// Uses smooth interpolation based on rotationSpeed for natural turning.
    /// </summary>
    private void FaceTarget()
    {
        if (!HasTarget)
            return;

        // Horizontal direction from enemy to target
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        // Ignore almost-zero direction to avoid jitter
        if (direction.sqrMagnitude < 0.001f)
            return;

        // Desired rotation to look at the target
        Quaternion targetRot = Quaternion.LookRotation(direction);

        // Smoothly interpolate rotation for smoother turn
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            Time.deltaTime * rotationSpeed
        );
    }

    /// <summary>
    /// Updates animator parameters related to movement and attacking.
    /// Feeds the NavMeshAgent velocity into a speed float and sets an attacking bool.
    /// </summary>
    private void UpdateAnimatorLocomotion()
    {
        if (animator == null)
            return;

        float speed = 0f;

        // Use agent velocity magnitude as movement speed for animations
        if (agent != null && agent.enabled)
            speed = agent.velocity.magnitude;

        // Drive locomotion blend tree with movement speed
        if (!string.IsNullOrEmpty(moveSpeedParam))
            animator.SetFloat(moveSpeedParam, speed);

        // Flag whether we are in attacking state for overlay/upper-body animations
        if (!string.IsNullOrEmpty(isAttackingBoolParam))
            animator.SetBool(isAttackingBoolParam, state == State.Attacking);
    }

    // =========================================================
    // COMBAT
    // =========================================================

    /// <summary>
    /// Chooses a random attack type index and triggers the attack animation.
    /// Damage is not applied here; it is applied by an animation event calling DoDamage at the hit frame.
    /// </summary>
    private void TriggerAttack()
    {
        if (animator == null)
            return;

        // Pick a random attack index in range [0, numberOfAttackTypes - 1]
        int attackIndexInt = Mathf.Clamp(
            Random.Range(0, numberOfAttackTypes),
            0,
            numberOfAttackTypes - 1
        );

        // Set blend tree parameter so correct attack animation is chosen
        if (!string.IsNullOrEmpty(attackIndexParam))
            animator.SetFloat(attackIndexParam, attackIndexInt);

        // Fire attack trigger to start the attack animation
        if (!string.IsNullOrEmpty(attackTriggerParam))
            animator.SetTrigger(attackTriggerParam);

        DoDamage();
    }

    /// <summary>
    /// Called by an animation event during an attack animation.
    /// Creates an overlap sphere at the attack point and applies damage to any player targets found.
    /// </summary>
    public void DoDamage()
    {
        // Dead enemies cannot deal damage
        if (IsDead)
            return;

        // If we have no target, there is nothing to hit
        if (!HasTarget)
            return;

        // Origin position for damage overlap sphere
        Vector3 origin =
            attackPoint != null
                ? attackPoint.position
                : transform.position + transform.forward * 1f + Vector3.up * 0.5f;

        // Collect all colliders on the playerLayer within attackRadius
        Collider[] hits = Physics.OverlapSphere(
            origin,
            attackRadius,
            playerLayer, // Only hit objects on player layer(s)
            QueryTriggerInteraction.Ignore // Ignore triggers like volumes
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];

            // Prefer generic IDamageable to support different health implementations
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.ApplyDamage(damagePerHit);
                continue; // Skip Health fallback if we already applied via IDamageable
            }

            // Fallback: direct Health component if present
            Health health = hit.GetComponent<Health>();
            if (health != null)
            {
                health.ApplyDamage(damagePerHit);
            }
        }
    }

    // =========================================================
    // HEALTH & DAMAGE
    // =========================================================

    /// <summary>
    /// Applies incoming damage to this enemy, plays hurt audio,
    /// triggers a hit reaction, and handles death when health reaches zero.
    /// </summary>
    public void TakeDamage(float amount)
    {
        // Already dead; ignore any further damage
        if (IsDead)
            return;

        // Ignore non-positive damage values
        if (amount <= 0f)
            return;

        // Reduce current health by incoming damage
        currentHealth -= amount;

        // Check for death
        if (currentHealth <= 0f)
        {
            currentHealth = 0f; // Clamp to zero

            // Play hurt audio one last time before dying
            PlayRandomClip(hurtClips);

            // Trigger full death behavior
            Die();
            return;
        }

        // Non-lethal hit: play hurt sound and hit reaction
        PlayRandomClip(hurtClips);
        TriggerHitReaction();
    }

    /// <summary>
    /// Implementation of IDamageable.
    /// Forwards the given damage amount to TakeDamage so external systems can damage this enemy.
    /// </summary>
    public void ApplyDamage(float amount)
    {
        // Use single damage entry point to keep logic in one place
        TakeDamage(amount);
    }

    /// <summary>
    /// Ensures the enemy only transitions to the dead state once.
    /// Calls ChangeState(State.Dead) to trigger full death behavior.
    /// </summary>
    private void Die()
    {
        // If we are already in dead state, do nothing
        if (IsDead)
            return;

        // Trigger transition into Dead state (stops AI, triggers animations, etc.)
        ChangeState(State.Dead);
    }

    /// <summary>
    /// Triggers a hit reaction animation if the enemy is still alive.
    /// Uses a trigger parameter to play a short "flinch" animation on damage.
    /// </summary>
    private void TriggerHitReaction()
    {
        if (IsDead)
            return;

        // Ensure animator and parameter are valid before triggering
        if (animator == null || string.IsNullOrEmpty(hitTriggerParam))
            return;

        animator.SetTrigger(hitTriggerParam);
    }

    /// <summary>
    /// Sets animator values related to death and transitions into the configured death animation state.
    /// This is called as part of the death state transition.
    /// </summary>
    private void TriggerDeath()
    {
        if (animator == null)
            return;

        // Set boolean dead flag on animator so AnimatorController can react
        if (!string.IsNullOrEmpty(isDeadBoolParam))
            animator.SetBool(isDeadBoolParam, true);

        // Fire death trigger to play death animation
        if (!string.IsNullOrEmpty(dieTriggerParam))
            animator.SetTrigger(dieTriggerParam);

        // Cross-fade into a specific death state for smoother start if specified
        if (!string.IsNullOrEmpty(deathStateName))
            animator.CrossFadeInFixedTime(deathStateName, 0.05f, 0, 0f);
    }

    // =========================================================
    // IDLE SPECIALS
    // =========================================================

    /// <summary>
    /// Picks a random time window between idleSpecialMinDelay and idleSpecialMaxDelay
    /// and stores when the next idle special animation is allowed to play.
    /// </summary>
    private void ScheduleNextIdleSpecial()
    {
        // Clamp min to a small positive value to avoid zero or negative delays
        idleSpecialMinDelay = Mathf.Max(0.1f, idleSpecialMinDelay);

        // Ensure max is never less than min
        idleSpecialMaxDelay = Mathf.Max(idleSpecialMinDelay, idleSpecialMaxDelay);

        // Randomize next idle special time between min and max delay
        nextIdleSpecialTime = Time.time + Random.Range(idleSpecialMinDelay, idleSpecialMaxDelay);
    }

    /// <summary>
    /// While the enemy is idle, occasionally triggers a flex or roar animation
    /// by using a timer and avoiding repeating the same variant twice in a row.
    /// </summary>
    private void TryPlayIdleSpecial()
    {
        // Idle specials only play while in Idle state
        if (state != State.Idle)
            return;

        if (animator == null)
            return;

        // Not yet time for the next idle special
        if (Time.time < nextIdleSpecialTime)
            return;

        // Randomly choose between variant 1 (flex) and 2 (roar)
        int variant;
        do
        {
            variant = Random.Range(1, 3); // Picks 1 or 2
        } while (variant == lastIdleVariant); // Avoid same variant twice in a row

        lastIdleVariant = variant;

        // Trigger chosen idle special based on variant index
        if (variant == 1 && !string.IsNullOrEmpty(flexTriggerParam))
        {
            animator.SetTrigger(flexTriggerParam);
        }
        else if (variant == 2 && !string.IsNullOrEmpty(roarTriggerParam))
        {
            animator.SetTrigger(roarTriggerParam);
        }

        // Schedule the next idle special after current one
        ScheduleNextIdleSpecial();
    }

    // =========================================================
    // AUDIO
    // =========================================================

    /// <summary>
    /// Picks a random audio clip from the given array and plays it through the AudioSource.
    /// Safely returns if the source or clip list is missing or empty.
    /// </summary>
    private void PlayRandomClip(AudioClip[] clips)
    {
        // Validate dependencies and clip list
        if (audioSource == null || clips == null || clips.Length == 0)
            return;

        // Random index into clip array
        int index = Random.Range(0, clips.Length);
        AudioClip clip = clips[index];

        if (clip == null)
            return;

        // Play one-shot so it does not interrupt currently playing sounds
        audioSource.PlayOneShot(clip);
    }
}

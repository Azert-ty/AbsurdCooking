


using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    private enum FacingMode
    {
        Movement,
        WaypointIdle,
        LookAtPosition,
        LookAtTarget
    }

    // =========================================================
    // ANIMATION
    // =========================================================

    [Header("Animation")]
    [SerializeField] private Animator animator;

    // =========================================================
    // VISION
    // =========================================================

    [Header("Vision")]
    [SerializeField] private Transform visionPivot;

    [Header("Cone Correction")]
    [Tooltip("Correction globale du cône. Laisse 0 si le cône regarde déjà dans la bonne direction.")]
    [SerializeField] private float globalConeAngleOffset = 0f;

    // =========================================================
    // ROTATION
    // =========================================================

    [Header("Rotation")]
    [Tooltip("Vitesse de rotation du cône en patrouille.")]
    [SerializeField] private float rotationSpeed = 180f;

    [Tooltip("Vitesse de rotation du cône pendant la poursuite.")]
    [SerializeField] private float chaseRotationSpeed = 360f;

    [SerializeField] private bool waitUntilRotatedBeforeMove = true;

    [SerializeField] private bool waitUntilLookRotationFinishedAtWaypoint = true;

    [SerializeField] private float rotationTolerance = 1f;

    private float targetVisionAngle;
    private bool hasTargetVisionAngle;

    private float activeConeAngleOffset;

    private EnemyWaypointLook.TurnDirection activeTurnDirection =
        EnemyWaypointLook.TurnDirection.Shortest;

    private Vector2 visualDirection = Vector2.down;

    private FacingMode facingMode = FacingMode.Movement;

    private Transform lookTarget;
    private Vector3 lookPosition;

    // =========================================================
    // SPEED
    // =========================================================

    [Header("Speed")]
    [SerializeField] private float patrolSpeed = 2.6f;
    [SerializeField] private float chaseStartSpeed = 3.4f;
    [SerializeField] private float chaseSpeed = 4.6f;
    [SerializeField] private float chaseSpeedRamp = 2.2f;

    // =========================================================
    // FATIGUE
    // =========================================================

    [Header("Fatigue")]
    [SerializeField] private float timeBeforeFatigue = 4f;
    [SerializeField] private float tiredChaseSpeed = 3.3f;
    [SerializeField] private GameObject tiredIcon;

    private float chaseTimer;
    private bool isTired;

    // =========================================================
    // CHASE BEHAVIOUR
    // =========================================================

    [Header("Chase Behaviour")]
    [SerializeField] private float chaseTrailDelayPerRank = 0.18f;
    [SerializeField] private float leadPredictionTime = 0.12f;
    [SerializeField] private float chaseRepathInterval = 0.12f;
    [SerializeField] private float minDestinationChange = 0.12f;

    private int chaseRank = 1;

    private float nextChaseRepathTime;

    private Vector3 lastChaseDestination;

    private bool hasLastChaseDestination;

    // =========================================================
    // CHASE / CONE SYNCHRONIZATION
    // =========================================================

    [Header("Chase / Cone Synchronization")]

    [Tooltip("Synchronise le déplacement de poursuite avec l'orientation du cône.")]
    [SerializeField] private bool synchronizeChaseWithCone = true;

    [Tooltip("Sous cet angle, l'ennemi court à pleine vitesse.")]
    [SerializeField] private float chaseFullSpeedAngle = 12f;

    [Tooltip("Au-dessus de cet angle, l'ennemi s'arrête pour tourner.")]
    [SerializeField] private float chaseTurnLockAngle = 70f;

    [Tooltip("Angle sous lequel l'ennemi peut recommencer à avancer après un gros virage.")]
    [SerializeField] private float chaseTurnUnlockAngle = 22f;

    [Tooltip("Vitesse minimale pendant un virage qui ne nécessite pas un arrêt complet.")]
    [Range(0f, 1f)]
    [SerializeField] private float chaseMinimumMovementFactor = 0.2f;

    private bool isChasing;
    private bool chaseTurnLocked;

    private float chaseBaseSpeed;
    private float chaseMovementFactor = 1f;

    // =========================================================
    // PATROL
    // =========================================================

    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;

    [SerializeField] private PatrolPathFeedback patrolPathFeedback;

    public Transform[] Waypoints => waypoints;

    [SerializeField] private float waitTime = 2f;

    [SerializeField] private float turnDelayBeforeMove = 0.2f;

    private NavMeshAgent agent;

    private int currentWaypoint;

    private bool isWaiting;

    public bool IsWaiting => isWaiting;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;

            agent.acceleration = 6f;
            agent.angularSpeed = 360f;
            agent.stoppingDistance = 0.1f;
            agent.autoBraking = false;

            agent.avoidancePriority = Random.Range(20, 80);
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (patrolPathFeedback == null)
        {
            patrolPathFeedback =
                GetComponent<PatrolPathFeedback>();
        }

        if (tiredIcon != null)
        {
            tiredIcon.SetActive(false);
        }

        activeConeAngleOffset =
            globalConeAngleOffset;

        activeTurnDirection =
            EnemyWaypointLook.TurnDirection.Shortest;

        chaseBaseSpeed = chaseStartSpeed;

        if (visionPivot != null)
        {
            targetVisionAngle =
                visionPivot.eulerAngles.z;

            hasTargetVisionAngle = true;

            SyncIdleSpriteWithCurrentCone();
        }

        UpdateAnimation();
    }

    private void Update()
    {
        // 1. Décide où le cône doit regarder.
        UpdateConeFacingTarget();

        // 2. Fait réellement tourner le cône.
        UpdateVisionRotation();

        // 3. Adapte le déplacement de chase à l'angle du cône.
        UpdateChaseConeSynchronization();

        // 4. Synchronise le sprite.
        UpdateSpriteDirection();

        // 5. Met à jour l'Animator.
        UpdateAnimation();
    }

    // =========================================================
    // AGENT
    // =========================================================

    private bool AgentReady()
    {
        return agent != null &&
               agent.enabled &&
               agent.isOnNavMesh;
    }

    private bool IsMoving()
    {
        if (!AgentReady())
        {
            return false;
        }

        if (agent.isStopped)
        {
            return false;
        }

        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            return true;
        }

        if (!agent.hasPath)
        {
            return false;
        }

        if (agent.pathPending)
        {
            return true;
        }

        return agent.remainingDistance >
               agent.stoppingDistance + 0.05f;
    }

    // =========================================================
    // CONE TARGET
    // =========================================================

    private void UpdateConeFacingTarget()
    {
        if (facingMode == FacingMode.LookAtTarget)
        {
            if (lookTarget == null)
            {
                return;
            }

            Vector2 direction =
                lookTarget.position -
                transform.position;

            SetTargetAngleFromDirection(direction);

            return;
        }

        if (facingMode == FacingMode.LookAtPosition)
        {
            Vector2 direction =
                lookPosition -
                transform.position;

            SetTargetAngleFromDirection(direction);

            return;
        }

        if (facingMode == FacingMode.WaypointIdle)
        {
            return;
        }

        UpdateConeFromMovement();
    }

    private void UpdateConeFromMovement()
    {
        Vector2 direction;

        if (isChasing)
        {
            direction =
                GetChaseSteeringDirection();
        }
        else
        {
            direction =
                GetAgentMovementDirection();
        }

        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        activeConeAngleOffset =
            globalConeAngleOffset;

        activeTurnDirection =
            EnemyWaypointLook.TurnDirection.Shortest;

        SetTargetAngleFromDirection(direction);
    }

    // =========================================================
    // MOVEMENT DIRECTIONS
    // =========================================================

    private Vector2 GetAgentMovementDirection()
    {
        if (!AgentReady())
        {
            return Vector2.zero;
        }

        Vector2 direction = Vector2.zero;

        // Direction actuelle.
        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            direction = agent.velocity;
        }
        // Direction voulue par le NavMesh.
        else if (agent.desiredVelocity.sqrMagnitude > 0.01f)
        {
            direction = agent.desiredVelocity;
        }
        // Prochain point du chemin.
        else if (agent.hasPath && !agent.pathPending)
        {
            direction =
                agent.steeringTarget -
                transform.position;
        }
        // Destination finale.
        else
        {
            direction =
                agent.destination -
                transform.position;
        }

        return direction;
    }

    private Vector2 GetChaseSteeringDirection()
    {
        if (!AgentReady())
        {
            return Vector2.zero;
        }

        // -----------------------------------------------------
        // 1. Direction que le NavMesh veut prendre.
        // -----------------------------------------------------

        if (agent.desiredVelocity.sqrMagnitude > 0.01f)
        {
            return agent.desiredVelocity.normalized;
        }

        // -----------------------------------------------------
        // 2. Prochain angle du chemin.
        //
        // Très important quand l'agent est arrêté pour tourner.
        // -----------------------------------------------------

        if (agent.hasPath && !agent.pathPending)
        {
            Vector2 directionToSteeringTarget =
                agent.steeringTarget -
                transform.position;

            if (directionToSteeringTarget.sqrMagnitude > 0.01f)
            {
                return directionToSteeringTarget.normalized;
            }
        }

        // -----------------------------------------------------
        // 3. Destination finale.
        // -----------------------------------------------------

        if (agent.hasPath)
        {
            Vector2 directionToDestination =
                agent.destination -
                transform.position;

            if (directionToDestination.sqrMagnitude > 0.01f)
            {
                return directionToDestination.normalized;
            }
        }

        return Vector2.zero;
    }

    // =========================================================
    // ANGLES
    // =========================================================

    private float DirectionToAngle(Vector2 direction)
    {
        return Mathf.Repeat(
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg,
            360f
        );
    }

    private void SetTargetAngleFromDirection(
        Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        float rawAngle =
            DirectionToAngle(direction);

        SetTargetVisionAngle(
            rawAngle,
            activeConeAngleOffset
        );
    }

    private void SetTargetVisionAngle(
        float rawAngle,
        float coneOffset)
    {
        targetVisionAngle =
            Mathf.Repeat(
                rawAngle + coneOffset,
                360f
            );

        hasTargetVisionAngle = true;
    }

    // =========================================================
    // VISION ROTATION
    // =========================================================

    private void UpdateVisionRotation()
    {
        if (visionPivot == null)
        {
            return;
        }

        if (!hasTargetVisionAngle)
        {
            return;
        }

        float currentAngle =
            visionPivot.eulerAngles.z;

        float activeRotationSpeed =
            isChasing
                ? chaseRotationSpeed
                : rotationSpeed;

        float newAngle =
            MoveAngleWithDirection(
                currentAngle,
                targetVisionAngle,
                activeRotationSpeed * Time.deltaTime,
                activeTurnDirection
            );

        visionPivot.rotation =
            Quaternion.Euler(
                0f,
                0f,
                newAngle
            );
    }

    private float MoveAngleWithDirection(
        float currentAngle,
        float targetAngle,
        float maxDelta,
        EnemyWaypointLook.TurnDirection turnDirection)
    {
        currentAngle =
            Mathf.Repeat(currentAngle, 360f);

        targetAngle =
            Mathf.Repeat(targetAngle, 360f);

        if (Mathf.Abs(
                Mathf.DeltaAngle(
                    currentAngle,
                    targetAngle
                )
            ) <= 0.01f)
        {
            return targetAngle;
        }

        // -----------------------------------------------------
        // SHORTEST
        // -----------------------------------------------------

        if (turnDirection ==
            EnemyWaypointLook.TurnDirection.Shortest)
        {
            return Mathf.MoveTowardsAngle(
                currentAngle,
                targetAngle,
                maxDelta
            );
        }

        // -----------------------------------------------------
        // CLOCKWISE
        // -----------------------------------------------------

        if (turnDirection ==
            EnemyWaypointLook.TurnDirection.Clockwise)
        {
            float clockwiseDistance =
                Mathf.Repeat(
                    currentAngle - targetAngle,
                    360f
                );

            if (clockwiseDistance <= maxDelta)
            {
                return targetAngle;
            }

            return Mathf.Repeat(
                currentAngle - maxDelta,
                360f
            );
        }

        // -----------------------------------------------------
        // COUNTER CLOCKWISE
        // -----------------------------------------------------

        if (turnDirection ==
            EnemyWaypointLook.TurnDirection.CounterClockwise)
        {
            float counterClockwiseDistance =
                Mathf.Repeat(
                    targetAngle - currentAngle,
                    360f
                );

            if (counterClockwiseDistance <= maxDelta)
            {
                return targetAngle;
            }

            return Mathf.Repeat(
                currentAngle + maxDelta,
                360f
            );
        }

        return Mathf.MoveTowardsAngle(
            currentAngle,
            targetAngle,
            maxDelta
        );
    }

    // =========================================================
    // CHASE SYNCHRONIZATION
    // =========================================================

    private void UpdateChaseConeSynchronization()
    {
        if (!isChasing)
        {
            chaseMovementFactor = 1f;
            return;
        }

        if (!AgentReady())
        {
            return;
        }

        // -----------------------------------------------------
        // Synchronisation désactivée.
        // -----------------------------------------------------

        if (!synchronizeChaseWithCone)
        {
            chaseTurnLocked = false;
            chaseMovementFactor = 1f;

            if (agent.isStopped)
            {
                agent.isStopped = false;
            }

            agent.speed = chaseBaseSpeed;

            return;
        }

        // -----------------------------------------------------
        // Impossible de calculer l'orientation.
        // -----------------------------------------------------

        if (visionPivot == null ||
            !hasTargetVisionAngle)
        {
            chaseTurnLocked = false;
            chaseMovementFactor = 1f;

            if (agent.isStopped)
            {
                agent.isStopped = false;
            }

            agent.speed = chaseBaseSpeed;

            return;
        }

        float angleDifference =
            Mathf.Abs(
                Mathf.DeltaAngle(
                    visionPivot.eulerAngles.z,
                    targetVisionAngle
                )
            );

        // -----------------------------------------------------
        // L'ennemi est déjà arrêté pour un gros virage.
        // -----------------------------------------------------

        if (chaseTurnLocked)
        {
            chaseMovementFactor = 0f;

            if (angleDifference >
                chaseTurnUnlockAngle)
            {
                if (!agent.isStopped)
                {
                    agent.isStopped = true;
                }

                return;
            }

            // Le cône est suffisamment revenu.
            chaseTurnLocked = false;

            if (agent.isStopped)
            {
                agent.isStopped = false;
            }
        }

        // -----------------------------------------------------
        // Nouveau très gros virage.
        // -----------------------------------------------------

        if (angleDifference >=
            chaseTurnLockAngle)
        {
            chaseTurnLocked = true;
            chaseMovementFactor = 0f;

            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            return;
        }

        // -----------------------------------------------------
        // Virage normal.
        //
        // Plus le cône est éloigné de la direction voulue,
        // plus l'ennemi ralentit.
        // -----------------------------------------------------

        chaseMovementFactor =
            1f -
            Mathf.InverseLerp(
                chaseFullSpeedAngle,
                chaseTurnLockAngle,
                angleDifference
            );

        chaseMovementFactor =
            Mathf.Clamp01(
                chaseMovementFactor
            );

        float effectiveMovementFactor =
            Mathf.Max(
                chaseMinimumMovementFactor,
                chaseMovementFactor
            );

        if (agent.isStopped)
        {
            agent.isStopped = false;
        }

        agent.speed =
            chaseBaseSpeed *
            effectiveMovementFactor;
    }

    // =========================================================
    // SPRITE DIRECTION
    // =========================================================

    private void UpdateSpriteDirection()
    {
        // -----------------------------------------------------
        // En chase :
        //
        // le sprite suit le cône.
        //
        // Ainsi :
        // sprite + cône = même orientation.
        // -----------------------------------------------------

        if (isChasing &&
            visionPivot != null)
        {
            SyncIdleSpriteWithCurrentCone();
            return;
        }

        // -----------------------------------------------------
        // Hors chase :
        //
        // le sprite suit le mouvement réel.
        // -----------------------------------------------------

        if (IsMoving())
        {
            Vector2 movementDirection =
                GetAgentMovementDirection();

            if (movementDirection.sqrMagnitude >= 0.01f)
            {
                float movementAngle =
                    DirectionToAngle(
                        movementDirection
                    );

                visualDirection =
                    AngleToCardinalDirection(
                        movementAngle
                    );
            }

            return;
        }

        SyncIdleSpriteWithCurrentCone();
    }

    private void SyncIdleSpriteWithCurrentCone()
    {
        if (visionPivot == null)
        {
            return;
        }

        float coneCurrentAngle =
            visionPivot.eulerAngles.z;

        float spriteAngle =
            coneCurrentAngle -
            activeConeAngleOffset;

        visualDirection =
            AngleToCardinalDirection(
                spriteAngle
            );
    }

    private bool VisionReachedTargetAngle()
    {
        if (visionPivot == null)
        {
            return true;
        }

        if (!hasTargetVisionAngle)
        {
            return true;
        }

        float difference =
            Mathf.Abs(
                Mathf.DeltaAngle(
                    visionPivot.eulerAngles.z,
                    targetVisionAngle
                )
            );

        return difference <=
               rotationTolerance;
    }

    private Vector2 AngleToCardinalDirection(
        float angle)
    {
        angle =
            Mathf.Repeat(angle, 360f);

        if (angle >= 45f &&
            angle < 135f)
        {
            return Vector2.up;
        }

        if (angle >= 135f &&
            angle < 225f)
        {
            return Vector2.left;
        }

        if (angle >= 225f &&
            angle < 315f)
        {
            return Vector2.down;
        }

        return Vector2.right;
    }

    // =========================================================
    // ANIMATION
    // =========================================================

    private void UpdateAnimation()
    {
        if (animator == null)
        {
            return;
        }

        bool moving = IsMoving();

        animator.SetBool(
            "IsMoving",
            moving
        );

        if (moving)
        {
            animator.SetFloat(
                "MoveX",
                visualDirection.x
            );

            animator.SetFloat(
                "MoveY",
                visualDirection.y
            );
        }
        else
        {
            animator.SetFloat(
                "MoveX",
                0f
            );

            animator.SetFloat(
                "MoveY",
                0f
            );
        }

        animator.SetFloat(
            "LastX",
            visualDirection.x
        );

        animator.SetFloat(
            "LastY",
            visualDirection.y
        );
    }

    // =========================================================
    // CHASE MODE
    // =========================================================

    private void EnterChaseMode()
    {
        if (isChasing)
        {
            return;
        }

        isChasing = true;

        chaseTurnLocked = false;

        chaseMovementFactor = 1f;

        chaseBaseSpeed =
            chaseStartSpeed;
    }

    private void ExitChaseMode()
    {
        isChasing = false;

        chaseTurnLocked = false;

        chaseMovementFactor = 1f;

        if (AgentReady() &&
            agent.isStopped)
        {
            agent.isStopped = false;
        }
    }

    // =========================================================
    // SPEED MODES
    // =========================================================

    public void SetPatrolSpeed()
    {
        ExitChaseMode();

        if (AgentReady())
        {
            agent.speed = patrolSpeed;
        }

        ResetChasePathMemory();
    }

    public void SetChaseSpeed()
    {
        EnterChaseMode();

        chaseBaseSpeed =
            chaseSpeed;

        if (AgentReady() &&
            !chaseTurnLocked)
        {
            agent.speed =
                chaseBaseSpeed;
        }
    }

    public void SetChaseRank(int rank)
    {
        chaseRank =
            Mathf.Max(1, rank);
    }

    // =========================================================
    // DESTINATION
    // =========================================================

    public bool ReachedDestination()
    {
        if (!AgentReady())
        {
            return false;
        }

        if (agent.pathPending)
        {
            return false;
        }

        return agent.remainingDistance <=
               agent.stoppingDistance + 0.1f;
    }

    public void MoveTo(Vector3 target)
    {
        if (!AgentReady())
        {
            return;
        }

        ExitChaseMode();

        facingMode =
            FacingMode.Movement;

        lookTarget = null;

        activeConeAngleOffset =
            globalConeAngleOffset;

        activeTurnDirection =
            EnemyWaypointLook.TurnDirection.Shortest;

        agent.isStopped = false;

        agent.SetDestination(target);
    }

    public void MoveToAndFace(
        Vector3 targetPosition)
    {
        if (!AgentReady())
        {
            return;
        }

        ExitChaseMode();

        facingMode =
            FacingMode.LookAtPosition;

        lookPosition =
            targetPosition;

        lookTarget = null;

        activeConeAngleOffset =
            globalConeAngleOffset;

        activeTurnDirection =
            EnemyWaypointLook.TurnDirection.Shortest;

        agent.isStopped = false;

        agent.SetDestination(
            targetPosition
        );
    }

    // =========================================================
    // CHASE
    // =========================================================

    public void ChaseTarget(Transform target)
    {
        if (!AgentReady())
        {
            return;
        }

        if (target == null)
        {
            return;
        }

        EnterChaseMode();

        facingMode =
            FacingMode.Movement;

        lookTarget = null;

        activeConeAngleOffset =
            globalConeAngleOffset;

        activeTurnDirection =
            EnemyWaypointLook.TurnDirection.Shortest;

        // -----------------------------------------------------
        // IMPORTANT :
        //
        // On ne redémarre pas l'agent si le système
        // de rotation l'a volontairement arrêté.
        // -----------------------------------------------------

        if (!chaseTurnLocked &&
            agent.isStopped)
        {
            agent.isStopped = false;
        }

        // -----------------------------------------------------
        // Limitation des recalculs de destination.
        // -----------------------------------------------------

        if (Time.time <
            nextChaseRepathTime)
        {
            return;
        }

        Vector3 chaseDestination =
            EnemyChaseCoordinator.GetChaseDestination(
                this,
                target,
                chaseTrailDelayPerRank,
                leadPredictionTime
            );

        // -----------------------------------------------------
        // Ignore les changements trop petits.
        // -----------------------------------------------------

        if (hasLastChaseDestination)
        {
            float sqrDistance =
                (
                    chaseDestination -
                    lastChaseDestination
                ).sqrMagnitude;

            if (sqrDistance <
                minDestinationChange *
                minDestinationChange)
            {
                nextChaseRepathTime =
                    Time.time +
                    chaseRepathInterval;

                return;
            }
        }

        // -----------------------------------------------------
        // Nouvelle destination.
        // -----------------------------------------------------

        agent.SetDestination(
            chaseDestination
        );

        lastChaseDestination =
            chaseDestination;

        hasLastChaseDestination = true;

        nextChaseRepathTime =
            Time.time +
            chaseRepathInterval;
    }

    private void ResetChasePathMemory()
    {
        nextChaseRepathTime = 0f;

        hasLastChaseDestination = false;

        lastChaseDestination =
            Vector3.zero;
    }

    // =========================================================
    // PATROL
    // =========================================================

    // public IEnumerator PatrolRoutine()
    // {
    //     SetPatrolSpeed();

    //     facingMode =
    //         FacingMode.Movement;

    //     lookTarget = null;

    //     if (waypoints == null ||
    //         waypoints.Length < 2)
    //     {
    //         yield break;
    //     }

    //     if (patrolPathFeedback == null)
    //     {
    //         patrolPathFeedback =
    //             GetComponent<PatrolPathFeedback>();
    //     }

    //     while (true)
    //     {
    //         int startWaypointIndex =
    //             currentWaypoint;

    //         int targetWaypointIndex =
    //             currentWaypoint + 1;

    //         if (targetWaypointIndex >=
    //             waypoints.Length)
    //         {
    //             targetWaypointIndex = 0;
    //         }

    //         // -------------------------------------------------
    //         // PATH FEEDBACK
    //         // -------------------------------------------------

    //         if (patrolPathFeedback != null)
    //         {
    //             patrolPathFeedback.StartSegment(
    //                 startWaypointIndex,
    //                 targetWaypointIndex
    //             );
    //         }

    //         // -------------------------------------------------
    //         // TOURNE VERS LE PROCHAIN WAYPOINT
    //         // -------------------------------------------------

    //         FaceNextWaypointFromCurrentWaypoint(
    //             waypoints[startWaypointIndex],
    //             waypoints[targetWaypointIndex].position
    //         );

    //         if (turnDelayBeforeMove > 0f)
    //         {
    //             yield return new WaitForSeconds(
    //                 turnDelayBeforeMove
    //             );
    //         }

    //         if (waitUntilRotatedBeforeMove)
    //         {
    //             while (!VisionReachedTargetAngle())
    //             {
    //                 yield return null;
    //             }
    //         }

    //         // -------------------------------------------------
    //         // DÉPLACEMENT
    //         // -------------------------------------------------

    //         facingMode =
    //             FacingMode.Movement;

    //         if (AgentReady())
    //         {
    //             agent.isStopped = false;

    //             agent.SetDestination(
    //                 waypoints[
    //                     targetWaypointIndex
    //                 ].position
    //             );
    //         }

    //         while (
    //             AgentReady() &&
    //             (
    //                 agent.pathPending ||
    //                 agent.remainingDistance >
    //                 agent.stoppingDistance + 0.05f
    //             )
    //         )
    //         {
    //             yield return null;
    //         }

    //         // -------------------------------------------------
    //         // ARRÊT
    //         // -------------------------------------------------

    //         if (AgentReady())
    //         {
    //             agent.isStopped = true;

    //             agent.ResetPath();

    //             agent.velocity =
    //                 Vector3.zero;
    //         }

    //         // -------------------------------------------------
    //         // REGARD DU WAYPOINT
    //         // -------------------------------------------------

    //         SetLookDirectionFromWaypoint(
    //             waypoints[targetWaypointIndex]
    //         );

    //         isWaiting = true;

    //         if (waitUntilLookRotationFinishedAtWaypoint)
    //         {
    //             while (!VisionReachedTargetAngle())
    //             {
    //                 yield return null;
    //             }
    //         }

    //         yield return new WaitForSeconds(
    //             waitTime
    //         );

    //         isWaiting = false;

    //         currentWaypoint =
    //             targetWaypointIndex;
    //     }
    // }



    public IEnumerator PatrolRoutine()
{
    SetPatrolSpeed();

    facingMode = FacingMode.Movement;
    lookTarget = null;

    if (waypoints == null || waypoints.Length == 0)
        yield break;

    if (patrolPathFeedback == null)
        patrolPathFeedback = GetComponent<PatrolPathFeedback>();

    // =====================================================
    // CAS SPÉCIAL : UN SEUL WAYPOINT
    // =====================================================

    if (waypoints.Length == 1)
    {
        Transform homeWaypoint = waypoints[0];

        if (homeWaypoint == null)
            yield break;

        // Tourne vers son poste.
        FaceNextWaypointFromCurrentWaypoint(
            null,
            homeWaypoint.position
        );

        if (turnDelayBeforeMove > 0f)
        {
            yield return new WaitForSeconds(
                turnDelayBeforeMove
            );
        }

        if (waitUntilRotatedBeforeMove)
        {
            while (!VisionReachedTargetAngle())
                yield return null;
        }

        // Retourne au waypoint.
        facingMode = FacingMode.Movement;

        if (AgentReady())
        {
            agent.isStopped = false;

            agent.SetDestination(
                homeWaypoint.position
            );
        }

        while (AgentReady() &&
               (agent.pathPending ||
                agent.remainingDistance >
                agent.stoppingDistance + 0.05f))
        {
            yield return null;
        }

        // Une fois revenu, s'arrête.
        if (AgentReady())
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        // Reprend l'orientation définie par son unique waypoint.
        SetLookDirectionFromWaypoint(
            homeWaypoint
        );

        isWaiting = true;

        if (waitUntilLookRotationFinishedAtWaypoint)
        {
            while (!VisionReachedTargetAngle())
                yield return null;
        }

        // Il reste à son poste.
        while (true)
            yield return null;
    }

    // =====================================================
    // CAS NORMAL : DEUX WAYPOINTS OU PLUS
    // =====================================================

    while (true)
    {
        int startWaypointIndex = currentWaypoint;

        int targetWaypointIndex = currentWaypoint + 1;

        if (targetWaypointIndex >= waypoints.Length)
            targetWaypointIndex = 0;

        if (patrolPathFeedback != null)
        {
            patrolPathFeedback.StartSegment(
                startWaypointIndex,
                targetWaypointIndex
            );
        }

        FaceNextWaypointFromCurrentWaypoint(
            waypoints[startWaypointIndex],
            waypoints[targetWaypointIndex].position
        );

        if (turnDelayBeforeMove > 0f)
        {
            yield return new WaitForSeconds(
                turnDelayBeforeMove
            );
        }

        if (waitUntilRotatedBeforeMove)
        {
            while (!VisionReachedTargetAngle())
                yield return null;
        }

        facingMode = FacingMode.Movement;

        if (AgentReady())
        {
            agent.isStopped = false;

            agent.SetDestination(
                waypoints[targetWaypointIndex].position
            );
        }

        while (AgentReady() &&
               (agent.pathPending ||
                agent.remainingDistance >
                agent.stoppingDistance + 0.05f))
        {
            yield return null;
        }

        if (AgentReady())
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        SetLookDirectionFromWaypoint(
            waypoints[targetWaypointIndex]
        );

        isWaiting = true;

        if (waitUntilLookRotationFinishedAtWaypoint)
        {
            while (!VisionReachedTargetAngle())
                yield return null;
        }

        yield return new WaitForSeconds(
            waitTime
        );

        isWaiting = false;

        currentWaypoint = targetWaypointIndex;
    }
}

    private void FaceNextWaypointFromCurrentWaypoint(
        Transform currentWaypointTransform,
        Vector3 nextWaypointPosition)
    {
        Vector2 direction =
            nextWaypointPosition -
            transform.position;

        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        activeConeAngleOffset =
            globalConeAngleOffset;

        activeTurnDirection =
            EnemyWaypointLook.TurnDirection.Shortest;

        if (currentWaypointTransform != null)
        {
            EnemyWaypointLook waypointLook =
                currentWaypointTransform
                    .GetComponent<EnemyWaypointLook>();

            if (waypointLook != null &&
                waypointLook.OverrideExitTurnDirection())
            {
                activeTurnDirection =
                    waypointLook.GetExitTurnDirection();
            }
        }

        facingMode =
            FacingMode.LookAtPosition;

        lookPosition =
            nextWaypointPosition;

        lookTarget = null;

        SetTargetAngleFromDirection(
            direction
        );

        UpdateAnimation();
    }

    private void SetLookDirectionFromWaypoint(
        Transform waypoint)
    {
        if (waypoint == null)
        {
            return;
        }

        float waypointRawAngle =
            waypoint.eulerAngles.z;

        float waypointConeOffset = 0f;

        EnemyWaypointLook.TurnDirection
            waypointTurnDirection =
                EnemyWaypointLook
                    .TurnDirection
                    .Shortest;

        EnemyWaypointLook waypointLook =
            waypoint.GetComponent<EnemyWaypointLook>();

        if (waypointLook != null)
        {
            waypointRawAngle =
                waypointLook.GetLookAngle();

            waypointConeOffset =
                waypointLook
                    .GetWaypointConeAngleOffset();

            waypointTurnDirection =
                waypointLook
                    .GetTurnDirection();
        }

        activeConeAngleOffset =
            globalConeAngleOffset +
            waypointConeOffset;

        activeTurnDirection =
            waypointTurnDirection;

        SetTargetVisionAngle(
            waypointRawAngle,
            activeConeAngleOffset
        );

        facingMode =
            FacingMode.WaypointIdle;

        lookTarget = null;

        SyncIdleSpriteWithCurrentCone();

        UpdateAnimation();
    }

    // =========================================================
    // STOP
    // =========================================================

    public void StopMovement()
    {
        ExitChaseMode();

        if (!AgentReady())
        {
            return;
        }

        agent.isStopped = true;

        agent.ResetPath();

        agent.velocity =
            Vector3.zero;
    }

    // =========================================================
    // FATIGUE
    // =========================================================

    public void StartChaseFatigue()
    {
        EnterChaseMode();

        chaseTimer = 0f;

        isTired = false;

        chaseBaseSpeed =
            chaseStartSpeed;

        if (tiredIcon != null)
        {
            tiredIcon.SetActive(false);
        }

        ResetChasePathMemory();

        if (AgentReady())
        {
            agent.speed =
                chaseBaseSpeed;
        }
    }

    public void UpdateChaseFatigue()
    {
        if (!AgentReady())
        {
            return;
        }

        if (isTired)
        {
            return;
        }

        chaseTimer +=
            Time.deltaTime;

        chaseBaseSpeed =
            Mathf.MoveTowards(
                chaseBaseSpeed,
                chaseSpeed,
                chaseSpeedRamp *
                Time.deltaTime
            );

        if (chaseTimer >=
            timeBeforeFatigue)
        {
            BecomeTired();
        }
    }

    private void BecomeTired()
    {
        isTired = true;

        chaseBaseSpeed =
            tiredChaseSpeed;

        if (tiredIcon != null)
        {
            tiredIcon.SetActive(true);
        }
    }

    public void ResetFatigue()
    {
        chaseTimer = 0f;

        isTired = false;

        if (tiredIcon != null)
        {
            tiredIcon.SetActive(false);
        }

        SetPatrolSpeed();
    }

    // =========================================================
    // MANUAL FACING
    // =========================================================

    public void FaceDirection(
        Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        facingMode =
            FacingMode.LookAtPosition;

        lookPosition =
            transform.position +
            (Vector3)direction;

        lookTarget = null;

        activeConeAngleOffset =
            globalConeAngleOffset;

        activeTurnDirection =
            EnemyWaypointLook.TurnDirection.Shortest;

        SetTargetAngleFromDirection(
            direction
        );

        UpdateAnimation();
    }

    public void FacePosition(
        Vector3 targetPosition)
    {
        Vector2 direction =
            targetPosition -
            transform.position;

        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        facingMode =
            FacingMode.LookAtPosition;

        lookPosition =
            targetPosition;

        lookTarget = null;

        activeConeAngleOffset =
            globalConeAngleOffset;

        activeTurnDirection =
            EnemyWaypointLook.TurnDirection.Shortest;

        SetTargetAngleFromDirection(
            direction
        );

        UpdateAnimation();
    }

    public void FaceTarget(
        Transform target)
    {
        if (target == null)
        {
            return;
        }

        facingMode =
            FacingMode.LookAtTarget;

        lookTarget = target;

        activeConeAngleOffset =
            globalConeAngleOffset;

        activeTurnDirection =
            EnemyWaypointLook.TurnDirection.Shortest;

        SetTargetAngleFromDirection(
            target.position -
            transform.position
        );

        UpdateAnimation();
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmos()
    {
        if (waypoints == null ||
            waypoints.Length == 0)
        {
            return;
        }

        Gizmos.color = Color.black;

        for (int i = 0;
             i < waypoints.Length;
             i++)
        {
            if (waypoints[i] == null)
            {
                continue;
            }

            Gizmos.DrawSphere(
                waypoints[i].position,
                0.15f
            );

            int nextIndex =
                (i + 1) %
                waypoints.Length;

            if (waypoints[nextIndex] != null)
            {
                Gizmos.DrawLine(
                    waypoints[i].position,
                    waypoints[nextIndex].position
                );
            }
        }
    }
}
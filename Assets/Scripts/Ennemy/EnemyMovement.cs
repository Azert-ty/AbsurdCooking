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

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Vision")]
    [SerializeField] private Transform visionPivot;

    [Header("Cone Correction")]
    [Tooltip("Correction globale du cône. Laisse 0 si le cône regarde déjà dans la bonne direction.")]
    [SerializeField] private float globalConeAngleOffset = 0f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 180f;

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

    [Header("Speed")]
    [SerializeField] private float patrolSpeed = 2.6f;
    [SerializeField] private float chaseStartSpeed = 3.4f;
    [SerializeField] private float chaseSpeed = 4.6f;
    [SerializeField] private float chaseSpeedRamp = 2.2f;

    [Header("Fatigue")]
    [SerializeField] private float timeBeforeFatigue = 4f;
    [SerializeField] private float tiredChaseSpeed = 3.3f;
    [SerializeField] private GameObject tiredIcon;

    private float chaseTimer;
    private bool isTired;

    [Header("Chase Behaviour")]
    [SerializeField] private float chaseTrailDelayPerRank = 0.18f;
    [SerializeField] private float leadPredictionTime = 0.12f;
    [SerializeField] private float chaseRepathInterval = 0.12f;
    [SerializeField] private float minDestinationChange = 0.12f;

    private int chaseRank = 1;
    private float nextChaseRepathTime;
    private Vector3 lastChaseDestination;
    private bool hasLastChaseDestination;

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
            animator = GetComponentInChildren<Animator>();

        if (patrolPathFeedback == null)
            patrolPathFeedback = GetComponent<PatrolPathFeedback>();

        if (tiredIcon != null)
            tiredIcon.SetActive(false);

        activeConeAngleOffset = globalConeAngleOffset;
        activeTurnDirection = EnemyWaypointLook.TurnDirection.Shortest;

        if (visionPivot != null)
        {
            targetVisionAngle = visionPivot.eulerAngles.z;
            hasTargetVisionAngle = true;

            SyncIdleSpriteWithCurrentCone();
        }

        UpdateAnimation();
    }

    private void Update()
    {
        UpdateConeFacingTarget();

        UpdateVisionRotation();

        UpdateSpriteDirection();

        UpdateAnimation();
    }

    private bool AgentReady()
    {
        return agent != null &&
               agent.enabled &&
               agent.isOnNavMesh;
    }

    private bool IsMoving()
    {
        if (!AgentReady())
            return false;

        if (agent.isStopped)
            return false;

        if (agent.velocity.sqrMagnitude > 0.01f)
            return true;

        if (!agent.hasPath)
            return false;

        if (agent.pathPending)
            return true;

        return agent.remainingDistance > agent.stoppingDistance + 0.05f;
    }

    private void UpdateConeFacingTarget()
    {
        if (facingMode == FacingMode.LookAtTarget)
        {
            if (lookTarget == null)
                return;

            Vector2 direction =
                lookTarget.position - transform.position;

            SetTargetAngleFromDirection(direction);
            return;
        }

        if (facingMode == FacingMode.LookAtPosition)
        {
            Vector2 direction =
                lookPosition - transform.position;

            SetTargetAngleFromDirection(direction);
            return;
        }

        if (facingMode == FacingMode.WaypointIdle)
            return;

        UpdateConeFromMovement();
    }

    private void UpdateConeFromMovement()
    {
        Vector2 direction = GetAgentMovementDirection();

        if (direction.sqrMagnitude < 0.01f)
            return;

        activeConeAngleOffset = globalConeAngleOffset;
        activeTurnDirection = EnemyWaypointLook.TurnDirection.Shortest;

        SetTargetAngleFromDirection(direction);
    }

    private void UpdateSpriteDirection()
    {
        if (IsMoving())
        {
            Vector2 movementDirection = GetAgentMovementDirection();

            if (movementDirection.sqrMagnitude >= 0.01f)
            {
                float movementAngle = DirectionToAngle(movementDirection);
                visualDirection = AngleToCardinalDirection(movementAngle);
            }

            return;
        }

        SyncIdleSpriteWithCurrentCone();
    }

    private Vector2 GetAgentMovementDirection()
    {
        if (!AgentReady())
            return Vector2.zero;

        Vector2 direction = Vector2.zero;

        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            direction = agent.velocity;
        }
        else if (agent.desiredVelocity.sqrMagnitude > 0.01f)
        {
            direction = agent.desiredVelocity;
        }
        else if (agent.hasPath && !agent.pathPending)
        {
            direction = agent.steeringTarget - transform.position;
        }
        else
        {
            direction = agent.destination - transform.position;
        }

        return direction;
    }

    private float DirectionToAngle(Vector2 direction)
    {
        return Mathf.Repeat(
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg,
            360f
        );
    }

    private void SetTargetAngleFromDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
            return;

        float rawAngle = DirectionToAngle(direction);

        SetTargetVisionAngle(rawAngle, activeConeAngleOffset);
    }

    private void SetTargetVisionAngle(float rawAngle, float coneOffset)
    {
        targetVisionAngle = Mathf.Repeat(rawAngle + coneOffset, 360f);
        hasTargetVisionAngle = true;
    }

    private void UpdateVisionRotation()
    {
        if (visionPivot == null)
            return;

        if (!hasTargetVisionAngle)
            return;

        float currentAngle = visionPivot.eulerAngles.z;

        float newAngle =
            MoveAngleWithDirection(
                currentAngle,
                targetVisionAngle,
                rotationSpeed * Time.deltaTime,
                activeTurnDirection
            );

        visionPivot.rotation =
            Quaternion.Euler(0f, 0f, newAngle);
    }

    private float MoveAngleWithDirection(
        float currentAngle,
        float targetAngle,
        float maxDelta,
        EnemyWaypointLook.TurnDirection turnDirection)
    {
        currentAngle = Mathf.Repeat(currentAngle, 360f);
        targetAngle = Mathf.Repeat(targetAngle, 360f);

        if (Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle)) <= 0.01f)
            return targetAngle;

        if (turnDirection == EnemyWaypointLook.TurnDirection.Shortest)
        {
            return Mathf.MoveTowardsAngle(
                currentAngle,
                targetAngle,
                maxDelta
            );
        }

        if (turnDirection == EnemyWaypointLook.TurnDirection.Clockwise)
        {
            float clockwiseDistance =
                Mathf.Repeat(currentAngle - targetAngle, 360f);

            if (clockwiseDistance <= maxDelta)
                return targetAngle;

            return Mathf.Repeat(currentAngle - maxDelta, 360f);
        }

        if (turnDirection == EnemyWaypointLook.TurnDirection.CounterClockwise)
        {
            float counterClockwiseDistance =
                Mathf.Repeat(targetAngle - currentAngle, 360f);

            if (counterClockwiseDistance <= maxDelta)
                return targetAngle;

            return Mathf.Repeat(currentAngle + maxDelta, 360f);
        }

        return Mathf.MoveTowardsAngle(
            currentAngle,
            targetAngle,
            maxDelta
        );
    }

    private void SyncIdleSpriteWithCurrentCone()
    {
        if (visionPivot == null)
            return;

        float coneCurrentAngle = visionPivot.eulerAngles.z;

        float spriteAngle =
            coneCurrentAngle - activeConeAngleOffset;

        visualDirection =
            AngleToCardinalDirection(spriteAngle);
    }

    private bool VisionReachedTargetAngle()
    {
        if (visionPivot == null)
            return true;

        if (!hasTargetVisionAngle)
            return true;

        float difference =
            Mathf.Abs(
                Mathf.DeltaAngle(
                    visionPivot.eulerAngles.z,
                    targetVisionAngle
                )
            );

        return difference <= rotationTolerance;
    }

    private Vector2 AngleToCardinalDirection(float angle)
    {
        angle = Mathf.Repeat(angle, 360f);

        if (angle >= 45f && angle < 135f)
            return Vector2.up;

        if (angle >= 135f && angle < 225f)
            return Vector2.left;

        if (angle >= 225f && angle < 315f)
            return Vector2.down;

        return Vector2.right;
    }

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        bool moving = IsMoving();

        animator.SetBool("IsMoving", moving);

        if (moving)
        {
            animator.SetFloat("MoveX", visualDirection.x);
            animator.SetFloat("MoveY", visualDirection.y);
        }
        else
        {
            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveY", 0f);
        }

        animator.SetFloat("LastX", visualDirection.x);
        animator.SetFloat("LastY", visualDirection.y);
    }

    public void SetPatrolSpeed()
    {
        if (AgentReady())
            agent.speed = patrolSpeed;

        ResetChasePathMemory();
    }

    public void SetChaseRank(int rank)
    {
        chaseRank = Mathf.Max(1, rank);
    }

    public bool ReachedDestination()
    {
        if (!AgentReady())
            return false;

        if (agent.pathPending)
            return false;

        return agent.remainingDistance <= agent.stoppingDistance + 0.1f;
    }

    public void MoveTo(Vector3 target)
    {
        if (!AgentReady())
            return;

        facingMode = FacingMode.Movement;
        lookTarget = null;

        activeConeAngleOffset = globalConeAngleOffset;
        activeTurnDirection = EnemyWaypointLook.TurnDirection.Shortest;

        agent.isStopped = false;
        agent.SetDestination(target);
    }

    public void MoveToAndFace(Vector3 targetPosition)
    {
        if (!AgentReady())
            return;

        facingMode = FacingMode.LookAtPosition;
        lookPosition = targetPosition;
        lookTarget = null;

        activeConeAngleOffset = globalConeAngleOffset;
        activeTurnDirection = EnemyWaypointLook.TurnDirection.Shortest;

        agent.isStopped = false;
        agent.SetDestination(targetPosition);
    }

    public void ChaseTarget(Transform target)
    {
        if (!AgentReady())
            return;

        if (target == null)
            return;

        facingMode = FacingMode.Movement;
        lookTarget = null;

        activeConeAngleOffset = globalConeAngleOffset;
        activeTurnDirection = EnemyWaypointLook.TurnDirection.Shortest;

        agent.isStopped = false;

        if (Time.time < nextChaseRepathTime)
            return;

        Vector3 chaseDestination =
            EnemyChaseCoordinator.GetChaseDestination(
                this,
                target,
                chaseTrailDelayPerRank,
                leadPredictionTime
            );

        if (hasLastChaseDestination)
        {
            float sqrDistance =
                (chaseDestination - lastChaseDestination).sqrMagnitude;

            if (sqrDistance < minDestinationChange * minDestinationChange)
                return;
        }

        agent.SetDestination(chaseDestination);

        lastChaseDestination = chaseDestination;
        hasLastChaseDestination = true;
        nextChaseRepathTime = Time.time + chaseRepathInterval;
    }

    private void ResetChasePathMemory()
    {
        nextChaseRepathTime = 0f;
        hasLastChaseDestination = false;
        lastChaseDestination = Vector3.zero;
    }

    public IEnumerator PatrolRoutine()
    {
        SetPatrolSpeed();

        facingMode = FacingMode.Movement;
        lookTarget = null;

        if (waypoints == null || waypoints.Length < 2)
            yield break;

        if (patrolPathFeedback == null)
            patrolPathFeedback = GetComponent<PatrolPathFeedback>();

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
                yield return new WaitForSeconds(turnDelayBeforeMove);

            if (waitUntilRotatedBeforeMove)
            {
                while (!VisionReachedTargetAngle())
                    yield return null;
            }

            facingMode = FacingMode.Movement;

            if (AgentReady())
            {
                agent.isStopped = false;
                agent.SetDestination(waypoints[targetWaypointIndex].position);
            }

            while (AgentReady() &&
                   (agent.pathPending ||
                    agent.remainingDistance > agent.stoppingDistance + 0.05f))
            {
                yield return null;
            }

            if (AgentReady())
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }

            SetLookDirectionFromWaypoint(waypoints[targetWaypointIndex]);

            isWaiting = true;

            if (waitUntilLookRotationFinishedAtWaypoint)
            {
                while (!VisionReachedTargetAngle())
                    yield return null;
            }

            yield return new WaitForSeconds(waitTime);

            isWaiting = false;

            currentWaypoint = targetWaypointIndex;
        }
    }

    private void FaceNextWaypointFromCurrentWaypoint(
        Transform currentWaypointTransform,
        Vector3 nextWaypointPosition)
    {
        Vector2 direction =
            nextWaypointPosition - transform.position;

        if (direction.sqrMagnitude < 0.01f)
            return;

        activeConeAngleOffset = globalConeAngleOffset;
        activeTurnDirection = EnemyWaypointLook.TurnDirection.Shortest;

        if (currentWaypointTransform != null)
        {
            EnemyWaypointLook waypointLook =
                currentWaypointTransform.GetComponent<EnemyWaypointLook>();

            if (waypointLook != null &&
                waypointLook.OverrideExitTurnDirection())
            {
                activeTurnDirection =
                    waypointLook.GetExitTurnDirection();
            }
        }

        facingMode = FacingMode.LookAtPosition;
        lookPosition = nextWaypointPosition;
        lookTarget = null;

        SetTargetAngleFromDirection(direction);

        UpdateAnimation();
    }

    private void SetLookDirectionFromWaypoint(Transform waypoint)
    {
        if (waypoint == null)
            return;

        float waypointRawAngle = waypoint.eulerAngles.z;
        float waypointConeOffset = 0f;

        EnemyWaypointLook.TurnDirection waypointTurnDirection =
            EnemyWaypointLook.TurnDirection.Shortest;

        EnemyWaypointLook waypointLook =
            waypoint.GetComponent<EnemyWaypointLook>();

        if (waypointLook != null)
        {
            waypointRawAngle = waypointLook.GetLookAngle();
            waypointConeOffset = waypointLook.GetWaypointConeAngleOffset();
            waypointTurnDirection = waypointLook.GetTurnDirection();
        }

        activeConeAngleOffset =
            globalConeAngleOffset + waypointConeOffset;

        activeTurnDirection = waypointTurnDirection;

        SetTargetVisionAngle(waypointRawAngle, activeConeAngleOffset);

        facingMode = FacingMode.WaypointIdle;
        lookTarget = null;

        SyncIdleSpriteWithCurrentCone();

        UpdateAnimation();
    }

    public void StopMovement()
    {
        if (!AgentReady())
            return;

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    public void SetChaseSpeed()
    {
        if (AgentReady())
            agent.speed = chaseSpeed;
    }

    public void StartChaseFatigue()
    {
        chaseTimer = 0f;
        isTired = false;

        if (tiredIcon != null)
            tiredIcon.SetActive(false);

        ResetChasePathMemory();

        if (AgentReady())
            agent.speed = chaseStartSpeed;
    }

    public void UpdateChaseFatigue()
    {
        if (!AgentReady())
            return;

        if (isTired)
            return;

        chaseTimer += Time.deltaTime;

        agent.speed =
            Mathf.MoveTowards(
                agent.speed,
                chaseSpeed,
                chaseSpeedRamp * Time.deltaTime
            );

        if (chaseTimer >= timeBeforeFatigue)
            BecomeTired();
    }

    private void BecomeTired()
    {
        isTired = true;

        if (AgentReady())
            agent.speed = tiredChaseSpeed;

        if (tiredIcon != null)
            tiredIcon.SetActive(true);
    }

    public void ResetFatigue()
    {
        chaseTimer = 0f;
        isTired = false;

        if (tiredIcon != null)
            tiredIcon.SetActive(false);

        SetPatrolSpeed();
    }

    public void FaceDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
            return;

        facingMode = FacingMode.LookAtPosition;
        lookPosition = transform.position + (Vector3)direction;
        lookTarget = null;

        activeConeAngleOffset = globalConeAngleOffset;
        activeTurnDirection = EnemyWaypointLook.TurnDirection.Shortest;

        SetTargetAngleFromDirection(direction);

        UpdateAnimation();
    }

    public void FacePosition(Vector3 targetPosition)
    {
        Vector2 direction =
            targetPosition - transform.position;

        if (direction.sqrMagnitude < 0.01f)
            return;

        facingMode = FacingMode.LookAtPosition;
        lookPosition = targetPosition;
        lookTarget = null;

        activeConeAngleOffset = globalConeAngleOffset;
        activeTurnDirection = EnemyWaypointLook.TurnDirection.Shortest;

        SetTargetAngleFromDirection(direction);

        UpdateAnimation();
    }

    public void FaceTarget(Transform target)
    {
        if (target == null)
            return;

        facingMode = FacingMode.LookAtTarget;
        lookTarget = target;

        activeConeAngleOffset = globalConeAngleOffset;
        activeTurnDirection = EnemyWaypointLook.TurnDirection.Shortest;

        SetTargetAngleFromDirection(
            target.position - transform.position
        );

        UpdateAnimation();
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Gizmos.color = Color.black;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
                continue;

            Gizmos.DrawSphere(
                waypoints[i].position,
                0.15f
            );

            int nextIndex = (i + 1) % waypoints.Length;

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
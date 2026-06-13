using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{


    [Header("Fatigue")]
    [SerializeField] private float timeBeforeFatigue = 3f;
    [SerializeField] private float tiredChaseSpeed = 4.5f;
    [SerializeField] private GameObject tiredIcon;

    private float chaseTimer;
    private bool isTired;


    [Header("Chase Formation")]
    [SerializeField] private float chaseSpacing = 0.6f;

    private int chaseRank = 1;
    


    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private PatrolPathFeedback patrolPathFeedback;
    public Transform[] Waypoints => waypoints;


    [SerializeField] private float waitTime = 2f;

    [SerializeField] private float rotationSpeed = 180f;

    [Header("Speed")]
    [SerializeField] private float chaseSpeed = 8f;


    [SerializeField] private float patrolSpeed = 3.5f;
    private NavMeshAgent agent;

    

    private int currentWaypoint;

    private bool isWaiting;

    public bool IsWaiting => isWaiting;


    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.updateRotation = false;
        agent.updateUpAxis = false;

        spriteRenderer =
            GetComponent<SpriteRenderer>();
        if (patrolPathFeedback == null)
            patrolPathFeedback = GetComponent<PatrolPathFeedback>();
        
        if (tiredIcon != null)
            tiredIcon.SetActive(false);
    }
    


    public void SetChaseSpeed()
    {
        if (agent != null)
            agent.speed = chaseSpeed;
    }


    public void SetChaseRank(int rank)
    {
        chaseRank = Mathf.Max(1, rank);
    }


    public void SetPatrolSpeed()
    {
        if (agent != null)
            agent.speed = patrolSpeed;
    }

    public bool ReachedDestination()
    {
        if (agent.pathPending)
            return false;

        return agent.remainingDistance <=
            agent.stoppingDistance + 0.1f;
    }

    public void MoveTo(Vector3 target)
    {
        agent.isStopped = false;
        agent.SetDestination(target);
    }

    public void ChaseTarget(Transform target)
    {
        if (target == null)
            return;

        agent.isStopped = false;

        int spacingIndex = chaseRank - 1;

        Vector3 directionFromTargetToEnemy =
            (transform.position - target.position).normalized;

        Vector3 chasePosition =
            target.position +
            directionFromTargetToEnemy * chaseSpacing * spacingIndex;

        agent.SetDestination(chasePosition);
    }
    private void Update()
    {
        RotateTowardsMovement();
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
                0.15f);

            int nextIndex = (i + 1) % waypoints.Length;

            if (waypoints[nextIndex] != null)
            {
                Gizmos.DrawLine(
                    waypoints[i].position,
                    waypoints[nextIndex].position);
            }
        }
    }


    public IEnumerator PatrolRoutine()
    {
        SetPatrolSpeed();

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
                patrolPathFeedback.StartSegment(
                    startWaypointIndex,
                    targetWaypointIndex);

            agent.isStopped = false;
            agent.SetDestination(waypoints[targetWaypointIndex].position);

            while (agent.pathPending ||
                agent.remainingDistance > agent.stoppingDistance)
            {
                yield return null;
            }

            agent.isStopped = true;

            yield return RotateToWaypoint(waypoints[targetWaypointIndex]);

            isWaiting = true;

            yield return new WaitForSeconds(waitTime);

            isWaiting = false;

            currentWaypoint = targetWaypointIndex;
        }
    }
    public void StopMovement()
    {
        if (agent == null)
            return;

        if (!agent.enabled)
            return;

        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    private void RotateTowardsMovement()
    {

        if (agent.isStopped)
            return;
        Vector2 velocity =
            agent.desiredVelocity.normalized;

        if (velocity.sqrMagnitude < 0.01f)
            return;

        float angle =
            Mathf.Atan2(
                velocity.y,
                velocity.x)
            * Mathf.Rad2Deg;

        Quaternion targetRotation =
            Quaternion.Euler(0, 0, angle);

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
    }

    public IEnumerator RotateToWaypoint(Transform waypoint)
    {
        
        Quaternion targetRotation =
            Quaternion.Euler(
                0f,
                0f,
                waypoint.eulerAngles.z);

        while (
            Quaternion.Angle(
                transform.rotation,
                targetRotation) > 1f)
        {
            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime);

            yield return null;
        }

        transform.rotation = targetRotation;
    }

    /*  
        Fonction pour  fatigue 
    
    */

    public void StartChaseFatigue()
    {
        chaseTimer = 0f;
        isTired = false;

        if (tiredIcon != null)
            tiredIcon.SetActive(false);

        SetChaseSpeed();
    }

    public void UpdateChaseFatigue()
    {
        if (isTired)
            return;

        chaseTimer += Time.deltaTime;

        if (chaseTimer >= timeBeforeFatigue)
        {
            BecomeTired();
        }
    }

    private void BecomeTired()
    {
        isTired = true;

        if (agent != null)
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
}
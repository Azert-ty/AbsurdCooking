using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;

    [SerializeField] private float waitTime = 2f;

    [SerializeField] private float rotationSpeed = 180f;

    private NavMeshAgent agent;

    

    private int currentWaypoint;

    private bool isWaiting;

    public bool IsWaiting => isWaiting;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.updateRotation = false;
        agent.updateUpAxis = false;
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
    private void Update()
    {
        RotateTowardsMovement();
    }

    public IEnumerator PatrolRoutine()
    {
        while (true)
        {
            agent.isStopped = false;

            agent.SetDestination(
                waypoints[currentWaypoint].position);

            while (agent.pathPending ||
                  agent.remainingDistance >
                  agent.stoppingDistance)
            {
                yield return null;
            }

            agent.isStopped = true;

            isWaiting = true;

            yield return new WaitForSeconds(waitTime);

            isWaiting = false;

            currentWaypoint =
                (currentWaypoint + 1)
                % waypoints.Length;
        }
    }

    public void StopMovement()
    {
        agent.isStopped = true;
    }

    private void RotateTowardsMovement()
    {
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
}
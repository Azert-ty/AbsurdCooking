using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform playerVisionPoint;

    [Header("Vision")]
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float visionAngle = 30f;

    [Header("Obstacles")]
    [SerializeField] private LayerMask obstacleMask;

    public Transform Player => player;

    public Vector2 LastKnownDirection
    {
        get;
        private set;
    }

    public Vector3 LastKnownPlayerPosition
    {
        get;
        private set;
    }

    public bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Transform target =
            playerVisionPoint != null ? playerVisionPoint : player;

        Vector2 directionToTarget =
            target.position - transform.position;

        float sqrDistance = directionToTarget.sqrMagnitude;

        if (sqrDistance > detectionRange * detectionRange)
            return false;

        Vector2 forward = transform.right;

        float dot =
            Vector2.Dot(
                forward,
                directionToTarget.normalized);

        if (dot < Mathf.Cos(visionAngle * Mathf.Deg2Rad))
            return false;

        float distance =
            Mathf.Sqrt(sqrDistance);

        RaycastHit2D wallHit =
            Physics2D.Raycast(
                transform.position,
                directionToTarget.normalized,
                distance,
                obstacleMask);

        Debug.DrawRay(
            transform.position,
            directionToTarget.normalized * distance,
            wallHit.collider == null ? Color.green : Color.red);

        if (wallHit.collider != null)
            return false;

        LastKnownPlayerPosition = target.position;

        LastKnownDirection =
            ((Vector2)target.position -
             (Vector2)transform.position).normalized;

        return true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange);

        Vector2 forward = transform.right;

        Vector2 left =
            Quaternion.Euler(0, 0, visionAngle) * forward;

        Vector2 right =
            Quaternion.Euler(0, 0, -visionAngle) * forward;

        Gizmos.color = Color.red;

        Gizmos.DrawLine(
            transform.position,
            transform.position + (Vector3)left * detectionRange);

        Gizmos.DrawLine(
            transform.position,
            transform.position + (Vector3)right * detectionRange);
    }
}
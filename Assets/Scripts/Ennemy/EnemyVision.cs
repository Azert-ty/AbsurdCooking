using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Vision")]
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float visionAngle = 30f;

    [SerializeField] private LayerMask visionMask;

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
        
        Vector2 directionToPlayer =
            player.position - transform.position;

        float sqrDistance = directionToPlayer.sqrMagnitude;

        if (sqrDistance > detectionRange * detectionRange)
            return false;

        Vector2 forward = transform.right;

        float dot =
            Vector2.Dot(
                forward,
                directionToPlayer.normalized);

        if (dot < Mathf.Cos(visionAngle * Mathf.Deg2Rad))
            return false;

        RaycastHit2D hit =
            Physics2D.Raycast(
                transform.position,
                directionToPlayer.normalized,
                Mathf.Sqrt(sqrDistance),
                visionMask);

        if (hit.collider == null)
            return false;
        if (hit.collider.CompareTag("Player"))
        {
            LastKnownPlayerPosition = player.position;
            LastKnownDirection =
            ((Vector2)player.position -
            (Vector2)transform.position).normalized;
            return true;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange);

        Vector2 forward = transform.right;

        Vector2 left =
            Quaternion.Euler(0, 0, visionAngle)
            * forward;

        Vector2 right =
            Quaternion.Euler(0, 0, -visionAngle)
            * forward;

        Gizmos.color = Color.red;

        Gizmos.DrawLine(
            transform.position,
            transform.position + (Vector3)left * detectionRange);

        Gizmos.DrawLine(
            transform.position,
            transform.position + (Vector3)right * detectionRange);
    }
}
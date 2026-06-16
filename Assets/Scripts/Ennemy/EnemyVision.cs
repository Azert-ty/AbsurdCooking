using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform playerVisionPoint;

    [Header("Vision Origin")]
    [Tooltip("Point depuis lequel l'ennemi voit. Idéalement, mets ici le même pivot que ton cône de vision.")]
    [SerializeField] private Transform visionOrigin;

    [Header("Vision")]
    [SerializeField] private float detectionRange = 5f;

    [Tooltip("Angle de demi-vision. 30 signifie 30 degrés à gauche + 30 degrés à droite.")]
    [SerializeField] private float visionAngle = 30f;

    [Header("Obstacles")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("Debug")]
    [SerializeField] private bool drawDebugRays = true;
    [SerializeField] private bool drawGizmos = true;

    public Transform Player => player;

    public Vector2 LastKnownDirection { get; private set; } = Vector2.down;
    public Vector3 LastKnownPlayerPosition { get; private set; }

    private void Awake()
    {
        if (visionOrigin == null)
            visionOrigin = transform;

        if (player != null)
        {
            Transform target = GetTargetPoint();
            LastKnownPlayerPosition = target.position;
        }
        else
        {
            LastKnownPlayerPosition = transform.position;
        }
    }

    public bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Transform origin = GetOrigin();
        Transform target = GetTargetPoint();

        Vector2 directionToTarget =
            target.position - origin.position;

        float sqrDistance =
            directionToTarget.sqrMagnitude;

        if (sqrDistance < 0.001f)
            return true;

        if (sqrDistance > detectionRange * detectionRange)
            return false;

        Vector2 directionNormalized =
            directionToTarget.normalized;

        Vector2 forward =
            GetForwardDirection();

        float dot =
            Vector2.Dot(forward, directionNormalized);

        float minDot =
            Mathf.Cos(visionAngle * Mathf.Deg2Rad);

        if (dot < minDot)
        {
            if (drawDebugRays)
            {
                Debug.DrawRay(
                    origin.position,
                    directionNormalized * Mathf.Sqrt(sqrDistance),
                    Color.gray
                );
            }

            return false;
        }

        if (IsBlockedByObstacle(origin.position, directionNormalized, Mathf.Sqrt(sqrDistance)))
            return false;

        SavePlayerData(target.position, origin.position);

        if (drawDebugRays)
        {
            Debug.DrawRay(
                origin.position,
                directionNormalized * Mathf.Sqrt(sqrDistance),
                Color.green
            );
        }

        return true;
    }

    public bool HasLineOfSightToPlayer()
    {
        if (player == null)
            return false;

        Transform origin = GetOrigin();
        Transform target = GetTargetPoint();

        Vector2 directionToTarget =
            target.position - origin.position;

        float sqrDistance =
            directionToTarget.sqrMagnitude;

        if (sqrDistance < 0.001f)
            return true;

        if (sqrDistance > detectionRange * detectionRange)
            return false;

        float distance =
            Mathf.Sqrt(sqrDistance);

        Vector2 directionNormalized =
            directionToTarget.normalized;

        if (IsBlockedByObstacle(origin.position, directionNormalized, distance))
            return false;

        SavePlayerData(target.position, origin.position);

        if (drawDebugRays)
        {
            Debug.DrawRay(
                origin.position,
                directionNormalized * distance,
                Color.yellow
            );
        }

        return true;
    }

    private Transform GetOrigin()
    {
        if (visionOrigin != null)
            return visionOrigin;

        return transform;
    }

    private Transform GetTargetPoint()
    {
        if (playerVisionPoint != null)
            return playerVisionPoint;

        return player;
    }

    private Vector2 GetForwardDirection()
    {
        Transform origin = GetOrigin();

        Vector2 forward = origin.right;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector2.right;

        return forward.normalized;
    }

    private bool IsBlockedByObstacle(
        Vector2 originPosition,
        Vector2 direction,
        float distance)
    {
        RaycastHit2D wallHit =
            Physics2D.Raycast(
                originPosition,
                direction,
                distance,
                obstacleMask
            );

        if (wallHit.collider != null)
        {
            if (drawDebugRays)
            {
                Debug.DrawRay(
                    originPosition,
                    direction * distance,
                    Color.red
                );
            }

            return true;
        }

        return false;
    }

    private void SavePlayerData(
        Vector3 targetPosition,
        Vector3 originPosition)
    {
        LastKnownPlayerPosition = targetPosition;

        Vector2 direction =
            targetPosition - originPosition;

        if (direction.sqrMagnitude > 0.001f)
            LastKnownDirection = direction.normalized;
    }

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;

        if (player != null)
        {
            Transform target = GetTargetPoint();
            LastKnownPlayerPosition = target.position;
        }
    }

    public void SetPlayerVisionPoint(Transform newPlayerVisionPoint)
    {
        playerVisionPoint = newPlayerVisionPoint;
    }

    public float GetDetectionRange()
    {
        return detectionRange;
    }

    public float GetVisionHalfAngle()
    {
        return visionAngle;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        Transform origin =
            visionOrigin != null ? visionOrigin : transform;

        Vector2 forward = origin.right;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector2.right;

        forward.Normalize();

        Gizmos.color = Color.blue;

        Gizmos.DrawWireSphere(
            origin.position,
            detectionRange
        );

        Vector2 leftDirection =
            Quaternion.Euler(0f, 0f, visionAngle) * forward;

        Vector2 rightDirection =
            Quaternion.Euler(0f, 0f, -visionAngle) * forward;

        Gizmos.color = Color.red;

        Gizmos.DrawLine(
            origin.position,
            origin.position + (Vector3)leftDirection * detectionRange
        );

        Gizmos.DrawLine(
            origin.position,
            origin.position + (Vector3)rightDirection * detectionRange
        );

        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(
            origin.position,
            origin.position + (Vector3)forward * detectionRange
        );
    }
}
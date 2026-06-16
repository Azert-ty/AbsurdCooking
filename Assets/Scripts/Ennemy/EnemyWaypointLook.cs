using UnityEngine;

public class EnemyWaypointLook : MonoBehaviour
{
    public enum LookDirection
    {
        Right,
        Up,
        Left,
        Down,
        CustomAngle
    }

    public enum TurnDirection
    {
        Shortest,
        Clockwise,
        CounterClockwise
    }

    [Header("Look Direction At This Waypoint")]
    [SerializeField] private LookDirection lookDirection = LookDirection.Right;

    [Tooltip("Utilisé seulement si Look Direction = Custom Angle.")]
    [SerializeField] private float customAngle = 0f;

    [Header("Turn Direction At This Waypoint")]
    [Tooltip("Sens de rotation utilisé quand l'ennemi arrive à ce waypoint.")]
    [SerializeField] private TurnDirection turnDirection = TurnDirection.Shortest;

    [Header("Exit Turn Direction")]
    [Tooltip("Active un sens de rotation spécial quand l'ennemi quitte ce waypoint.")]
    [SerializeField] private bool overrideExitTurnDirection = false;

    [Tooltip("Sens de rotation utilisé quand l'ennemi quitte ce waypoint.")]
    [SerializeField] private TurnDirection exitTurnDirection = TurnDirection.Shortest;

    [Header("Cone Correction")]
    [Tooltip("Correction locale du cône pour ce waypoint. Laisse 0 si ton cône est déjà bon.")]
    [SerializeField] private float waypointConeAngleOffset = 0f;

    public float GetLookAngle()
    {
        switch (lookDirection)
        {
            case LookDirection.Right:
                return 0f;

            case LookDirection.Up:
                return 90f;

            case LookDirection.Left:
                return 180f;

            case LookDirection.Down:
                return 270f;

            case LookDirection.CustomAngle:
                return customAngle;

            default:
                return 0f;
        }
    }

    public TurnDirection GetTurnDirection()
    {
        return turnDirection;
    }

    public bool OverrideExitTurnDirection()
    {
        return overrideExitTurnDirection;
    }

    public TurnDirection GetExitTurnDirection()
    {
        return exitTurnDirection;
    }

    public float GetWaypointConeAngleOffset()
    {
        return waypointConeAngleOffset;
    }
}
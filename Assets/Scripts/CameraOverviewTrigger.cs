using System.Collections;
using UnityEngine;

public class CameraOverviewTrigger : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera overviewCamera;

    [Header("Systems")]
    [SerializeField] private PlayerMovement playerController;
    [SerializeField] private CameraFollow cameraFollow;

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 1.2f;
    [SerializeField] private float curveSideOffset = 2.5f;
    [SerializeField] private AnimationCurve transitionCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool isOverviewActive;
    private bool isTransitioning;

    private Vector3 playerCameraOriginalPosition;
    private float playerCameraOriginalSize;

    private Coroutine transitionCoroutine;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera != null && cameraFollow == null)
            cameraFollow = playerCamera.GetComponent<CameraFollow>();
    }

    private void Start()
    {
        if (overviewCamera != null)
            overviewCamera.enabled = false;
    }

    public void ToggleOverview()
    {
        if (isTransitioning)
            return;

        if (!isOverviewActive)
            EnterOverview();
        else
            ExitOverview();
    }

    private void EnterOverview()
    {
        if (playerCamera == null || overviewCamera == null)
            return;

        isOverviewActive = true;

        playerCameraOriginalPosition = playerCamera.transform.position;
        playerCameraOriginalSize = playerCamera.orthographicSize;

        if (playerController != null)
            playerController.SetInputEnabled(false);

        if (cameraFollow != null)
            cameraFollow.enabled = false;

        overviewCamera.enabled = false;
        playerCamera.enabled = true;

        StartCameraTransition(
            playerCamera.transform.position,
            overviewCamera.transform.position,
            playerCamera.orthographicSize,
            overviewCamera.orthographicSize
        );
    }

    private void ExitOverview()
    {
        if (playerCamera == null)
            return;

        isOverviewActive = false;

        StartCameraTransition(
            playerCamera.transform.position,
            playerCameraOriginalPosition,
            playerCamera.orthographicSize,
            playerCameraOriginalSize,
            OnExitTransitionFinished
        );
    }

    private void StartCameraTransition(
        Vector3 startPosition,
        Vector3 endPosition,
        float startSize,
        float endSize,
        System.Action onComplete = null)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(
            CameraTransitionRoutine(
                startPosition,
                endPosition,
                startSize,
                endSize,
                onComplete
            )
        );
    }

    private IEnumerator CameraTransitionRoutine(
        Vector3 startPosition,
        Vector3 endPosition,
        float startSize,
        float endSize,
        System.Action onComplete)
    {
        isTransitioning = true;

        float timer = 0f;

        Vector3 controlPointA;
        Vector3 controlPointB;

        CalculateBezierControlPoints(
            startPosition,
            endPosition,
            out controlPointA,
            out controlPointB
        );

        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;

            float rawT = timer / transitionDuration;
            float curvedT = transitionCurve.Evaluate(rawT);

            Vector3 newPosition =
                GetCubicBezierPoint(
                    curvedT,
                    startPosition,
                    controlPointA,
                    controlPointB,
                    endPosition
                );

            playerCamera.transform.position = newPosition;

            playerCamera.orthographicSize =
                Mathf.Lerp(startSize, endSize, curvedT);

            yield return null;
        }

        playerCamera.transform.position = endPosition;
        playerCamera.orthographicSize = endSize;

        isTransitioning = false;
        transitionCoroutine = null;

        onComplete?.Invoke();
    }

    private void OnExitTransitionFinished()
    {
        if (cameraFollow != null)
            cameraFollow.enabled = true;

        if (playerController != null)
            playerController.SetInputEnabled(true);
    }

    private void CalculateBezierControlPoints(
        Vector3 startPosition,
        Vector3 endPosition,
        out Vector3 controlPointA,
        out Vector3 controlPointB)
    {
        Vector3 direction = endPosition - startPosition;
        direction.z = 0f;

        Vector3 perpendicular =
            new Vector3(-direction.y, direction.x, 0f).normalized;

        if (perpendicular.sqrMagnitude < 0.01f)
            perpendicular = Vector3.right;

        Vector3 offset = perpendicular * curveSideOffset;

        controlPointA =
            startPosition +
            direction * 0.33f +
            offset;

        controlPointB =
            startPosition +
            direction * 0.66f +
            offset;

        controlPointA.z = Mathf.Lerp(startPosition.z, endPosition.z, 0.33f);
        controlPointB.z = Mathf.Lerp(startPosition.z, endPosition.z, 0.66f);
    }

    private Vector3 GetCubicBezierPoint(
        float t,
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3)
    {
        float oneMinusT = 1f - t;

        return
            oneMinusT * oneMinusT * oneMinusT * p0 +
            3f * oneMinusT * oneMinusT * t * p1 +
            3f * oneMinusT * t * t * p2 +
            t * t * t * p3;
    }
}
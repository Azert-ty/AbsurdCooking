using System.Collections;
using UnityEngine;

public class ExitDoorCutscene : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform doorFocusPoint;
    [SerializeField] private AnimatedExitDoor exitDoor;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Camera Follow Script")]
    [SerializeField] private CameraFollow cameraFollow;

    [Header("Enemies To Pause")]
    [SerializeField] private EnemyAI[] enemiesToPause;

    [Header("Camera Movement")]
    [SerializeField] private float cameraMoveSpeed = 6f;
    [SerializeField] private float arriveDistance = 0.05f;

    [Header("Timing")]
    [SerializeField] private float waitBeforeDoorOpen = 0.4f;
    [SerializeField] private float waitAfterDoorOpen = 1.2f;

    private bool cutsceneStarted;
    private Vector3 originalCameraPosition;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (cameraFollow == null && mainCamera != null)
            cameraFollow = mainCamera.GetComponent<CameraFollow>();
    }

    private void Update()
    {
        if (cutsceneStarted)
            return;

        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.HasObjective())
        {
            StartCoroutine(PlayCutscene());
        }
    }

    private IEnumerator PlayCutscene()
    {
        cutsceneStarted = true;

        StopPlayer();
        StopEnemies();
        DisableCameraFollow();

        originalCameraPosition = mainCamera.transform.position;

        Vector3 doorCameraPosition =
            new Vector3(
                doorFocusPoint.position.x,
                doorFocusPoint.position.y,
                originalCameraPosition.z);

        yield return MoveCameraTo(doorCameraPosition);

        yield return new WaitForSeconds(waitBeforeDoorOpen);

        if (exitDoor != null)
            exitDoor.OpenDoor();

        yield return new WaitForSeconds(waitAfterDoorOpen);

        yield return MoveCameraTo(originalCameraPosition);

        EnableCameraFollow();
        ResumeEnemies();
        ResumePlayer();
    }

    private IEnumerator MoveCameraTo(Vector3 targetPosition)
    {
        while (Vector3.Distance(mainCamera.transform.position, targetPosition) > arriveDistance)
        {
            mainCamera.transform.position =
                Vector3.MoveTowards(
                    mainCamera.transform.position,
                    targetPosition,
                    cameraMoveSpeed * Time.deltaTime);

            yield return null;
        }

        mainCamera.transform.position = targetPosition;
    }

    private void StopPlayer()
    {
        if (playerMovement != null)
            playerMovement.SetInputEnabled(false);
    }

    private void ResumePlayer()
    {
        if (playerMovement != null)
            playerMovement.SetInputEnabled(true);
    }

    private void DisableCameraFollow()
    {
        if (cameraFollow != null)
            cameraFollow.enabled = false;
    }

    private void EnableCameraFollow()
    {
        if (cameraFollow != null)
            cameraFollow.enabled = true;
    }

    private void StopEnemies()
    {
        foreach (EnemyAI enemy in enemiesToPause)
        {
            if (enemy != null)
                enemy.PauseAI();
        }
    }

    private void ResumeEnemies()
    {
        foreach (EnemyAI enemy in enemiesToPause)
        {
            if (enemy != null)
                enemy.ResumeAI();
        }
    }
}
using UnityEngine;

public class CameraOverviewTrigger : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera overviewCamera;

    [Header("Systems")]
    [SerializeField] private PlayerMovement playerController;

    private bool isOverviewActive;

    private void Start()
    {
        overviewCamera.enabled = false;
    }

    public void ToggleOverview()
    {
        if (!isOverviewActive)
            EnterOverview();
        else
            ExitOverview();
    }

    private void EnterOverview()
    {
        isOverviewActive = true;

        if (playerController != null)
            playerController.SetInputEnabled(false);

        playerCamera.enabled = false;
        overviewCamera.enabled = true;
    }

    private void ExitOverview()
    {
        isOverviewActive = false;

        overviewCamera.enabled = false;
        playerCamera.enabled = true;

        if (playerController != null)
            playerController.SetInputEnabled(true);
    }
}
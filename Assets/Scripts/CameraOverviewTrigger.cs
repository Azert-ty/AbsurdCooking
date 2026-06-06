// using UnityEngine;

// public class CameraOverviewTrigger : MonoBehaviour
// {
//     [SerializeField] private Camera playerCamera;
//     [SerializeField] private Camera overviewCamera;
//     [SerializeField] private Transform player;
//     [SerializeField] private Vector3 offset = new Vector3(0, 30f, 0);

//     [Header("Systems")]
//     [SerializeField] private PlayerMovement playerController;

//     private bool isOverviewActive;

//     public void ToggleOverview()
//     {
//         if (!isOverviewActive)
//             EnterOverview();
//         else
//             ExitOverview();
//     }

//     private void EnterOverview()
//     {
//         isOverviewActive = true;

//         if (playerController != null)
//             playerController.SetInputEnabled(false);

//         playerCamera.enabled = false;
//         overviewCamera.enabled = true;

//         UpdateOverviewCamera();
//     }

//     private void ExitOverview()
//     {
//         isOverviewActive = false;

//         overviewCamera.enabled = false;
//         playerCamera.enabled = true;

//         if (playerController != null)
//             playerController.SetInputEnabled(true);
//     }

//     private void Update()
//     {
//         if (!isOverviewActive) return;

//         UpdateOverviewCamera();
//     }

//     private void UpdateOverviewCamera()
//     {
//         overviewCamera.transform.position = player.position + offset;
//         overviewCamera.transform.LookAt(player.position);
//     }
// }

using UnityEngine;

public class CameraOverviewTrigger : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera overviewCamera;
    [SerializeField] private Transform player;
    
    [Header("Configuration 2D")]
    // En 2D, on utilise une caméra orthographique. Pour dézoomer, on augmente sa taille.
    [SerializeField] private float overviewCameraSize = 15f; 
    // On garde un offset sur Z (souvent -10) pour que la caméra reste en retrait par rapport aux sprites
    [SerializeField] private float cameraZOffset = -10f; 

    [Header("Systems")]
    [SerializeField] private PlayerMovement playerController;

    private bool isOverviewActive;

    private void Start()
    {
        if (overviewCamera != null) 
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
        if (player == null) return;

        isOverviewActive = true;

        if (playerController != null)
            playerController.SetInputEnabled(false);

        playerCamera.enabled = false;
        overviewCamera.enabled = true;

        // On configure la taille de zoom de la caméra d'ensemble
        overviewCamera.orthographic = true;
        overviewCamera.orthographicSize = overviewCameraSize;

        UpdateOverviewCamera();
    }

    private void ExitOverview()
    {
        isOverviewActive = false;

        overviewCamera.enabled = false;
        playerCamera.enabled = true;

        if (playerController != null)
            playerController.SetInputEnabled(true);
    }

    private void LateUpdate()
    {
        if (!isOverviewActive) return;

        UpdateOverviewCamera();
    }

    private void UpdateOverviewCamera()
    {
        if (player == null) return;
        
        // En 2D, la caméra suit les X et Y du joueur, mais reste à une distance Z de sécurité
        Vector3 targetPosition = new Vector3(player.position.x, player.position.y, cameraZOffset);
        overviewCamera.transform.position = targetPosition;
    }
}
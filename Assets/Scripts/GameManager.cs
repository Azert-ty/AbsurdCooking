

// using UnityEngine;
// using UnityEngine.SceneManagement;
// using UnityEngine.InputSystem;

// public class GameManager : MonoBehaviour
// {


//     [Header("Sounds")]
//     [SerializeField] private AudioClip victorySound;
//     [SerializeField] private AudioClip gameOverSound;
//     [SerializeField] private AudioSource audioSource;
//     public static GameManager Instance { get; private set; }

//     private bool objectiveCollected;
//     private bool gameEnded;

//     public bool ObjectiveCollected => objectiveCollected;
//     public bool GameEnded => gameEnded;

//     private void Awake()
//     {
//         if (Instance != null && Instance != this)
//         {
//             Destroy(gameObject);
//             return;
//         }
        
//         Instance = this;
//         if (audioSource == null)
//             audioSource = GetComponent<AudioSource>();
//         Time.timeScale = 1f;
//     }

//     private void Update()
//     {
//         if (!gameEnded)
//             return;

//         if (Keyboard.current == null)
//             return;

//         if (Keyboard.current.rKey.wasPressedThisFrame)
//         {
//             RestartGame();
//         }
//     }

//     public void SetObjectiveCollected()
//     {
//         if (objectiveCollected)
//             return;

//         objectiveCollected = true;

//         Debug.Log("Trésor récupéré !");
//     }

//     public bool HasObjective()
//     {
//         return objectiveCollected;
//     }

//     public void Victory()
//     {
//         if (gameEnded)
//             return;

//         gameEnded = true;

//         Debug.Log("VICTORY");

//         PlaySound(victorySound);

//         if (GameUI.Instance != null)
//         {
//             GameUI.Instance.ShowVictoryPanel();
//         }

//         Time.timeScale = 0f;
//     }

//     public void GameOver()
//     {
//         if (gameEnded)
//             return;

//         gameEnded = true;

//         Debug.Log("GAME OVER");

//         PlaySound(gameOverSound);

//         if (GameUI.Instance != null)
//         {
//             GameUI.Instance.ShowGameOverPanel();
//         }

//         Time.timeScale = 0f;
//     }

//     private void PlaySound(AudioClip clip)
//     {
//         if (clip == null)
//             return;

//         if (audioSource != null)
//         {
//             audioSource.PlayOneShot(clip);
//         }
//         else
//         {
//             AudioSource.PlayClipAtPoint(
//                 clip,
//                 Camera.main.transform.position);
//         }
//     }
//     public void RestartGame()
//     {
//         Time.timeScale = 1f;

//         SceneManager.LoadScene(
//             SceneManager.GetActiveScene().buildIndex);
//     }
// }

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Objects To Disable On End")]
    [SerializeField] private GameObject hudObject;
    [SerializeField] private GameObject cameraEmptyObject;

    [Header("Victory Stars")]
    [SerializeField] private float timeLimitForStar = 120f;

    private float gameTimer;
    private bool playerWasSeen;

    public float GameTimer => gameTimer;
    public float TimeLimitForStar => timeLimitForStar;
    public bool PlayerWasSeen => playerWasSeen;

    public static GameManager Instance { get; private set; }

    private bool objectiveCollected;
    private bool gameEnded;

    public bool ObjectiveCollected => objectiveCollected;
    public bool GameEnded => gameEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        Time.timeScale = 1f;
    }

    // private void Update()
    // {
    //     if (!gameEnded)
    //         return;

    //     if (Keyboard.current == null)
    //         return;

    //     if (Keyboard.current.rKey.wasPressedThisFrame)
    //     {
    //         RestartGame();
    //     }
    // }

    private void Update()
    {
        if (gameEnded)
            return;

        gameTimer += Time.deltaTime;
    }

    public void RegisterPlayerSeen()
    {
        playerWasSeen = true;
    }

    public void SetObjectiveCollected()
    {
        if (objectiveCollected)
            return;

        objectiveCollected = true;

        Debug.Log("Trésor récupéré !");
    }

    public bool HasObjective()
    {
        return objectiveCollected;
    }

    public void Victory()
    {
        if (gameEnded)
            return;

        gameEnded = true;

        Debug.Log("VICTORY");

        PlaySound(victorySound);

        DisableEndObjects();

        if (GameUI.Instance != null)
        {
            GameUI.Instance.ShowVictoryPanel();
        }

        Time.timeScale = 0f;
    }

    public void GameOver()
    {
        if (gameEnded)
            return;

        gameEnded = true;

        Debug.Log("GAME OVER");

        PlaySound(gameOverSound);

        DisableEndObjects();

        if (GameUI.Instance != null)
        {
            GameUI.Instance.ShowGameOverPanel();
        }

        Time.timeScale = 0f;
    }

    private void DisableEndObjects()
    {
        if (hudObject != null)
            hudObject.SetActive(false);

        if (cameraEmptyObject != null)
            cameraEmptyObject.SetActive(false);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
            return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else if (Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(
                clip,
                Camera.main.transform.position);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex);
    }
}
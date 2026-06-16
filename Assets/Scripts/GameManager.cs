using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private AudioSource audioSource;

    [SerializeField, Range(0f, 1f)]
    private float victorySoundVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float gameOverSoundVolume = 1f;

    [Header("Objects To Disable On End")]
    [SerializeField] private GameObject hudObject;
    [SerializeField] private GameObject cameraEmptyObject;

    [Header("Game Over Capture Effect")]
    [SerializeField] private GameObject captureCloudPrefab;
    [SerializeField] private float captureEffectDuration = 1.3f;
    [SerializeField] private bool hidePlayerDuringCapture = true;
    [SerializeField] private bool hideEnemyDuringCapture = true;

    [Header("Capture Sound")]
    [SerializeField] private AudioClip captureSound;

    [SerializeField, Range(0f, 1f)]
    private float captureSoundVolume = 1f;

    [Header("Victory Cinematic")]
    [SerializeField] private float victoryRunDuration = 1.5f;
    [SerializeField] private float victoryRunSpeed = 4.5f;

    [Tooltip("Direction de course pendant la cinématique de victoire. Pour Walk_Right : X = 1, Y = 0.")]
    [SerializeField] private Vector2 victoryRunDirection = Vector2.right;

    [SerializeField] private Animator playerAnimator;

    [Header("Gameplay References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private GameObject playerVisualObject;
    [SerializeField] private EnemyAI[] enemiesToPauseOnGameOver;
    [SerializeField] private EnemyAI[] enemiesToPauseOnVictory;

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

    private GameObject capturedEnemyVisualObject;

    private bool playerMovementWasEnabled;

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

        if (playerAnimator == null && playerMovement != null)
            playerAnimator = playerMovement.GetComponentInChildren<Animator>();

        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!gameEnded)
        {
            gameTimer += Time.deltaTime;
            return;
        }

        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartGame();
        }
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

        StartCoroutine(VictoryRoutine());
    }

    private IEnumerator VictoryRoutine()
    {
        gameEnded = true;

        Debug.Log("VICTORY CINEMATIC START");

        if (GameAudioManager.Instance != null)
            GameAudioManager.Instance.StopAllGameplayAudio();

        PreparePlayerForVictoryCinematic();

        PauseVictoryEnemies();

        if (hudObject != null)
            hudObject.SetActive(false);

        yield return RunPlayerVictoryCinematic();

        Debug.Log("VICTORY");

        PlaySound(victorySound, victorySoundVolume);

        if (cameraEmptyObject != null)
            cameraEmptyObject.SetActive(false);

        if (GameUI.Instance != null)
            GameUI.Instance.ShowVictoryPanel();

        Time.timeScale = 0f;
    }

    private void PreparePlayerForVictoryCinematic()
    {
        if (playerMovement == null)
            return;

        playerMovement.SetInputEnabled(false);

        playerMovementWasEnabled = playerMovement.enabled;
        playerMovement.enabled = false;
    }

    private IEnumerator RunPlayerVictoryCinematic()
    {
        if (playerMovement == null)
            yield break;

        Transform playerTransform = playerMovement.transform;

        Vector2 runDirection = victoryRunDirection;

        if (runDirection.sqrMagnitude < 0.01f)
            runDirection = Vector2.right;

        runDirection.Normalize();

        float timer = 0f;

        while (timer < victoryRunDuration)
        {
            ForcePlayerWalkRightAnimation();

            Vector3 movement =
                (Vector3)(runDirection * victoryRunSpeed * Time.deltaTime);

            playerTransform.position += movement;

            timer += Time.deltaTime;
            yield return null;
        }

        ForcePlayerIdleRightAnimation();
    }

    private void ForcePlayerWalkRightAnimation()
    {
        if (playerAnimator == null)
            return;

        playerAnimator.SetBool("IsMoving", true);

        playerAnimator.SetFloat("MoveX", 1f);
        playerAnimator.SetFloat("MoveY", 0f);

        playerAnimator.SetFloat("LastX", 1f);
        playerAnimator.SetFloat("LastY", 0f);
    }

    private void ForcePlayerIdleRightAnimation()
    {
        if (playerAnimator == null)
            return;

        playerAnimator.SetBool("IsMoving", false);

        playerAnimator.SetFloat("MoveX", 0f);
        playerAnimator.SetFloat("MoveY", 0f);

        playerAnimator.SetFloat("LastX", 1f);
        playerAnimator.SetFloat("LastY", 0f);
    }

    private void PauseVictoryEnemies()
    {
        if (enemiesToPauseOnVictory == null)
            return;

        foreach (EnemyAI enemy in enemiesToPauseOnVictory)
        {
            if (enemy != null)
                enemy.PauseAI();
        }
    }

    public void GameOver()
    {
        Vector3 fallbackPosition = Vector3.zero;

        if (playerMovement != null)
            fallbackPosition = playerMovement.transform.position;

        GameOver(fallbackPosition, null);
    }

    public void GameOver(Vector3 capturePosition)
    {
        GameOver(capturePosition, null);
    }

    public void GameOver(Vector3 capturePosition, GameObject enemyVisualObject)
    {
        if (gameEnded)
            return;

        capturedEnemyVisualObject = enemyVisualObject;

        StartCoroutine(GameOverRoutine(capturePosition));
    }

    private IEnumerator GameOverRoutine(Vector3 capturePosition)
    {
        gameEnded = true;

        Debug.Log("GAME OVER CAPTURE START");

        if (GameAudioManager.Instance != null)
            GameAudioManager.Instance.StopAllGameplayAudio();

        StopGameplayForCapture();

        if (hudObject != null)
            hudObject.SetActive(false);

        SpawnCaptureCloud(capturePosition);

        PlaySound(captureSound, captureSoundVolume);

        yield return new WaitForSeconds(captureEffectDuration);

        Debug.Log("GAME OVER");

        PlaySound(gameOverSound, gameOverSoundVolume);

        if (cameraEmptyObject != null)
            cameraEmptyObject.SetActive(false);

        if (GameUI.Instance != null)
            GameUI.Instance.ShowGameOverPanel();

        Time.timeScale = 0f;
    }

    private void StopGameplayForCapture()
    {
        if (playerMovement != null)
            playerMovement.SetInputEnabled(false);

        if (hidePlayerDuringCapture && playerVisualObject != null)
            playerVisualObject.SetActive(false);

        if (hideEnemyDuringCapture && capturedEnemyVisualObject != null)
            capturedEnemyVisualObject.SetActive(false);

        if (enemiesToPauseOnGameOver == null)
            return;

        foreach (EnemyAI enemy in enemiesToPauseOnGameOver)
        {
            if (enemy != null)
                enemy.PauseAI();
        }
    }

    private void SpawnCaptureCloud(Vector3 position)
    {
        if (captureCloudPrefab == null)
            return;

        Instantiate(
            captureCloudPrefab,
            position,
            Quaternion.identity
        );
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
        else if (Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(
                clip,
                Camera.main.transform.position,
                volume
            );
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}
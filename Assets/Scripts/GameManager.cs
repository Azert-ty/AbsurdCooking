


using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    // =========================================================
    // SOUNDS
    // =========================================================

    [Header("Sounds")]
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private AudioSource audioSource;

    [SerializeField, Range(0f, 1f)]
    private float victorySoundVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float gameOverSoundVolume = 1f;

    // =========================================================
    // OBJECTS TO DISABLE
    // =========================================================

    [Header("Objects To Disable On End")]
    [SerializeField] private GameObject hudObject;
    [SerializeField] private GameObject cameraEmptyObject;

    // =========================================================
    // GAME OVER CAPTURE EFFECT
    // =========================================================

    [Header("Game Over Capture Effect")]
    [SerializeField] private GameObject captureCloudPrefab;
    [SerializeField] private float captureEffectDuration = 1.3f;
    [SerializeField] private bool hidePlayerDuringCapture = true;
    [SerializeField] private bool hideEnemyDuringCapture = true;

    // =========================================================
    // CAPTURE SOUND
    // =========================================================

    [Header("Capture Sound")]
    [SerializeField] private AudioClip captureSound;

    [SerializeField, Range(0f, 1f)]
    private float captureSoundVolume = 1f;

    // =========================================================
    // VICTORY CINEMATIC
    // =========================================================

    [Header("Victory Cinematic")]

    [Tooltip(
        "Durée pendant laquelle le joueur court vers le bas."
    )]
    [SerializeField]
    private float victoryRunDuration = 1.5f;

    [Tooltip(
        "Vitesse du joueur pendant sa course de victoire vers le bas."
    )]
    [SerializeField]
    private float victoryRunSpeed = 4.5f;

    [SerializeField]
    private Animator playerAnimator;

    // =========================================================
    // GAMEPLAY REFERENCES
    // =========================================================

    [Header("Gameplay References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private GameObject playerVisualObject;

    [SerializeField]
    private EnemyAI[] enemiesToPauseOnGameOver;

    [SerializeField]
    private EnemyAI[] enemiesToPauseOnVictory;

    // =========================================================
    // VICTORY STARS
    // =========================================================

    [Header("Victory Stars")]
    [SerializeField] private float timeLimitForStar = 120f;

    // =========================================================
    // TIMER / STATS
    // =========================================================

    private float gameTimer;
    private bool playerWasSeen;

    public float GameTimer => gameTimer;
    public float TimeLimitForStar => timeLimitForStar;
    public bool PlayerWasSeen => playerWasSeen;

    // =========================================================
    // SINGLETON
    // =========================================================

    public static GameManager Instance { get; private set; }

    // =========================================================
    // GAME STATE
    // =========================================================

    private bool objectiveCollected;
    private bool gameEnded;

    private GameObject capturedEnemyVisualObject;

    public bool ObjectiveCollected => objectiveCollected;
    public bool GameEnded => gameEnded;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (audioSource == null)
        {
            audioSource =
                GetComponent<AudioSource>();
        }

        if (playerAnimator == null &&
            playerMovement != null)
        {
            playerAnimator =
                playerMovement
                    .GetComponentInChildren<Animator>();
        }

        Time.timeScale = 1f;
    }

    private void Update()
    {
        // -----------------------------------------------------
        // TIMER
        // -----------------------------------------------------

        if (!gameEnded)
        {
            gameTimer += Time.deltaTime;
            return;
        }

        // -----------------------------------------------------
        // RESTART AVEC R
        // -----------------------------------------------------

        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartGame();
        }
    }

    // =========================================================
    // PLAYER SEEN
    // =========================================================

    public void RegisterPlayerSeen()
    {
        playerWasSeen = true;
    }

    // =========================================================
    // OBJECTIVE
    // =========================================================

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

    // =========================================================
    // VICTORY
    // =========================================================

    public void Victory()
    {
        if (gameEnded)
            return;

        StartCoroutine(
            VictoryRoutine()
        );
    }

    private IEnumerator VictoryRoutine()
    {
        gameEnded = true;

        Debug.Log(
            "VICTORY CINEMATIC START"
        );

        // -----------------------------------------------------
        // ARRÊTE LES SONS DE GAMEPLAY
        // -----------------------------------------------------

        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance
                .StopAllGameplayAudio();
        }

        // -----------------------------------------------------
        // PRÉPARE LE JOUEUR
        // -----------------------------------------------------

        PreparePlayerForVictoryCinematic();

        // -----------------------------------------------------
        // ARRÊTE LES ENNEMIS
        // -----------------------------------------------------

        PauseVictoryEnemies();

        // -----------------------------------------------------
        // CACHE LE HUD
        // -----------------------------------------------------

        if (hudObject != null)
        {
            hudObject.SetActive(false);
        }

        // -----------------------------------------------------
        // COURSE DU JOUEUR VERS LE BAS
        // -----------------------------------------------------

        yield return RunPlayerVictoryCinematic();

        // -----------------------------------------------------
        // VICTOIRE FINALE
        // -----------------------------------------------------

        Debug.Log("VICTORY");

        PlaySound(
            victorySound,
            victorySoundVolume
        );

        // -----------------------------------------------------
        // DÉSACTIVE L'OBJET CAMÉRA
        // -----------------------------------------------------

        if (cameraEmptyObject != null)
        {
            cameraEmptyObject.SetActive(false);
        }

        // -----------------------------------------------------
        // AFFICHE L'ÉCRAN DE VICTOIRE
        // -----------------------------------------------------

        if (GameUI.Instance != null)
        {
            GameUI.Instance
                .ShowVictoryPanel();
        }

        // -----------------------------------------------------
        // PAUSE LE JEU
        // -----------------------------------------------------

        Time.timeScale = 0f;
    }

    // =========================================================
    // PREPARE PLAYER FOR VICTORY
    // =========================================================

    private void PreparePlayerForVictoryCinematic()
    {
        if (playerMovement == null)
            return;

        // Désactive les entrées du joueur.
        playerMovement.SetInputEnabled(false);

        // Désactive le script de déplacement normal.
        playerMovement.enabled = false;
    }

    // =========================================================
    // VICTORY RUN DOWN
    // =========================================================

    private IEnumerator RunPlayerVictoryCinematic()
    {
        if (playerMovement == null)
            yield break;

        Transform playerTransform =
            playerMovement.transform;

        // -----------------------------------------------------
        // DIRECTION FIXE :
        //
        // BAS = X 0 / Y -1
        //
        // Plus de réglage dans l'Inspector.
        // Le joueur court toujours vers le bas.
        // -----------------------------------------------------

        Vector2 runDirection =
            Vector2.down;

        float timer = 0f;

        while (timer < victoryRunDuration)
        {
            // -------------------------------------------------
            // FORCE L'ANIMATION DE MARCHE VERS LE BAS
            // -------------------------------------------------

            ForcePlayerWalkDownAnimation();

            // -------------------------------------------------
            // DÉPLACEMENT VERS LE BAS
            // -------------------------------------------------

            Vector3 movement =
                (Vector3)(
                    runDirection *
                    victoryRunSpeed *
                    Time.deltaTime
                );

            playerTransform.position +=
                movement;

            timer += Time.deltaTime;

            yield return null;
        }

        // -----------------------------------------------------
        // FIN DE LA COURSE :
        // RESTE TOURNÉ VERS LE BAS
        // -----------------------------------------------------

        ForcePlayerIdleDownAnimation();
    }

    // =========================================================
    // WALK DOWN ANIMATION
    // =========================================================

    private void ForcePlayerWalkDownAnimation()
    {
        if (playerAnimator == null)
            return;

        // Le joueur marche.
        playerAnimator.SetBool(
            "IsMoving",
            true
        );

        // -----------------------------------------------------
        // DIRECTION ACTUELLE :
        //
        // X = 0
        // Y = -1
        //
        // Donc vers le bas.
        // -----------------------------------------------------

        playerAnimator.SetFloat(
            "MoveX",
            0f
        );

        playerAnimator.SetFloat(
            "MoveY",
            -1f
        );

        // -----------------------------------------------------
        // MÉMORISE LA DIRECTION
        // -----------------------------------------------------

        playerAnimator.SetFloat(
            "LastX",
            0f
        );

        playerAnimator.SetFloat(
            "LastY",
            -1f
        );
    }

    // =========================================================
    // IDLE DOWN ANIMATION
    // =========================================================

    private void ForcePlayerIdleDownAnimation()
    {
        if (playerAnimator == null)
            return;

        // Le joueur ne marche plus.
        playerAnimator.SetBool(
            "IsMoving",
            false
        );

        // Aucune direction de déplacement.
        playerAnimator.SetFloat(
            "MoveX",
            0f
        );

        playerAnimator.SetFloat(
            "MoveY",
            0f
        );

        // -----------------------------------------------------
        // MAIS IL RESTE TOURNÉ VERS LE BAS
        // -----------------------------------------------------

        playerAnimator.SetFloat(
            "LastX",
            0f
        );

        playerAnimator.SetFloat(
            "LastY",
            -1f
        );
    }

    // =========================================================
    // PAUSE VICTORY ENEMIES
    // =========================================================

    private void PauseVictoryEnemies()
    {
        if (enemiesToPauseOnVictory == null)
            return;

        foreach (
            EnemyAI enemy
            in enemiesToPauseOnVictory)
        {
            if (enemy != null)
            {
                enemy.PauseAI();
            }
        }
    }

    // =========================================================
    // GAME OVER
    // =========================================================

    public void GameOver()
    {
        Vector3 fallbackPosition =
            Vector3.zero;

        if (playerMovement != null)
        {
            fallbackPosition =
                playerMovement
                    .transform
                    .position;
        }

        GameOver(
            fallbackPosition,
            null
        );
    }

    public void GameOver(
        Vector3 capturePosition)
    {
        GameOver(
            capturePosition,
            null
        );
    }

    public void GameOver(
        Vector3 capturePosition,
        GameObject enemyVisualObject)
    {
        if (gameEnded)
            return;

        capturedEnemyVisualObject =
            enemyVisualObject;

        StartCoroutine(
            GameOverRoutine(
                capturePosition
            )
        );
    }

    // =========================================================
    // GAME OVER ROUTINE
    // =========================================================

    private IEnumerator GameOverRoutine(
        Vector3 capturePosition)
    {
        gameEnded = true;

        Debug.Log(
            "GAME OVER CAPTURE START"
        );

        // -----------------------------------------------------
        // ARRÊTE LES SONS DE GAMEPLAY
        // -----------------------------------------------------

        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance
                .StopAllGameplayAudio();
        }

        // -----------------------------------------------------
        // ARRÊTE LE GAMEPLAY
        // -----------------------------------------------------

        StopGameplayForCapture();

        // -----------------------------------------------------
        // CACHE LE HUD
        // -----------------------------------------------------

        if (hudObject != null)
        {
            hudObject.SetActive(false);
        }

        // -----------------------------------------------------
        // EFFET DE CAPTURE
        // -----------------------------------------------------

        SpawnCaptureCloud(
            capturePosition
        );

        PlaySound(
            captureSound,
            captureSoundVolume
        );

        yield return new WaitForSeconds(
            captureEffectDuration
        );

        // -----------------------------------------------------
        // GAME OVER FINAL
        // -----------------------------------------------------

        Debug.Log("GAME OVER");

        PlaySound(
            gameOverSound,
            gameOverSoundVolume
        );

        if (cameraEmptyObject != null)
        {
            cameraEmptyObject.SetActive(false);
        }

        if (GameUI.Instance != null)
        {
            GameUI.Instance
                .ShowGameOverPanel();
        }

        Time.timeScale = 0f;
    }

    // =========================================================
    // STOP GAMEPLAY FOR CAPTURE
    // =========================================================

    private void StopGameplayForCapture()
    {
        // -----------------------------------------------------
        // DÉSACTIVE LE JOUEUR
        // -----------------------------------------------------

        if (playerMovement != null)
        {
            playerMovement
                .SetInputEnabled(false);
        }

        // -----------------------------------------------------
        // CACHE LE VISUEL DU JOUEUR
        // -----------------------------------------------------

        if (hidePlayerDuringCapture &&
            playerVisualObject != null)
        {
            playerVisualObject.SetActive(false);
        }

        // -----------------------------------------------------
        // CACHE LE VISUEL DE L'ENNEMI
        // -----------------------------------------------------

        if (hideEnemyDuringCapture &&
            capturedEnemyVisualObject != null)
        {
            capturedEnemyVisualObject.SetActive(false);
        }

        // -----------------------------------------------------
        // ARRÊTE LES ENNEMIS
        // -----------------------------------------------------

        if (enemiesToPauseOnGameOver == null)
            return;

        foreach (
            EnemyAI enemy
            in enemiesToPauseOnGameOver)
        {
            if (enemy != null)
            {
                enemy.PauseAI();
            }
        }
    }

    // =========================================================
    // CAPTURE CLOUD
    // =========================================================

    private void SpawnCaptureCloud(
        Vector3 position)
    {
        if (captureCloudPrefab == null)
            return;

        Instantiate(
            captureCloudPrefab,
            position,
            Quaternion.identity
        );
    }

    // =========================================================
    // SOUND
    // =========================================================

    private void PlaySound(
        AudioClip clip,
        float volume)
    {
        if (clip == null)
            return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(
                clip,
                volume
            );
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

    // =========================================================
    // RESTART
    // =========================================================

    public void RestartGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager
                .GetActiveScene()
                .buildIndex
        );
    }
}
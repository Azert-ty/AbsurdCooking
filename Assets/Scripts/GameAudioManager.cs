using System.Collections;
using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioSource backgroundMusicSource;

    [Header("Chase Loop")]
    [SerializeField] private AudioSource chaseLoopSource;

    [Header("Settings")]
    [SerializeField] private bool resumeMusicAfterChase = true;

    private int activeChasers;
    private Coroutine chaseLoopRoutine;
    private bool gameEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (chaseLoopSource == null)
        {
            chaseLoopSource = gameObject.AddComponent<AudioSource>();
        }

        chaseLoopSource.loop = true;
        chaseLoopSource.playOnAwake = false;
    }

    public void StartChase(AudioClip chaseLoopClip, float delayBeforeLoop)
    {
        if (gameEnded)
            return;

        bool firstChaser = activeChasers == 0;
        activeChasers++;

        if (!firstChaser)
            return;

        StopBackgroundMusic();

        if (chaseLoopRoutine != null)
            StopCoroutine(chaseLoopRoutine);

        chaseLoopRoutine = StartCoroutine(
            StartChaseLoopAfterDelay(chaseLoopClip, delayBeforeLoop));
    }

    public void StopChase()
    {
        if (activeChasers > 0)
            activeChasers--;

        if (activeChasers > 0)
            return;

        activeChasers = 0;

        StopChaseLoop();

        if (!gameEnded && resumeMusicAfterChase)
            ResumeBackgroundMusic();
    }

    private IEnumerator StartChaseLoopAfterDelay(AudioClip chaseLoopClip, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (gameEnded)
            yield break;

        if (activeChasers <= 0)
            yield break;

        if (chaseLoopClip == null)
            yield break;

        chaseLoopSource.clip = chaseLoopClip;
        chaseLoopSource.loop = true;
        chaseLoopSource.Play();
    }

    private void StopChaseLoop()
    {
        if (chaseLoopRoutine != null)
        {
            StopCoroutine(chaseLoopRoutine);
            chaseLoopRoutine = null;
        }

        if (chaseLoopSource != null)
        {
            chaseLoopSource.Stop();
            chaseLoopSource.clip = null;
        }
    }

    private void StopBackgroundMusic()
    {
        if (backgroundMusicSource != null)
            backgroundMusicSource.Stop();
    }

    private void ResumeBackgroundMusic()
    {
        if (backgroundMusicSource == null)
            return;

        if (!backgroundMusicSource.isPlaying)
            backgroundMusicSource.Play();
    }

    public void StopAllGameplayAudio()
    {
        gameEnded = true;
        activeChasers = 0;

        StopChaseLoop();

        AudioSource[] allSources =
            FindObjectsByType<AudioSource>(
                FindObjectsSortMode.None);

        foreach (AudioSource source in allSources)
        {
            if (source != null)
                source.Stop();
        }
    }
}
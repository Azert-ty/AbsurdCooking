// using System.Collections;
// using TMPro;
// using UnityEngine;
// using UnityEngine.InputSystem;
// using UnityEngine.UI;

// public class IntroTutorialManager : MonoBehaviour
// {
//     private enum IntroPhase
//     {
//         Title,
//         StoryImages,
//         Dialogue,
//         Finished
//     }

//     [Header("Title Screen")]
//     [SerializeField] private Sprite titleSprite;
//     [SerializeField] private AudioClip titleSound;
//     [SerializeField] private float titleDuration = 3f;
//     [SerializeField] private bool loopTitleSound = false;

//     [Header("Story Images")]
//     [SerializeField] private Image storyImage;
//     [SerializeField] private Sprite[] storySprites;

//     [Tooltip("Image 1 = son 1, Image 2 = son 2, Image 3 = son 3. Le son 3 reste aussi pour les images 4 et 5.")]
//     [SerializeField] private AudioClip[] storySounds;

//     [SerializeField] private AudioSource storyAudioSource;
//     [SerializeField] private Button nextButton;

//     [Header("Story Audio Rules")]
//     [Tooltip("À partir de cette image, le même son reste jusqu'à la fin des images narratives. 3 = à partir de l'image 3.")]
//     [SerializeField] private int sharedSoundStartsAtImageNumber = 3;

//     [SerializeField] private bool loopStorySounds = true;

//     [Tooltip("Transition douce entre deux sons différents.")]
//     [SerializeField] private float audioFadeDuration = 0.35f;

//     [Header("Fade")]
//     [Tooltip("Image UI noire plein écran placée au-dessus de storyImage.")]
//     [SerializeField] private Image blackFadeImage;

//     [SerializeField] private float fadeDuration = 0.4f;

//     [Header("UI Dialogue")]
//     [SerializeField] private GameObject dialoguePanel;
//     [SerializeField] private TextMeshProUGUI dialogueText;
//     [SerializeField] private TextMeshProUGUI continueText;

//     [Header("Optional UI")]
//     [SerializeField] private Image tutorialImage;

//     [Header("References")]
//     [SerializeField] private PlayerMovement playerMovement;
//     [SerializeField] private IntroDoor introDoor;

//     [Header("Audio Control")]
//     [SerializeField] private bool pauseGameplayAudioDuringIntro = true;

//     private IntroPhase currentPhase;

//     private int storyIndex;
//     private int dialogueStep;

//     private bool introFinished;
//     private bool isTransitioning;
//     private bool previousAudioListenerPause;

//     private float storyAudioBaseVolume = 1f;

//     private Coroutine introCoroutine;
//     private Coroutine audioFadeCoroutine;

//     private AudioClip currentIntroClip;

//     private void Awake()
//     {
//         if (nextButton != null)
//             nextButton.onClick.AddListener(ContinueIntro);

//         if (blackFadeImage != null)
//             blackFadeImage.raycastTarget = false;

//         if (storyAudioSource != null)
//         {
//             storyAudioSource.playOnAwake = false;
//             storyAudioSource.ignoreListenerPause = true;
//             storyAudioBaseVolume = storyAudioSource.volume;
//         }
//     }

//     private void OnDestroy()
//     {
//         if (nextButton != null)
//             nextButton.onClick.RemoveListener(ContinueIntro);

//         RestoreGameplayAudio();
//     }

//     private void Start()
//     {
//         StartIntro();
//     }

//     private void Update()
//     {
//         if (introFinished)
//             return;

//         if (Keyboard.current == null)
//             return;

//         if (Keyboard.current.digit5Key.wasPressedThisFrame ||
//             Keyboard.current.numpad5Key.wasPressedThisFrame)
//         {
//             ContinueIntro();
//         }
//     }

//     private void StartIntro()
//     {
//         introFinished = false;
//         isTransitioning = false;
//         storyIndex = 0;
//         dialogueStep = 0;
//         currentIntroClip = null;

//         PauseGameplayAudio();

//         if (playerMovement != null)
//             playerMovement.SetInputEnabled(false);

//         if (dialoguePanel != null)
//             dialoguePanel.SetActive(false);

//         if (tutorialImage != null)
//             tutorialImage.gameObject.SetActive(false);

//         if (storyImage != null)
//             storyImage.gameObject.SetActive(true);

//         if (nextButton != null)
//             nextButton.gameObject.SetActive(false);

//         if (continueText != null)
//             continueText.gameObject.SetActive(false);

//         if (blackFadeImage != null)
//             blackFadeImage.gameObject.SetActive(true);

//         SetBlackAlpha(1f);

//         if (introCoroutine != null)
//             StopCoroutine(introCoroutine);

//         introCoroutine = StartCoroutine(TitleThenStoryRoutine());
//     }

//     private IEnumerator TitleThenStoryRoutine()
//     {
//         currentPhase = IntroPhase.Title;

//         if (storyImage != null && titleSprite != null)
//             storyImage.sprite = titleSprite;

//         PlayIntroSound(titleSound, loopTitleSound, true);

//         yield return FadeBlack(1f, 0f);

//         if (titleDuration > 0f)
//             yield return new WaitForSecondsRealtime(titleDuration);

//         yield return FadeBlack(0f, 1f);

//         yield return StopIntroSoundWithFade();

//         StartStoryImages();

//         yield return FadeBlack(1f, 0f);
//     }

//     private void StartStoryImages()
//     {
//         currentPhase = IntroPhase.StoryImages;
//         storyIndex = 0;

//         if (storySprites == null || storySprites.Length == 0)
//         {
//             StartDialogueIntro();
//             return;
//         }

//         if (storyImage != null)
//             storyImage.sprite = storySprites[0];

//         PlayStorySoundForImage(0);

//         if (nextButton != null)
//             nextButton.gameObject.SetActive(true);

//         if (continueText != null)
//         {
//             continueText.text = "Appuyez sur [5]";
//             continueText.gameObject.SetActive(true);
//         }
//     }

//     public void ContinueIntro()
//     {
//         if (introFinished || isTransitioning)
//             return;

//         switch (currentPhase)
//         {
//             case IntroPhase.Title:
//                 break;

//             case IntroPhase.StoryImages:
//                 ContinueStoryImages();
//                 break;

//             case IntroPhase.Dialogue:
//                 ContinueDialogue();
//                 break;
//             case IntroPhase.Finished:
//                 break;
//         }
//     }

//     private void ContinueStoryImages()
//     {
//         storyIndex++;

//         if (storyIndex >= storySprites.Length)
//         {
//             if (introCoroutine != null)
//                 StopCoroutine(introCoroutine);

//             introCoroutine = StartCoroutine(TransitionToDialogueRoutine());
//             return;
//         }

//         if (introCoroutine != null)
//             StopCoroutine(introCoroutine);

//         introCoroutine = StartCoroutine(ChangeStoryImageWithBlackFade(storyIndex));
//     }

//     private IEnumerator ChangeStoryImageWithBlackFade(int index)
//     {
//         isTransitioning = true;

//         yield return FadeBlack(0f, 1f);

//         if (storyImage != null && index >= 0 && index < storySprites.Length)
//             storyImage.sprite = storySprites[index];

//         PlayStorySoundForImage(index);

//         yield return FadeBlack(1f, 0f);

//         isTransitioning = false;
//     }

//     private IEnumerator TransitionToDialogueRoutine()
//     {
//         isTransitioning = true;

//         yield return FadeBlack(0f, 1f);

//         yield return StopIntroSoundWithFade();

//         StartDialogueIntro();

//         yield return FadeBlack(1f, 0f);

//         isTransitioning = false;
//     }

//     private IEnumerator FadeBlack(float from, float to)
//     {
//         if (blackFadeImage == null)
//             yield break;

//         float timer = 0f;

//         Color color = blackFadeImage.color;
//         color.a = from;
//         blackFadeImage.color = color;

//         while (timer < fadeDuration)
//         {
//             timer += Time.unscaledDeltaTime;

//             float t = timer / fadeDuration;
//             color.a = Mathf.Lerp(from, to, t);
//             blackFadeImage.color = color;

//             yield return null;
//         }

//         color.a = to;
//         blackFadeImage.color = color;
//     }

//     private void SetBlackAlpha(float alpha)
//     {
//         if (blackFadeImage == null)
//             return;

//         Color color = blackFadeImage.color;
//         color.a = alpha;
//         blackFadeImage.color = color;
//     }

//     private void PlayStorySoundForImage(int imageIndex)
//     {
//         AudioClip clip = GetStorySoundForImage(imageIndex);

//         if (clip == null)
//             return;

//         PlayIntroSound(clip, loopStorySounds, false);
//     }

//     private AudioClip GetStorySoundForImage(int imageIndex)
//     {
//         if (storySounds == null || storySounds.Length == 0)
//             return null;

//         int sharedStartIndex = Mathf.Max(0, sharedSoundStartsAtImageNumber - 1);

//         int soundIndex;

//         if (imageIndex >= sharedStartIndex)
//             soundIndex = sharedStartIndex;
//         else
//             soundIndex = imageIndex;

//         if (soundIndex < 0 || soundIndex >= storySounds.Length)
//             return null;

//         return storySounds[soundIndex];
//     }

//     private void PlayIntroSound(AudioClip clip, bool loop, bool instant)
//     {
//         if (storyAudioSource == null)
//             return;

//         if (clip == null)
//             return;

//         if (currentIntroClip == clip && storyAudioSource.isPlaying)
//             return;

//         if (audioFadeCoroutine != null)
//             StopCoroutine(audioFadeCoroutine);

//         audioFadeCoroutine = StartCoroutine(
//             ChangeIntroSoundWithFade(clip, loop, instant)
//         );
//     }

//     private IEnumerator ChangeIntroSoundWithFade(AudioClip newClip, bool loop, bool instant)
//     {
//         if (storyAudioSource == null)
//             yield break;

//         if (!instant && storyAudioSource.isPlaying && audioFadeDuration > 0f)
//             yield return FadeAudioVolume(storyAudioSource.volume, 0f);

//         storyAudioSource.Stop();
//         storyAudioSource.clip = newClip;
//         storyAudioSource.loop = loop;
//         storyAudioSource.volume = instant ? storyAudioBaseVolume : 0f;

//         currentIntroClip = newClip;

//         storyAudioSource.Play();

//         if (!instant && audioFadeDuration > 0f)
//             yield return FadeAudioVolume(0f, storyAudioBaseVolume);
//         else
//             storyAudioSource.volume = storyAudioBaseVolume;
//     }

//     private IEnumerator StopIntroSoundWithFade()
//     {
//         if (storyAudioSource == null)
//             yield break;

//         if (audioFadeCoroutine != null)
//             StopCoroutine(audioFadeCoroutine);

//         if (storyAudioSource.isPlaying && audioFadeDuration > 0f)
//             yield return FadeAudioVolume(storyAudioSource.volume, 0f);

//         storyAudioSource.Stop();
//         storyAudioSource.clip = null;
//         storyAudioSource.volume = storyAudioBaseVolume;

//         currentIntroClip = null;
//     }

//     private IEnumerator FadeAudioVolume(float from, float to)
//     {
//         if (storyAudioSource == null)
//             yield break;

//         float timer = 0f;

//         storyAudioSource.volume = from;

//         while (timer < audioFadeDuration)
//         {
//             timer += Time.unscaledDeltaTime;

//             float t = timer / audioFadeDuration;
//             storyAudioSource.volume = Mathf.Lerp(from, to, t);

//             yield return null;
//         }

//         storyAudioSource.volume = to;
//     }

//     private void StartDialogueIntro()
//     {
//         currentPhase = IntroPhase.Dialogue;
//         dialogueStep = 0;

//         if (storyImage != null)
//             storyImage.gameObject.SetActive(false);

//         if (nextButton != null)
//             nextButton.gameObject.SetActive(true);

//         if (dialoguePanel != null)
//             dialoguePanel.SetActive(true);

//         if (tutorialImage != null)
//             tutorialImage.gameObject.SetActive(true);

//         ShowDialogue("Je dois récupérer le masque sacré.");
//     }

//     private void ContinueDialogue()
//     {
//         dialogueStep++;

//         switch (dialogueStep)
//         {
//             case 1:
//                 if (introDoor != null)
//                     introDoor.OpenDoor();

//                 ShowDialogue("La voie est ouverte. Je dois rester discret.");
//                 break;

//             case 2:
//                 ShowDialogue("Je dois éviter les cônes de vision des gardes à tout prix.");
//                 break;

//             case 3:
//                 ShowDialogue("Je dois ramener le masque à mon village.");
//                 break;

//             case 4:
//                 EndIntro();
//                 break;
//         }
//     }

//     private void ShowDialogue(string message)
//     {
//         if (dialogueText != null)
//             dialogueText.text = $"<i>{message}</i>";

//         if (continueText != null)
//         {
//             continueText.text = "Appuyez sur [5]";
//             continueText.gameObject.SetActive(true);
//         }
//     }

//     private void EndIntro()
//     {
//         introFinished = true;
//         currentPhase = IntroPhase.Finished;

//         if (introCoroutine != null)
//             StopCoroutine(introCoroutine);

//         if (audioFadeCoroutine != null)
//             StopCoroutine(audioFadeCoroutine);

//         if (storyAudioSource != null)
//         {
//             storyAudioSource.Stop();
//             storyAudioSource.clip = null;
//             storyAudioSource.volume = storyAudioBaseVolume;
//         }

//         currentIntroClip = null;

//         RestoreGameplayAudio();

//         if (dialoguePanel != null)
//             dialoguePanel.SetActive(false);

//         if (tutorialImage != null)
//             tutorialImage.gameObject.SetActive(false);

//         if (storyImage != null)
//             storyImage.gameObject.SetActive(false);

//         if (blackFadeImage != null)
//             blackFadeImage.gameObject.SetActive(false);

//         if (nextButton != null)
//             nextButton.gameObject.SetActive(false);

//         if (playerMovement != null)
//             playerMovement.SetInputEnabled(true);
//     }

//     private void PauseGameplayAudio()
//     {
//         if (!pauseGameplayAudioDuringIntro)
//             return;

//         previousAudioListenerPause = AudioListener.pause;
//         AudioListener.pause = true;

//         if (storyAudioSource != null)
//             storyAudioSource.ignoreListenerPause = true;
//     }

//     private void RestoreGameplayAudio()
//     {
//         if (!pauseGameplayAudioDuringIntro)
//             return;

//         AudioListener.pause = previousAudioListenerPause;
//     }
// }

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class IntroTutorialManager : MonoBehaviour
{
    private enum IntroPhase
    {
        Title,
        StoryImages,
        Dialogue,
        Finished
    }

    [System.Serializable]
    public class TimedStoryEffect
    {
        [Tooltip("Effet sonore à jouer.")]
        public AudioClip clip;

        [Tooltip("Temps d'attente après l'apparition de la frame.")]
        [Min(0f)]
        public float delay;

        [Range(0f, 1f)]
        public float volume = 1f;
    }

    [System.Serializable]
    public class StoryFrameAudio
    {
        [Tooltip("Petits effets sonores joués pendant cette frame.")]
        public TimedStoryEffect[] effects;
    }

    // =========================================================
    // TITLE SCREEN
    // =========================================================

    [Header("Title Screen")]
    [SerializeField] private Sprite titleSprite;
    [SerializeField] private AudioClip titleSound;
    [SerializeField] private float titleDuration = 3f;
    [SerializeField] private bool loopTitleSound = false;

    [SerializeField, Range(0f, 1f)]
    private float titleVolume = 1f;

    // =========================================================
    // STORY IMAGES
    // =========================================================

    [Header("Story Images")]
    [SerializeField] private Image storyImage;
    [SerializeField] private Sprite[] storySprites;
    [SerializeField] private Button nextButton;

    // =========================================================
    // STORY MUSIC
    // =========================================================

    [Header("Story Music")]
    [Tooltip("Même musique utilisée pendant les 5 frames.")]
    [SerializeField] private AudioClip storyMusic;

    [Tooltip("AudioSource réservé à la musique principale de l'intro.")]
    [SerializeField] private AudioSource storyMusicSource;

    [SerializeField] private bool loopStoryMusic = true;

    [SerializeField, Range(0f, 1f)]
    private float storyMusicVolume = 0.65f;

    [Tooltip("Fondu quand la musique commence ou s'arrête.")]
    [SerializeField] private float storyMusicFadeDuration = 0.35f;

    // =========================================================
    // STORY EFFECTS
    // =========================================================

    [Header("Story Frame Effects")]
    [Tooltip("Element 0 = Frame 1, Element 1 = Frame 2, etc.")]
    [SerializeField] private StoryFrameAudio[] frameAudio;

    [Tooltip("AudioSource réservé aux petits effets sonores.")]
    [SerializeField] private AudioSource storyEffectsSource;

    [SerializeField, Range(0f, 1f)]
    private float globalEffectsVolume = 1f;

    // =========================================================
    // FADE
    // =========================================================

    [Header("Fade")]
    [Tooltip("Image UI noire plein écran placée au-dessus de storyImage.")]
    [SerializeField] private Image blackFadeImage;

    [SerializeField] private float fadeDuration = 0.4f;

    // =========================================================
    // UI DIALOGUE
    // =========================================================

    [Header("UI Dialogue")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI continueText;

    [Header("Optional UI")]
    [SerializeField] private Image tutorialImage;

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private IntroDoor introDoor;

    // =========================================================
    // AUDIO CONTROL
    // =========================================================

    [Header("Audio Control")]
    [SerializeField] private bool pauseGameplayAudioDuringIntro = true;

    // =========================================================
    // PRIVATE STATE
    // =========================================================

    private IntroPhase currentPhase;

    private int storyIndex;
    private int dialogueStep;

    private bool introFinished;
    private bool isTransitioning;
    private bool previousAudioListenerPause;

    private Coroutine introCoroutine;
    private Coroutine storyMusicFadeCoroutine;
    private Coroutine frameEffectsCoroutine;

    // =========================================================
    // UNITY EVENTS
    // =========================================================

    private void Awake()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(ContinueIntro);

        if (blackFadeImage != null)
            blackFadeImage.raycastTarget = false;

        SetupAudioSources();
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(ContinueIntro);

        StopAllStoryAudioImmediate();
        RestoreGameplayAudio();
    }

    private void Start()
    {
        StartIntro();
    }

    private void Update()
    {
        if (introFinished)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit5Key.wasPressedThisFrame ||
            Keyboard.current.numpad5Key.wasPressedThisFrame)
        {
            ContinueIntro();
        }
    }

    // =========================================================
    // SETUP
    // =========================================================

    private void SetupAudioSources()
    {
        if (storyMusicSource == null)
            storyMusicSource = gameObject.AddComponent<AudioSource>();

        storyMusicSource.playOnAwake = false;
        storyMusicSource.loop = false;
        storyMusicSource.spatialBlend = 0f;
        storyMusicSource.ignoreListenerPause = true;

        if (storyEffectsSource == null)
            storyEffectsSource = gameObject.AddComponent<AudioSource>();

        storyEffectsSource.playOnAwake = false;
        storyEffectsSource.loop = false;
        storyEffectsSource.spatialBlend = 0f;
        storyEffectsSource.ignoreListenerPause = true;
    }

    // =========================================================
    // INTRO FLOW
    // =========================================================

    private void StartIntro()
    {
        introFinished = false;
        isTransitioning = false;
        storyIndex = 0;
        dialogueStep = 0;

        StopAllStoryAudioImmediate();
        PauseGameplayAudio();

        if (playerMovement != null)
            playerMovement.SetInputEnabled(false);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (tutorialImage != null)
            tutorialImage.gameObject.SetActive(false);

        if (storyImage != null)
            storyImage.gameObject.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        if (continueText != null)
            continueText.gameObject.SetActive(false);

        if (blackFadeImage != null)
            blackFadeImage.gameObject.SetActive(true);

        SetBlackAlpha(1f);

        if (introCoroutine != null)
            StopCoroutine(introCoroutine);

        introCoroutine = StartCoroutine(TitleThenStoryRoutine());
    }

    private IEnumerator TitleThenStoryRoutine()
    {
        currentPhase = IntroPhase.Title;

        if (storyImage != null && titleSprite != null)
            storyImage.sprite = titleSprite;

        PlayTitleSound();

        yield return FadeBlack(1f, 0f);

        if (titleDuration > 0f)
            yield return new WaitForSecondsRealtime(titleDuration);

        yield return FadeBlack(0f, 1f);

        yield return StopMainAudioWithFade();

        StartStoryImages();

        yield return FadeBlack(1f, 0f);
    }

    private void StartStoryImages()
    {
        currentPhase = IntroPhase.StoryImages;
        storyIndex = 0;

        if (storySprites == null || storySprites.Length == 0)
        {
            StartDialogueIntro();
            return;
        }

        if (storyImage != null)
            storyImage.sprite = storySprites[0];

        StartStoryMusic();
        PlayFrameEffects(0);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        if (continueText != null)
        {
            continueText.text = "Appuyez sur [5]";
            continueText.gameObject.SetActive(true);
        }
    }

    public void ContinueIntro()
    {
        if (introFinished || isTransitioning)
            return;

        switch (currentPhase)
        {
            case IntroPhase.Title:
                break;

            case IntroPhase.StoryImages:
                ContinueStoryImages();
                break;

            case IntroPhase.Dialogue:
                ContinueDialogue();
                break;
        }
    }

    private void ContinueStoryImages()
    {
        storyIndex++;

        if (storyIndex >= storySprites.Length)
        {
            if (introCoroutine != null)
                StopCoroutine(introCoroutine);

            introCoroutine = StartCoroutine(TransitionToDialogueRoutine());
            return;
        }

        if (introCoroutine != null)
            StopCoroutine(introCoroutine);

        introCoroutine = StartCoroutine(ChangeStoryImageWithBlackFade(storyIndex));
    }

    private IEnumerator ChangeStoryImageWithBlackFade(int index)
    {
        isTransitioning = true;

        yield return FadeBlack(0f, 1f);

        if (storyImage != null &&
            index >= 0 &&
            index < storySprites.Length)
        {
            storyImage.sprite = storySprites[index];
        }

        PlayFrameEffects(index);

        yield return FadeBlack(1f, 0f);

        isTransitioning = false;
    }

    private IEnumerator TransitionToDialogueRoutine()
    {
        isTransitioning = true;

        yield return FadeBlack(0f, 1f);

        yield return StopStoryAudioWithFade();

        StartDialogueIntro();

        yield return FadeBlack(1f, 0f);

        isTransitioning = false;
    }

    // =========================================================
    // FADE VISUAL
    // =========================================================

    private IEnumerator FadeBlack(float from, float to)
    {
        if (blackFadeImage == null)
            yield break;

        float timer = 0f;

        Color color = blackFadeImage.color;
        color.a = from;
        blackFadeImage.color = color;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t =
                fadeDuration <= 0f
                    ? 1f
                    : timer / fadeDuration;

            color.a = Mathf.Lerp(from, to, t);
            blackFadeImage.color = color;

            yield return null;
        }

        color.a = to;
        blackFadeImage.color = color;
    }

    private void SetBlackAlpha(float alpha)
    {
        if (blackFadeImage == null)
            return;

        Color color = blackFadeImage.color;
        color.a = alpha;
        blackFadeImage.color = color;
    }

    // =========================================================
    // AUDIO : TITLE
    // =========================================================

    private void PlayTitleSound()
    {
        if (titleSound == null || storyMusicSource == null)
            return;

        StopFrameEffects();

        if (storyMusicFadeCoroutine != null)
        {
            StopCoroutine(storyMusicFadeCoroutine);
            storyMusicFadeCoroutine = null;
        }

        storyMusicSource.Stop();
        storyMusicSource.clip = titleSound;
        storyMusicSource.loop = loopTitleSound;
        storyMusicSource.volume = titleVolume;
        storyMusicSource.Play();
    }

    // =========================================================
    // AUDIO : STORY MUSIC
    // =========================================================

    private void StartStoryMusic()
    {
        if (storyMusic == null || storyMusicSource == null)
            return;

        if (storyMusicSource.isPlaying &&
            storyMusicSource.clip == storyMusic)
        {
            return;
        }

        if (storyMusicFadeCoroutine != null)
        {
            StopCoroutine(storyMusicFadeCoroutine);
            storyMusicFadeCoroutine = null;
        }

        storyMusicSource.Stop();
        storyMusicSource.clip = storyMusic;
        storyMusicSource.loop = loopStoryMusic;
        storyMusicSource.volume = 0f;
        storyMusicSource.Play();

        storyMusicFadeCoroutine =
            StartCoroutine(FadeStoryMusic(0f, storyMusicVolume));
    }

    private IEnumerator FadeStoryMusic(float from, float to)
    {
        if (storyMusicSource == null)
            yield break;

        float timer = 0f;

        storyMusicSource.volume = from;

        if (storyMusicFadeDuration <= 0f)
        {
            storyMusicSource.volume = to;
            storyMusicFadeCoroutine = null;
            yield break;
        }

        while (timer < storyMusicFadeDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = timer / storyMusicFadeDuration;

            storyMusicSource.volume =
                Mathf.Lerp(from, to, t);

            yield return null;
        }

        storyMusicSource.volume = to;
        storyMusicFadeCoroutine = null;
    }

    private IEnumerator StopMainAudioWithFade()
    {
        if (storyMusicSource == null)
            yield break;

        if (storyMusicFadeCoroutine != null)
        {
            StopCoroutine(storyMusicFadeCoroutine);
            storyMusicFadeCoroutine = null;
        }

        if (storyMusicSource.isPlaying)
        {
            float startVolume = storyMusicSource.volume;

            yield return FadeStoryMusic(startVolume, 0f);

            storyMusicSource.Stop();
        }

        storyMusicSource.clip = null;
        storyMusicSource.volume = storyMusicVolume;
    }

    // =========================================================
    // AUDIO : FRAME EFFECTS
    // =========================================================

    private void PlayFrameEffects(int frameIndex)
    {
        StopFrameEffects();

        if (frameAudio == null)
            return;

        if (frameIndex < 0 || frameIndex >= frameAudio.Length)
            return;

        StoryFrameAudio frame = frameAudio[frameIndex];

        if (frame == null ||
            frame.effects == null ||
            frame.effects.Length == 0)
        {
            return;
        }

        frameEffectsCoroutine =
            StartCoroutine(PlayFrameEffectsRoutine(frame));
    }

    private IEnumerator PlayFrameEffectsRoutine(StoryFrameAudio frame)
    {
        float currentDelay = 0f;

        for (int i = 0; i < frame.effects.Length; i++)
        {
            TimedStoryEffect effect = frame.effects[i];

            if (effect == null || effect.clip == null)
                continue;

            float targetDelay =
                Mathf.Max(currentDelay, effect.delay);

            float waitDuration =
                targetDelay - currentDelay;

            if (waitDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(waitDuration);
            }

            if (storyEffectsSource != null)
            {
                storyEffectsSource.PlayOneShot(
                    effect.clip,
                    effect.volume * globalEffectsVolume
                );
            }

            currentDelay = targetDelay;
        }

        frameEffectsCoroutine = null;
    }

    private void StopFrameEffects()
    {
        if (frameEffectsCoroutine != null)
        {
            StopCoroutine(frameEffectsCoroutine);
            frameEffectsCoroutine = null;
        }

        if (storyEffectsSource != null)
            storyEffectsSource.Stop();
    }

    private IEnumerator StopStoryAudioWithFade()
    {
        StopFrameEffects();

        yield return StopMainAudioWithFade();
    }

    private void StopAllStoryAudioImmediate()
    {
        if (storyMusicFadeCoroutine != null)
        {
            StopCoroutine(storyMusicFadeCoroutine);
            storyMusicFadeCoroutine = null;
        }

        StopFrameEffects();

        if (storyMusicSource != null)
        {
            storyMusicSource.Stop();
            storyMusicSource.clip = null;
            storyMusicSource.volume = storyMusicVolume;
        }
    }

    // =========================================================
    // DIALOGUE INTRO
    // =========================================================

    private void StartDialogueIntro()
    {
        currentPhase = IntroPhase.Dialogue;
        dialogueStep = 0;

        if (storyImage != null)
            storyImage.gameObject.SetActive(false);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (tutorialImage != null)
            tutorialImage.gameObject.SetActive(true);

        ShowDialogue("Je dois récupérer le coffret avant demain.");
    }

    private void ContinueDialogue()
    {
        dialogueStep++;

        switch (dialogueStep)
        {
            case 1:
                if (introDoor != null)
                    introDoor.OpenDoor();

                ShowDialogue("La voie est ouverte. Je dois rester discret.");
                break;

            case 2:
                ShowDialogue("Je dois éviter les cônes de vision des policiers.");
                break;

            case 3:
                ShowDialogue("Je dois sortir d'ici sans me faire prendre.");
                break;

            case 4:
                EndIntro();
                break;
        }
    }

    private void ShowDialogue(string message)
    {
        if (dialogueText != null)
            dialogueText.text = $"<i>{message}</i>";

        if (continueText != null)
        {
            continueText.text = "Appuyez sur [5]";
            continueText.gameObject.SetActive(true);
        }
    }

    private void EndIntro()
    {
        introFinished = true;
        currentPhase = IntroPhase.Finished;

        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

        StopAllStoryAudioImmediate();
        RestoreGameplayAudio();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (tutorialImage != null)
            tutorialImage.gameObject.SetActive(false);

        if (storyImage != null)
            storyImage.gameObject.SetActive(false);

        if (blackFadeImage != null)
            blackFadeImage.gameObject.SetActive(false);

        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        if (continueText != null)
            continueText.gameObject.SetActive(false);

        if (playerMovement != null)
            playerMovement.SetInputEnabled(true);
    }

    // =========================================================
    // GAMEPLAY AUDIO PAUSE
    // =========================================================

    private void PauseGameplayAudio()
    {
        if (!pauseGameplayAudioDuringIntro)
            return;

        previousAudioListenerPause = AudioListener.pause;
        AudioListener.pause = true;

        if (storyMusicSource != null)
            storyMusicSource.ignoreListenerPause = true;

        if (storyEffectsSource != null)
            storyEffectsSource.ignoreListenerPause = true;
    }

    private void RestoreGameplayAudio()
    {
        if (!pauseGameplayAudioDuringIntro)
            return;

        AudioListener.pause = previousAudioListenerPause;
    }
}
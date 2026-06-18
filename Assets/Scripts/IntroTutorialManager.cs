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

    [Header("Title Screen")]
    [SerializeField] private Sprite titleSprite;
    [SerializeField] private AudioClip titleSound;
    [SerializeField] private float titleDuration = 3f;
    [SerializeField] private bool loopTitleSound = false;

    [Header("Story Images")]
    [SerializeField] private Image storyImage;
    [SerializeField] private Sprite[] storySprites;

    [Tooltip("Image 1 = son 1, Image 2 = son 2, Image 3 = son 3. Le son 3 reste aussi pour les images 4 et 5.")]
    [SerializeField] private AudioClip[] storySounds;

    [SerializeField] private AudioSource storyAudioSource;
    [SerializeField] private Button nextButton;

    [Header("Story Audio Rules")]
    [Tooltip("À partir de cette image, le même son reste jusqu'à la fin des images narratives. 3 = à partir de l'image 3.")]
    [SerializeField] private int sharedSoundStartsAtImageNumber = 3;

    [SerializeField] private bool loopStorySounds = true;

    [Tooltip("Transition douce entre deux sons différents.")]
    [SerializeField] private float audioFadeDuration = 0.35f;

    [Header("Fade")]
    [Tooltip("Image UI noire plein écran placée au-dessus de storyImage.")]
    [SerializeField] private Image blackFadeImage;

    [SerializeField] private float fadeDuration = 0.4f;

    [Header("UI Dialogue")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI continueText;

    [Header("Optional UI")]
    [SerializeField] private Image tutorialImage;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private IntroDoor introDoor;

    [Header("Audio Control")]
    [SerializeField] private bool pauseGameplayAudioDuringIntro = true;

    private IntroPhase currentPhase;

    private int storyIndex;
    private int dialogueStep;

    private bool introFinished;
    private bool isTransitioning;
    private bool previousAudioListenerPause;

    private float storyAudioBaseVolume = 1f;

    private Coroutine introCoroutine;
    private Coroutine audioFadeCoroutine;

    private AudioClip currentIntroClip;

    private void Awake()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(ContinueIntro);

        if (blackFadeImage != null)
            blackFadeImage.raycastTarget = false;

        if (storyAudioSource != null)
        {
            storyAudioSource.playOnAwake = false;
            storyAudioSource.ignoreListenerPause = true;
            storyAudioBaseVolume = storyAudioSource.volume;
        }
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(ContinueIntro);

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

    private void StartIntro()
    {
        introFinished = false;
        isTransitioning = false;
        storyIndex = 0;
        dialogueStep = 0;
        currentIntroClip = null;

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

        PlayIntroSound(titleSound, loopTitleSound, true);

        yield return FadeBlack(1f, 0f);

        if (titleDuration > 0f)
            yield return new WaitForSecondsRealtime(titleDuration);

        yield return FadeBlack(0f, 1f);

        yield return StopIntroSoundWithFade();

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

        PlayStorySoundForImage(0);

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

        if (storyImage != null && index >= 0 && index < storySprites.Length)
            storyImage.sprite = storySprites[index];

        PlayStorySoundForImage(index);

        yield return FadeBlack(1f, 0f);

        isTransitioning = false;
    }

    private IEnumerator TransitionToDialogueRoutine()
    {
        isTransitioning = true;

        yield return FadeBlack(0f, 1f);

        yield return StopIntroSoundWithFade();

        StartDialogueIntro();

        yield return FadeBlack(1f, 0f);

        isTransitioning = false;
    }

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

            float t = timer / fadeDuration;
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

    private void PlayStorySoundForImage(int imageIndex)
    {
        AudioClip clip = GetStorySoundForImage(imageIndex);

        if (clip == null)
            return;

        PlayIntroSound(clip, loopStorySounds, false);
    }

    private AudioClip GetStorySoundForImage(int imageIndex)
    {
        if (storySounds == null || storySounds.Length == 0)
            return null;

        int sharedStartIndex = Mathf.Max(0, sharedSoundStartsAtImageNumber - 1);

        int soundIndex;

        if (imageIndex >= sharedStartIndex)
            soundIndex = sharedStartIndex;
        else
            soundIndex = imageIndex;

        if (soundIndex < 0 || soundIndex >= storySounds.Length)
            return null;

        return storySounds[soundIndex];
    }

    private void PlayIntroSound(AudioClip clip, bool loop, bool instant)
    {
        if (storyAudioSource == null)
            return;

        if (clip == null)
            return;

        if (currentIntroClip == clip && storyAudioSource.isPlaying)
            return;

        if (audioFadeCoroutine != null)
            StopCoroutine(audioFadeCoroutine);

        audioFadeCoroutine = StartCoroutine(
            ChangeIntroSoundWithFade(clip, loop, instant)
        );
    }

    private IEnumerator ChangeIntroSoundWithFade(AudioClip newClip, bool loop, bool instant)
    {
        if (storyAudioSource == null)
            yield break;

        if (!instant && storyAudioSource.isPlaying && audioFadeDuration > 0f)
            yield return FadeAudioVolume(storyAudioSource.volume, 0f);

        storyAudioSource.Stop();
        storyAudioSource.clip = newClip;
        storyAudioSource.loop = loop;
        storyAudioSource.volume = instant ? storyAudioBaseVolume : 0f;

        currentIntroClip = newClip;

        storyAudioSource.Play();

        if (!instant && audioFadeDuration > 0f)
            yield return FadeAudioVolume(0f, storyAudioBaseVolume);
        else
            storyAudioSource.volume = storyAudioBaseVolume;
    }

    private IEnumerator StopIntroSoundWithFade()
    {
        if (storyAudioSource == null)
            yield break;

        if (audioFadeCoroutine != null)
            StopCoroutine(audioFadeCoroutine);

        if (storyAudioSource.isPlaying && audioFadeDuration > 0f)
            yield return FadeAudioVolume(storyAudioSource.volume, 0f);

        storyAudioSource.Stop();
        storyAudioSource.clip = null;
        storyAudioSource.volume = storyAudioBaseVolume;

        currentIntroClip = null;
    }

    private IEnumerator FadeAudioVolume(float from, float to)
    {
        if (storyAudioSource == null)
            yield break;

        float timer = 0f;

        storyAudioSource.volume = from;

        while (timer < audioFadeDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = timer / audioFadeDuration;
            storyAudioSource.volume = Mathf.Lerp(from, to, t);

            yield return null;
        }

        storyAudioSource.volume = to;
    }

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

        ShowDialogue("Je dois récupérer le masque sacré.");
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
                ShowDialogue("Je dois éviter les cônes de vision des gardes à tout prix.");
                break;

            case 3:
                ShowDialogue("Je dois ramener le masque à mon village.");
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
            StopCoroutine(introCoroutine);

        if (audioFadeCoroutine != null)
            StopCoroutine(audioFadeCoroutine);

        if (storyAudioSource != null)
        {
            storyAudioSource.Stop();
            storyAudioSource.clip = null;
            storyAudioSource.volume = storyAudioBaseVolume;
        }

        currentIntroClip = null;

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

        if (playerMovement != null)
            playerMovement.SetInputEnabled(true);
    }

    private void PauseGameplayAudio()
    {
        if (!pauseGameplayAudioDuringIntro)
            return;

        previousAudioListenerPause = AudioListener.pause;
        AudioListener.pause = true;

        if (storyAudioSource != null)
            storyAudioSource.ignoreListenerPause = true;
    }

    private void RestoreGameplayAudio()
    {
        if (!pauseGameplayAudioDuringIntro)
            return;

        AudioListener.pause = previousAudioListenerPause;
    }
}
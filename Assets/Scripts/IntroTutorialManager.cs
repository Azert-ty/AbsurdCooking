using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class IntroTutorialManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI continueText;

    [Header("Optional UI")]
    [SerializeField] private Image tutorialImage;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private IntroDoor introDoor;

    private int step;
    private bool introFinished;

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
        step = 0;
        introFinished = false;

        if (playerMovement != null)
            playerMovement.SetInputEnabled(false);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (tutorialImage != null)
            tutorialImage.gameObject.SetActive(true);

        ShowDialogue("Je dois récupérer la clé.");
    }

    private void ContinueIntro()
    {
        step++;

        switch (step)
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

                EndIntro();
                break;
        }
    }

    private void ShowDialogue(string message)
    {
        dialogueText.text = $"<i>{message}</i>";

        continueText.text = "Appuyez sur [5]";
        continueText.gameObject.SetActive(true);
    }

    private void EndIntro()
    {
        introFinished = true;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (tutorialImage != null)
            tutorialImage.gameObject.SetActive(false);

        if (playerMovement != null)
            playerMovement.SetInputEnabled(true);
    }
}
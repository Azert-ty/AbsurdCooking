// using TMPro;
// using UnityEngine;

// public class GameUI : MonoBehaviour
// {
//     public static GameUI Instance { get; private set; }

//     [Header("HUD")]
//     [SerializeField] private TextMeshProUGUI coinsText;
//     [SerializeField] private TextMeshProUGUI objectiveText;

//     [Header("Game Over")]
//     [SerializeField] private GameObject gameOverPanel;
//     [SerializeField] private TextMeshProUGUI gameOverText;

//     [Header("Victory")]
//     [SerializeField] private GameObject victoryPanel;
//     [SerializeField] private TextMeshProUGUI victoryText;

//     private void Awake()
//     {
//         if (Instance != null && Instance != this)
//         {
//             Destroy(gameObject);
//             return;
//         }

//         Instance = this;

//         if (gameOverPanel != null)
//             gameOverPanel.SetActive(false);

//         if (victoryPanel != null)
//             victoryPanel.SetActive(false);
//     }

//     private void Update()
//     {
//         UpdateHUD();
//     }

//     private void UpdateHUD()
//     {
//         UpdateCoinsText();
//         UpdateObjectiveText();
//     }

//     private void UpdateCoinsText()
//     {
//         if (coinsText == null)
//             return;

//         if (CollectibleManager.Instance == null)
//             return;

//         coinsText.text =
//             "Pièces : " +
//             CollectibleManager.Instance.CoinCount +
//             " / " +
//             CollectibleManager.Instance.TotalCoins;
//     }

//     private void UpdateObjectiveText()
//     {
//         if (objectiveText == null)
//             return;

//         if (GameManager.Instance == null)
//             return;

//         if (GameManager.Instance.HasObjective())
//         {
//             objectiveText.text =
//                 "Objectif : rejoindre la sortie";
//         }
//         else
//         {
//             objectiveText.text =
//                 "Objectif : récupérer le trésor";
//         }
//     }

//     public void ShowGameOverPanel()
//     {
//         if (gameOverPanel != null)
//             gameOverPanel.SetActive(true);

//         if (gameOverText != null)
//         {
//             gameOverText.text =
//                 "Vous avez été repéré\nAppuyez sur R pour recommencer";
//         }
//     }

//     public void ShowVictoryPanel()
//     {
//         if (victoryPanel != null)
//             victoryPanel.SetActive(true);

//         if (victoryText == null)
//             return;

//         int coins = 0;
//         int totalCoins = 0;

//         if (CollectibleManager.Instance != null)
//         {
//             coins = CollectibleManager.Instance.CoinCount;
//             totalCoins = CollectibleManager.Instance.TotalCoins;
//         }

//         victoryText.text =
//             "Mission réussie !\n" +
//             "Pièces collectées : " + coins + " / " + totalCoins + "\n" +
//             "Trésor récupéré\n" +
//             "Appuyez sur R pour rejouer";
//     }
// }

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{

    
    public static GameUI Instance { get; private set; }
    [Header("Victory Stars")]
    [SerializeField] private Image timeStarImage;
    [SerializeField] private Image coinsStarImage;
    [SerializeField] private Image stealthStarImage;

    [SerializeField] private Sprite fullStarSprite;
    [SerializeField] private Sprite emptyStarSprite;


    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI objectiveText;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button gameOverRestartButton;

    [Header("Victory")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TextMeshProUGUI victoryText;
    [SerializeField] private Button victoryRestartButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        if (gameOverRestartButton != null)
            gameOverRestartButton.onClick.AddListener(RestartGame);

        if (victoryRestartButton != null)
            victoryRestartButton.onClick.AddListener(RestartGame);
    }

    private void Update()
    {
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        UpdateCoinsText();
        UpdateObjectiveText();
    }

    private void UpdateCoinsText()
    {
        if (coinsText == null)
            return;

        if (CollectibleManager.Instance == null)
            return;

        coinsText.text =
            "Pièces : " +
            CollectibleManager.Instance.CoinCount +
            " / " +
            CollectibleManager.Instance.TotalCoins;
    }

    private void UpdateObjectiveText()
    {
        if (objectiveText == null)
            return;

        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.HasObjective())
        {
            objectiveText.text =
                "Objectif : rejoindre la sortie";
        }
        else
        {
            objectiveText.text =
                "Objectif : récupérer le trésor";
        }
    }

    public void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (gameOverText != null)
        {
            gameOverText.text =
                "Epinglé !";
        }
    }

    public void ShowVictoryPanel()
    {
        if (victoryPanel != null)
            victoryPanel.SetActive(true);

        int coins = 0;
        int totalCoins = 0;

        if (CollectibleManager.Instance != null)
        {
            coins = CollectibleManager.Instance.CoinCount;
            totalCoins = CollectibleManager.Instance.TotalCoins;
        }

        bool timeStar = false;
        bool coinsStar = false;
        bool stealthStar = false;

        float gameTime = 0f;
        float timeLimit = 0f;

        if (GameManager.Instance != null)
        {
            gameTime = GameManager.Instance.GameTimer;
            timeLimit = GameManager.Instance.TimeLimitForStar;

            timeStar = gameTime <= timeLimit;
            stealthStar = !GameManager.Instance.PlayerWasSeen;
        }

        coinsStar =
            totalCoins > 0 &&
            coins >= totalCoins;

        SetStar(timeStarImage, timeStar);
        SetStar(coinsStarImage, coinsStar);
        SetStar(stealthStarImage, stealthStar);

        int starCount = 0;

        if (timeStar)
            starCount++;

        if (coinsStar)
            starCount++;

        if (stealthStar)
            starCount++;

        if (victoryText != null)
        {
            victoryText.text =
                "Mission réussie !\n" +
                "Temps : " + Mathf.FloorToInt(gameTime) + "s / " + Mathf.FloorToInt(timeLimit) + "s\n" +
                "Pièces : " + coins + " / " + totalCoins + "\n" ;
        }
    }

    private void SetStar(Image starImage, bool unlocked)
    {
        if (starImage == null)
            return;

        starImage.sprite =
            unlocked ? fullStarSprite : emptyStarSprite;

        starImage.enabled = true;
    }
    private void RestartGame()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RestartGame();
    }
}
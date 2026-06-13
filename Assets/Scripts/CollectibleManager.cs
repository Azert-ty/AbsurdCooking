using UnityEngine;

public class CollectibleManager : MonoBehaviour
{
    public static CollectibleManager Instance { get; private set; }

    [Header("Coins")]
    [SerializeField] private int coinCount;
    [SerializeField] private int totalCoins;

    public int CoinCount => coinCount;
    public int TotalCoins => totalCoins;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        CountCoinsInScene();
    }

    private void CountCoinsInScene()
    {
        Coin[] coins = FindObjectsByType<Coin>(
            FindObjectsSortMode.None);

        totalCoins = coins.Length;
    }

    public void AddCoin(int value)
    {
        coinCount += value;

        Debug.Log("Pièces : " + coinCount + " / " + totalCoins);
    }
}
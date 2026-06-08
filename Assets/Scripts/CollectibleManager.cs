using UnityEngine;

public class CollectibleManager : MonoBehaviour
{
    public static CollectibleManager Instance { get; private set; }

    [Header("Collectibles")]
    [SerializeField] private int coinCount;
    [SerializeField] private bool hasTreasure;

    public int CoinCount => coinCount;
    public bool HasTreasure => hasTreasure;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddCoin(int value)
    {
        coinCount += value;

        Debug.Log("Pièces : " + coinCount);
    }

    public void CollectTreasure()
    {
        hasTreasure = true;

        Debug.Log("Trésor récupéré !");
    }
}
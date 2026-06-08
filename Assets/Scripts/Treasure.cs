using UnityEngine;

public class Treasure : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        CollectibleManager.Instance.CollectTreasure();

        Destroy(gameObject);
    }
}
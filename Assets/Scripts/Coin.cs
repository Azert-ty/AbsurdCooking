using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Coin : MonoBehaviour
{
    [SerializeField] private int value = 1;

    [Header("Feedback")]
    [SerializeField] private GameObject collectEffect;
    [SerializeField] private AudioClip collectSound;

    private bool collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
            return;

        if (!other.CompareTag("Player"))
            return;

        collected = true;

        CollectibleManager.Instance.AddCoin(value);

        PlayFeedback();

        Destroy(gameObject);
    }

    private void PlayFeedback()
    {
        if (collectEffect != null)
        {
            Instantiate(
                collectEffect,
                transform.position,
                Quaternion.identity);
        }

        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(
                collectSound,
                transform.position);
        }
    }
}
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Treasure : MonoBehaviour
{
    [Header("Feedback")]
    [SerializeField] private GameObject collectEffect;
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioSource audioSource;

    [SerializeField, Range(0f, 1f)]
    private float volume = 0.8f;

    private bool collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
            return;

        if (!other.CompareTag("Player"))
            return;

        collected = true;

        GameManager.Instance.SetObjectiveCollected();

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

        if (collectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(
                collectSound,
                volume);
        }
    }
}
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Treasure : MonoBehaviour
{
    [Header("Feedback")]
    [SerializeField] private GameObject collectEffect;
    [SerializeField] private AudioClip collectSound;

    [SerializeField, Range(0f, 1f)]
    private float volume = 0.8f;

    [Header("Visuals To Hide")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private bool collected;
    private Collider2D treasureCollider;

    private void Awake()
    {
        treasureCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
            return;

        if (!other.CompareTag("Player"))
            return;

        collected = true;

        if (GameManager.Instance != null)
            GameManager.Instance.SetObjectiveCollected();

        PlayFeedback();

        HideTreasure();

        Destroy(gameObject, GetDestroyDelay());
    }

    private void PlayFeedback()
    {
        if (collectEffect != null)
        {
            Instantiate(
                collectEffect,
                transform.position,
                Quaternion.identity
            );
        }

        if (collectSound != null)
        {
            PlaySound2D(
                collectSound,
                volume
            );
        }
    }

    private void PlaySound2D(AudioClip clip, float soundVolume)
    {
        GameObject soundObject = new GameObject("Treasure Collect Sound");

        AudioSource source = soundObject.AddComponent<AudioSource>();

        source.clip = clip;
        source.volume = soundVolume;
        source.spatialBlend = 0f;
        source.playOnAwake = false;

        source.Play();

        Destroy(soundObject, clip.length + 0.1f);
    }

    private void HideTreasure()
    {
        if (treasureCollider != null)
            treasureCollider.enabled = false;

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    private float GetDestroyDelay()
    {
        if (collectSound == null)
            return 0.1f;

        return collectSound.length + 0.1f;
    }
}
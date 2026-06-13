

using UnityEngine;

public class EnemyFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer enemyRenderer;

    [Header("State Colors")]
    [SerializeField] private Color patrolColor = Color.white;
    [SerializeField] private Color alertColor = Color.red;
    [SerializeField] private Color chaseColor = Color.red;
    [SerializeField] private Color searchColor = Color.yellow;

    [Header("Icons")]
    [SerializeField] private GameObject alertIcon;
    [SerializeField] private GameObject searchIcon;

    [Header("Sounds")]
    [SerializeField] private AudioClip alertSound;
    [SerializeField] private AudioClip chaseSound;
    [SerializeField] private AudioClip searchSound;

    [Header("Sound Cooldowns")]
    [SerializeField] private float alertSoundCooldown = 1f;
    [SerializeField] private float chaseSoundCooldown = 1f;
    [SerializeField] private float searchSoundCooldown = 1f;

    private float lastAlertSoundTime = -999f;
    private float lastChaseSoundTime = -999f;
    private float lastSearchSoundTime = -999f;

    private AudioSource audioSource;

    private void Awake()
    {
        if (enemyRenderer == null)
            enemyRenderer = GetComponent<SpriteRenderer>();

        audioSource = GetComponent<AudioSource>();

        HideIcons();
    }

    public void ShowPatrol()
    {
        SetColor(patrolColor);
        HideIcons();
    }

    public void ShowAlert()
    {
        SetColor(alertColor);
        ShowOnly(alertIcon);

        PlaySound(
            alertSound,
            ref lastAlertSoundTime,
            alertSoundCooldown);
    }

    public void ShowChase()
    {
        SetColor(chaseColor);
        HideIcons();

        PlaySound(
            chaseSound,
            ref lastChaseSoundTime,
            chaseSoundCooldown);
    }

    public void ShowSearch()
    {
        SetColor(searchColor);
        ShowOnly(searchIcon);

        PlaySound(
            searchSound,
            ref lastSearchSoundTime,
            searchSoundCooldown);
    }

    private void SetColor(Color color)
    {
        if (enemyRenderer != null)
            enemyRenderer.color = color;
    }

    private void HideIcons()
    {
        if (alertIcon != null)
            alertIcon.SetActive(false);

        if (searchIcon != null)
            searchIcon.SetActive(false);
    }

    private void ShowOnly(GameObject icon)
    {
        HideIcons();

        if (icon != null)
            icon.SetActive(true);
    }

    private void PlaySound(
        AudioClip clip,
        ref float lastPlayTime,
        float cooldown)
    {
        if (clip == null)
            return;

        if (Time.time < lastPlayTime + cooldown)
            return;

        lastPlayTime = Time.time;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(
                clip,
                transform.position);
        }
    }

    public void SetupReferences(
    SpriteRenderer renderer,
    GameObject alert,
    GameObject search)
{
    enemyRenderer = renderer;
    alertIcon = alert;
    searchIcon = search;

    HideIcons();
}
}
using UnityEngine;

public class EnemyContactWarning : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyVision enemyVision;
    [SerializeField] private SpriteRenderer warningRenderer;

    [Header("Distances")]
    [Tooltip("Distance à laquelle le feedback commence à apparaître.")]
    [SerializeField] private float warningDistance = 1.1f;

    [Tooltip("Distance à laquelle le danger devient critique.")]
    [SerializeField] private float criticalDistance = 0.7f;

    [Tooltip("Distance réelle de capture.")]
    [SerializeField] private float captureDistance = 0.5f;

    [Header("Appearance")]
    [SerializeField] private float normalAlpha = 0.15f;
    [SerializeField] private float criticalAlpha = 0.45f;

    [Header("Pulse")]
    [SerializeField] private float pulseSpeed = 6f;
    [SerializeField] private float pulseAmount = 0.12f;

    [Header("Visual Size")]
    [SerializeField] private float ellipseHeightRatio = 0.55f;

    private Vector3 baseScale;

    private void Awake()
    {
        if (enemyVision == null)
            enemyVision = GetComponentInParent<EnemyVision>();

        if (warningRenderer == null)
            warningRenderer = GetComponent<SpriteRenderer>();

        ResizeEllipseToWarningDistance();

        SetAlpha(0f);
    }

    private void Update()
    {
        if (enemyVision == null ||
            enemyVision.Player == null ||
            warningRenderer == null)
        {
            return;
        }

        float distance =
            Vector2.Distance(
                transform.parent.position,
                enemyVision.Player.position
            );

        // -------------------------------------------------
        // JOUEUR LOIN
        // -------------------------------------------------

        if (distance >= warningDistance)
        {
            SetAlpha(0f);

            transform.localScale =
                Vector3.Lerp(
                    transform.localScale,
                    baseScale,
                    10f * Time.deltaTime
                );

            return;
        }

        // -------------------------------------------------
        // JOUEUR PROCHE
        // -------------------------------------------------

        if (distance > criticalDistance)
        {
            float proximity =
                Mathf.InverseLerp(
                    warningDistance,
                    criticalDistance,
                    distance
                );

            float alpha =
                Mathf.Lerp(
                    normalAlpha,
                    criticalAlpha,
                    proximity
                );

            SetAlpha(alpha);

            transform.localScale =
                Vector3.Lerp(
                    transform.localScale,
                    baseScale,
                    10f * Time.deltaTime
                );

            return;
        }

        // -------------------------------------------------
        // DANGER CRITIQUE
        // -------------------------------------------------

        float pulse =
            1f +
            Mathf.Sin(Time.time * pulseSpeed) *
            pulseAmount;

        transform.localScale =
            baseScale * pulse;

        float criticalProximity =
            Mathf.InverseLerp(
                criticalDistance,
                captureDistance,
                distance
            );

        float criticalPulseAlpha =
            criticalAlpha +
            Mathf.Sin(Time.time * pulseSpeed) * 0.1f;

        SetAlpha(
            Mathf.Clamp01(
                criticalPulseAlpha +
                criticalProximity * 0.2f
            )
        );
    }

    private void SetAlpha(float alpha)
    {
        Color color =
            warningRenderer.color;

        color.a = alpha;

        warningRenderer.color =
            color;
    }

    private void ResizeEllipseToWarningDistance()
    {
        if (warningRenderer == null)
            return;

        Sprite sprite = warningRenderer.sprite;

        if (sprite == null)
            return;

        Vector2 spriteSize =
            sprite.bounds.size;

        if (spriteSize.x <= 0f ||
            spriteSize.y <= 0f)
        {
            return;
        }

        float targetWidth =
            warningDistance * 2f;

        float targetHeight =
            targetWidth * ellipseHeightRatio;

        transform.localScale =
            new Vector3(
                targetWidth / spriteSize.x,
                targetHeight / spriteSize.y,
                1f
            );

        baseScale =
            transform.localScale;
    }
}
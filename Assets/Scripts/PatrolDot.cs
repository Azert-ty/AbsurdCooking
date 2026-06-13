using UnityEngine;

public class PatrolDot : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Colors")]
    [SerializeField] private Color futureColor = Color.red;
    [SerializeField] private Color passedColor = Color.white;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetPassed(bool passed)
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.color =
            passed ? passedColor : futureColor;
    }
}
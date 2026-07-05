using UnityEngine;

[DisallowMultipleComponent]
public class SortByY : MonoBehaviour
{
    [Header("Sprite à trier")]
    [Tooltip("Laisse vide : le SpriteRenderer sera trouvé automatiquement.")]
    [SerializeField] private SpriteRenderer objectRenderer;

    [Header("Correction du point de tri")]
    [Tooltip("Permet d'avancer ou reculer légèrement le point de tri.")]
    [SerializeField] private float sortYOffset = 0f;

    [Header("Réglages")]
    [SerializeField] private int baseOrder = 10000;

    [Tooltip("Précision du tri selon la position Y.")]
    [SerializeField] private int precision = 100;

    [Tooltip("Correction manuelle de l'ordre final.")]
    [SerializeField] private int sortingOffset = 0;

    private float automaticSortOffsetY;
    private bool sortOffsetInitialized;

    private int lastSortingOrder = int.MinValue;

    private void Awake()
    {
        FindSpriteRendererIfNeeded();
        InitializeAutomaticSortPoint();
        UpdateSortingOrder();
    }

    private void Reset()
    {
        objectRenderer = null;
        FindSpriteRendererIfNeeded();
    }

    private void LateUpdate()
    {
        if (objectRenderer == null)
        {
            FindSpriteRendererIfNeeded();

            if (objectRenderer == null)
                return;
        }

        if (!sortOffsetInitialized)
            InitializeAutomaticSortPoint();

        UpdateSortingOrder();
    }

    private void FindSpriteRendererIfNeeded()
    {
        if (objectRenderer != null)
            return;

        // 1. Cas normal :
        // le SpriteRenderer est directement sur le GameObject.
        objectRenderer = GetComponent<SpriteRenderer>();

        if (objectRenderer != null)
            return;

        // 2. Cas du policier :
        // cherche d'abord un enfant nommé "Visual".
        Transform visual = transform.Find("Visual");

        if (visual != null)
        {
            objectRenderer =
                visual.GetComponentInChildren<SpriteRenderer>(true);

            if (objectRenderer != null)
                return;
        }

        // 3. Dernier recours :
        // prend le plus grand SpriteRenderer enfant.
        objectRenderer = FindLargestChildSpriteRenderer();
    }

    private SpriteRenderer FindLargestChildSpriteRenderer()
    {
        SpriteRenderer[] renderers =
            GetComponentsInChildren<SpriteRenderer>(true);

        SpriteRenderer bestRenderer = null;
        float largestArea = -1f;

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || renderer.sprite == null)
                continue;

            Vector2 size = renderer.bounds.size;
            float area = size.x * size.y;

            if (area > largestArea)
            {
                largestArea = area;
                bestRenderer = renderer;
            }
        }

        return bestRenderer;
    }

    private void InitializeAutomaticSortPoint()
    {
        if (objectRenderer == null)
            return;

        // Calcule une seule fois la distance entre
        // le centre du GameObject et le bas visible du sprite.
        automaticSortOffsetY =
            objectRenderer.bounds.min.y - transform.position.y;

        sortOffsetInitialized = true;
    }

    private void UpdateSortingOrder()
    {
        if (objectRenderer == null)
            return;

        float sortY =
            transform.position.y
            + automaticSortOffsetY
            + sortYOffset;

        int newSortingOrder =
            baseOrder
            - Mathf.RoundToInt(sortY * precision)
            + sortingOffset;

        if (newSortingOrder == lastSortingOrder)
            return;

        objectRenderer.sortingOrder = newSortingOrder;
        lastSortingOrder = newSortingOrder;
    }
}
using UnityEngine;

public class SortByY : MonoBehaviour
{
    [Header("Renderer à trier")]
    [SerializeField] private Renderer objectRenderer;

    [Header("Point utilisé pour le tri")]
    [Tooltip("Laisse vide pour utiliser la position de ce GameObject.")]
    [SerializeField] private Transform sortPoint;

    [Header("Réglages")]
    [SerializeField] private int precision = 100;
    [SerializeField] private int sortingOffset = 0;

    private void Awake()
    {
        FindRendererIfNeeded();

        if (sortPoint == null)
            sortPoint = transform;
    }

    private void Reset()
    {
        FindRendererIfNeeded();
    }

    private void LateUpdate()
    {
        if (objectRenderer == null)
        {
            FindRendererIfNeeded();

            if (objectRenderer == null)
                return;
        }

        if (sortPoint == null)
            sortPoint = transform;

        objectRenderer.sortingOrder =
            Mathf.RoundToInt(-sortPoint.position.y * precision) + sortingOffset;
    }

    private void FindRendererIfNeeded()
    {
        if (objectRenderer != null)
            return;

        objectRenderer = GetComponent<Renderer>();

        if (objectRenderer == null)
            objectRenderer = GetComponentInChildren<Renderer>();
    }
}
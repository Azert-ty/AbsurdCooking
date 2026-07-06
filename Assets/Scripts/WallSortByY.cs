using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
public class WallSortByY : MonoBehaviour
{
    // =========================================================
    // MÊMES VALEURS QUE SORT BY Y
    // =========================================================

    private const string SharedSortingLayer = "Default";

    private const int BaseOrder = 12200;

    private const int Precision = 100;

    // =========================================================
    // SORT POINT
    // =========================================================

    [Header("Sort Point")]

    [Tooltip(
        "Point représentant le bas du mur. " +
        "C'est ce Y qui sera utilisé pour le tri."
    )]
    [SerializeField]
    private Transform sortPoint;

    // =========================================================
    // FALLBACK REFERENCES
    // =========================================================

    [Header("Automatic Fallback")]

    [SerializeField]
    private Collider2D wallCollider;

    // =========================================================
    // INTERNAL
    // =========================================================

    private SortingGroup sortingGroup;

    private Renderer targetRenderer;

    private int lastSortingOrder =
        int.MinValue;

    // =========================================================
    // UNITY
    // =========================================================

    private void Reset()
    {
        AutoSetup();

        UpdateSorting(true);
    }

    private void Awake()
    {
        AutoSetup();

        UpdateSorting(true);
    }

    private void OnEnable()
    {
        AutoSetup();

        UpdateSorting(true);
    }

    private void OnValidate()
    {
        AutoSetup();

        UpdateSorting(true);
    }

    // =========================================================
    // AUTO SETUP
    // =========================================================

    private void AutoSetup()
    {
        // -----------------------------------------------------
        // SORTING GROUP
        // -----------------------------------------------------

        sortingGroup =
            GetComponent<SortingGroup>();

        // -----------------------------------------------------
        // RENDERER
        //
        // Fonctionne avec :
        // - SpriteRenderer
        // - TilemapRenderer
        // - MeshRenderer
        // -----------------------------------------------------

        if (sortingGroup == null)
        {
            targetRenderer =
                GetComponent<Renderer>();

            if (targetRenderer == null)
            {
                targetRenderer =
                    GetComponentInChildren<Renderer>(
                        true
                    );
            }
        }

        // -----------------------------------------------------
        // COLLIDER DE SECOURS
        // -----------------------------------------------------

        if (wallCollider == null)
        {
            Collider2D[] colliders =
                GetComponentsInChildren<Collider2D>(
                    true
                );

            foreach (
                Collider2D collider
                in colliders)
            {
                if (collider == null)
                    continue;

                if (!collider.enabled)
                    continue;

                if (collider.isTrigger)
                    continue;

                wallCollider = collider;

                break;
            }
        }
    }

    // =========================================================
    // SORT Y
    // =========================================================

    private float GetSortY()
    {
        // -----------------------------------------------------
        // 1. POINT MANUEL
        //
        // C'est la solution recommandée pour les murs.
        // -----------------------------------------------------

        if (sortPoint != null)
        {
            return sortPoint.position.y;
        }

        // -----------------------------------------------------
        // 2. BAS DU COLLIDER
        // -----------------------------------------------------

        if (wallCollider != null &&
            wallCollider.enabled)
        {
            return wallCollider.bounds.min.y;
        }

        // -----------------------------------------------------
        // 3. BAS DU RENDERER
        // -----------------------------------------------------

        if (targetRenderer != null)
        {
            return targetRenderer.bounds.min.y;
        }

        // -----------------------------------------------------
        // 4. DERNIER RECOURS
        // -----------------------------------------------------

        return transform.position.y;
    }

    // =========================================================
    // SORTING
    // =========================================================

    private void UpdateSorting(
        bool forceUpdate)
    {
        if (sortingGroup == null &&
            targetRenderer == null)
        {
            return;
        }

        float sortY =
            GetSortY();

        int newSortingOrder =
            BaseOrder -
            Mathf.RoundToInt(
                sortY * Precision
            );

        if (!forceUpdate &&
            newSortingOrder ==
            lastSortingOrder)
        {
            return;
        }

        // -----------------------------------------------------
        // SORTING GROUP
        // -----------------------------------------------------

        if (sortingGroup != null)
        {
            sortingGroup.sortingLayerName =
                SharedSortingLayer;

            sortingGroup.sortingOrder =
                newSortingOrder;
        }

        // -----------------------------------------------------
        // RENDERER
        // -----------------------------------------------------

        else
        {
            targetRenderer.sortingLayerName =
                SharedSortingLayer;

            targetRenderer.sortingOrder =
                newSortingOrder;
        }

        lastSortingOrder =
            newSortingOrder;
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        float y =
            GetSortY();

        Gizmos.color =
            Color.magenta;

        Vector3 center =
            new Vector3(
                transform.position.x,
                y,
                transform.position.z
            );

        Gizmos.DrawSphere(
            center,
            0.12f
        );

        Gizmos.DrawLine(
            center + Vector3.left * 2f,
            center + Vector3.right * 2f
        );
    }
}
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class SortByY : MonoBehaviour
{
    // =========================================================
    // GLOBAL SETTINGS
    // =========================================================

    // Tous les objets qui utilisent SortByY doivent être
    // dans la même Sorting Layer pour pouvoir se croiser.
    private const string SharedSortingLayer = "Default";

    private const int BaseOrder = 10000;
    private const int Precision = 100;

    // =========================================================
    // REFERENCES
    // =========================================================

    private SortingGroup sortingGroup;

    private SpriteRenderer targetRenderer;

    private SpriteRenderer[] allSpriteRenderers;

    private Collider2D groundCollider;

    // =========================================================
    // CACHE
    // =========================================================

    private int lastSortingOrder =
        int.MinValue;

    // =========================================================
    // UNITY
    // =========================================================

    private void Reset()
    {
        AutoSetup(true);

        ForceSharedSortingLayer();

        UpdateSortingOrder(true);
    }

    private void Awake()
    {
        AutoSetup(true);

        ForceSharedSortingLayer();

        UpdateSortingOrder(true);
    }

    private void OnEnable()
    {
        ForceSharedSortingLayer();

        UpdateSortingOrder(true);
    }

    private void LateUpdate()
    {
        UpdateSortingOrder(false);
    }

    // =========================================================
    // AUTO SETUP
    // =========================================================

    private void AutoSetup(
        bool allowSortingGroupCreation)
    {
        FindRenderers(
            allowSortingGroupCreation
        );

        FindGroundCollider();
    }

    // =========================================================
    // RENDERERS
    // =========================================================

    private void FindRenderers(
        bool allowSortingGroupCreation)
    {
        allSpriteRenderers =
            GetComponentsInChildren<SpriteRenderer>(
                true
            );

        // -----------------------------------------------------
        // 1. SORTING GROUP DÉJÀ PRÉSENT
        // -----------------------------------------------------

        sortingGroup =
            GetComponent<SortingGroup>();

        if (sortingGroup != null)
        {
            targetRenderer = null;

            return;
        }

        // -----------------------------------------------------
        // 2. PLUSIEURS SPRITES
        //
        // On crée automatiquement un SortingGroup.
        // -----------------------------------------------------

        if (allSpriteRenderers.Length > 1 &&
            allowSortingGroupCreation)
        {
            sortingGroup =
                gameObject.AddComponent<SortingGroup>();

            targetRenderer = null;

            return;
        }

        // -----------------------------------------------------
        // 3. SPRITE DIRECTEMENT SUR L'OBJET
        // -----------------------------------------------------

        targetRenderer =
            GetComponent<SpriteRenderer>();

        if (targetRenderer != null)
        {
            return;
        }

        // -----------------------------------------------------
        // 4. CAS DU POLICIER AVEC ENFANT "Visual"
        // -----------------------------------------------------

        Transform visual =
            transform.Find("Visual");

        if (visual != null)
        {
            targetRenderer =
                visual.GetComponentInChildren<SpriteRenderer>(
                    true
                );

            if (targetRenderer != null)
            {
                return;
            }
        }

        // -----------------------------------------------------
        // 5. DERNIER RECOURS
        //
        // Utilise le plus grand sprite enfant.
        // -----------------------------------------------------

        targetRenderer =
            FindLargestSpriteRenderer(
                allSpriteRenderers
            );
    }

    private SpriteRenderer FindLargestSpriteRenderer(
        SpriteRenderer[] renderers)
    {
        SpriteRenderer bestRenderer = null;

        float largestArea = -1f;

        foreach (
            SpriteRenderer renderer
            in renderers)
        {
            if (renderer == null ||
                renderer.sprite == null)
            {
                continue;
            }

            Vector2 size =
                renderer.bounds.size;

            float area =
                size.x * size.y;

            if (area > largestArea)
            {
                largestArea = area;

                bestRenderer = renderer;
            }
        }

        return bestRenderer;
    }

    // =========================================================
    // FORCE SAME SORTING LAYER
    // =========================================================

    private void ForceSharedSortingLayer()
    {
        // -----------------------------------------------------
        // OBJET COMPLEXE AVEC SORTING GROUP
        // -----------------------------------------------------

        if (sortingGroup != null)
        {
            sortingGroup.sortingLayerName =
                SharedSortingLayer;

            return;
        }

        // -----------------------------------------------------
        // OBJET SIMPLE AVEC UN SEUL SPRITE
        // -----------------------------------------------------

        if (targetRenderer != null)
        {
            targetRenderer.sortingLayerName =
                SharedSortingLayer;
        }
    }

    // =========================================================
    // COLLIDER USED FOR SORTING
    // =========================================================

    private void FindGroundCollider()
    {
        groundCollider = null;

        // -----------------------------------------------------
        // 1. COLLIDER PHYSIQUE DIRECT
        // -----------------------------------------------------

        Collider2D directCollider =
            GetComponent<Collider2D>();

        if (IsValidPhysicalCollider(
                directCollider))
        {
            groundCollider =
                directCollider;

            return;
        }

        // -----------------------------------------------------
        // 2. COLLIDERS ENFANTS
        // -----------------------------------------------------

        Collider2D[] colliders =
            GetComponentsInChildren<Collider2D>(
                true
            );

        Collider2D lowestPhysicalCollider =
            null;

        float lowestY =
            float.PositiveInfinity;

        foreach (
            Collider2D collider
            in colliders)
        {
            if (!IsValidPhysicalCollider(
                    collider))
            {
                continue;
            }

            float colliderY =
                collider.bounds.center.y;

            if (colliderY < lowestY)
            {
                lowestY =
                    colliderY;

                lowestPhysicalCollider =
                    collider;
            }
        }

        groundCollider =
            lowestPhysicalCollider;
    }

    private bool IsValidPhysicalCollider(
        Collider2D collider)
    {
        return collider != null &&
               collider.enabled &&
               !collider.isTrigger;
    }

    // =========================================================
    // AUTOMATIC SORT POINT
    // =========================================================

    private float GetSortY()
    {
        // -----------------------------------------------------
        // MEILLEUR CAS :
        // BAS DU COLLIDER PHYSIQUE
        // -----------------------------------------------------

        if (groundCollider != null &&
            groundCollider.enabled)
        {
            return groundCollider.bounds.min.y;
        }

        // -----------------------------------------------------
        // UN SEUL SPRITE
        // -----------------------------------------------------

        if (targetRenderer != null)
        {
            return targetRenderer.bounds.min.y;
        }

        // -----------------------------------------------------
        // PLUSIEURS SPRITES DANS UN SORTING GROUP
        // -----------------------------------------------------

        if (allSpriteRenderers != null &&
            allSpriteRenderers.Length > 0)
        {
            float lowestY =
                float.PositiveInfinity;

            bool foundRenderer = false;

            foreach (
                SpriteRenderer renderer
                in allSpriteRenderers)
            {
                if (renderer == null ||
                    !renderer.enabled)
                {
                    continue;
                }

                lowestY =
                    Mathf.Min(
                        lowestY,
                        renderer.bounds.min.y
                    );

                foundRenderer = true;
            }

            if (foundRenderer)
            {
                return lowestY;
            }
        }

        // -----------------------------------------------------
        // DERNIER RECOURS
        // -----------------------------------------------------

        return transform.position.y;
    }

    // =========================================================
    // SORTING
    // =========================================================

    private void UpdateSortingOrder(
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
        // SPRITE SIMPLE
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
}
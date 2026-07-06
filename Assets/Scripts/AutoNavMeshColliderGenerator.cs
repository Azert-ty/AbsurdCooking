using UnityEngine;

[DisallowMultipleComponent]
public class AutoNavMeshColliderGenerator : MonoBehaviour
{
    private const string GeneratedName = "__AUTO_NAV_COLLIDER__";

    private const float NavMeshZPosition = 1f;
    private const float NavMeshColliderSizeZ = 1f;

    [ContextMenu("Generate / Refresh All NavMesh Colliders")]
    public void GenerateAll()
    {
        Collider2D[] allColliders =
            FindObjectsByType<Collider2D>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        int count = 0;

        foreach (Collider2D collider2D in allColliders)
        {
            if (!IsValidSource(collider2D))
                continue;

            GenerateFor(collider2D);
            count++;
        }

        Debug.Log(
            $"[AutoNavMesh] {count} colliders générés ou mis à jour."
        );
    }

    private bool IsValidSource(Collider2D collider2D)
    {
        if (collider2D == null)
            return false;

        if (!collider2D.enabled)
            return false;

        if (collider2D.isTrigger)
            return false;

        return true;
    }

    private void GenerateFor(Collider2D source)
    {
        GameObject generatedObject =
            GetOrCreateGeneratedObject(source.transform);

        BoxCollider navMeshCollider =
            generatedObject.GetComponent<BoxCollider>();

        if (navMeshCollider == null)
        {
            navMeshCollider =
                generatedObject.AddComponent<BoxCollider>();
        }

        CopyCollider2DToBoxCollider(
            source,
            generatedObject.transform,
            navMeshCollider
        );
    }

    private GameObject GetOrCreateGeneratedObject(
        Transform sourceTransform)
    {
        Transform existing =
            sourceTransform.Find(GeneratedName);

        if (existing != null)
            return existing.gameObject;

        GameObject generated =
            new GameObject(GeneratedName);

        generated.transform.SetParent(
            sourceTransform,
            true
        );

        return generated;
    }

    private void CopyCollider2DToBoxCollider(
        Collider2D source,
        Transform generatedTransform,
        BoxCollider navMeshCollider)
    {
        Bounds bounds = source.bounds;

        // Position :
        // même centre X/Y que le Collider2D
        // mais Z TOUJOURS égal à 1.
        generatedTransform.position =
            new Vector3(
                bounds.center.x,
                bounds.center.y,
                NavMeshZPosition
            );

        generatedTransform.rotation =
            Quaternion.identity;

        generatedTransform.localScale =
            Vector3.one;

        navMeshCollider.center =
            Vector3.zero;

        // Taille :
        // X du Collider2D
        // Y du Collider2D
        // Z toujours égal à 1.
        navMeshCollider.size =
            new Vector3(
                bounds.size.x,
                bounds.size.y,
                NavMeshColliderSizeZ
            );

        navMeshCollider.isTrigger = false;
    }
}
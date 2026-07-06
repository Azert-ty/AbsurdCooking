using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class AutoNavMeshTilemapColliderGenerator : MonoBehaviour
{
    private const string GeneratedParentName =
        "__AUTO_NAV_TILEMAP_COLLIDERS__";

    private const string GeneratedColliderName =
        "__AUTO_NAV_TILEMAP_COLLIDER__";

    // RÈGLE CRITIQUE DU PROJET.
    private const float NavMeshPositionZ = 1f;
    private const float NavMeshSizeZ = 1f;

    // =========================================================
    // GÉNÉRATION
    // =========================================================

    [ContextMenu("Generate / Refresh Tilemap NavMesh Colliders")]
    public void GenerateAll()
    {
        TilemapRenderer[] allRenderers =
            FindObjectsByType<TilemapRenderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        int tilemapCount = 0;
        int colliderCount = 0;

        foreach (TilemapRenderer tilemapRenderer in allRenderers)
        {
            if (!TryGetValidTilemap(
                    tilemapRenderer,
                    out Tilemap tilemap,
                    out TilemapCollider2D tilemapCollider))
            {
                continue;
            }

            int generated =
                GenerateForTilemap(
                    tilemap,
                    tilemapCollider
                );

            if (generated > 0)
            {
                tilemapCount++;
                colliderCount += generated;
            }
        }

        Debug.Log(
            $"[AutoNavMeshTilemap] " +
            $"{colliderCount} colliders générés " +
            $"sur {tilemapCount} Tilemaps."
        );
    }

    // =========================================================
    // VALIDATION TILEMAP
    // =========================================================

    private bool TryGetValidTilemap(
        TilemapRenderer renderer,
        out Tilemap tilemap,
        out TilemapCollider2D tilemapCollider)
    {
        tilemap =
            renderer.GetComponent<Tilemap>();

        tilemapCollider =
            renderer.GetComponent<TilemapCollider2D>();

        if (tilemap == null)
            return false;

        // Pas de TilemapCollider2D :
        // probablement sol ou décoration.
        if (tilemapCollider == null)
            return false;

        if (!tilemapCollider.enabled)
            return false;

        if (tilemapCollider.isTrigger)
            return false;

        return true;
    }

    // =========================================================
    // GÉNÉRATION POUR UNE TILEMAP
    // =========================================================

    private int GenerateForTilemap(
        Tilemap tilemap,
        TilemapCollider2D tilemapCollider)
    {
        // Supprime les anciens colliders générés.
        DeleteGeneratedParent(tilemap.transform);

        // Trouve uniquement les cellules
        // qui possèdent réellement un collider.
        HashSet<Vector3Int> collidableCells =
            FindCollidableCells(tilemap);

        if (collidableCells.Count == 0)
            return 0;

        // Transforme les cellules connectées
        // en rectangles continus.
        List<CellRectangle> rectangles =
            BuildRectangles(
                tilemap,
                collidableCells
            );

        // Parent commun à tous les colliders
        // de cette Tilemap.
        GameObject generatedParent =
            CreateGeneratedParent(tilemap.transform);

        int index = 0;

        foreach (CellRectangle rectangle in rectangles)
        {
            CreateNavMeshCollider(
                tilemap,
                generatedParent.transform,
                rectangle,
                index
            );

            index++;
        }

        return index;
    }

    // =========================================================
    // TROUVE LES TILES AVEC COLLISION
    // =========================================================

    private HashSet<Vector3Int> FindCollidableCells(
        Tilemap tilemap)
    {
        HashSet<Vector3Int> result =
            new HashSet<Vector3Int>();

        BoundsInt bounds = tilemap.cellBounds;

        for (int z = bounds.zMin; z < bounds.zMax; z++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    Vector3Int cell =
                        new Vector3Int(x, y, z);

                    if (!tilemap.HasTile(cell))
                        continue;

                    Tile.ColliderType colliderType =
                        tilemap.GetColliderType(cell);

                    if (colliderType ==
                        Tile.ColliderType.None)
                    {
                        continue;
                    }

                    result.Add(cell);
                }
            }
        }

        return result;
    }

    // =========================================================
    // FUSION DES CELLULES EN RECTANGLES
    // =========================================================

    private List<CellRectangle> BuildRectangles(
        Tilemap tilemap,
        HashSet<Vector3Int> cells)
    {
        List<CellRectangle> rectangles =
            new List<CellRectangle>();

        HashSet<Vector3Int> remaining =
            new HashSet<Vector3Int>(cells);

        BoundsInt bounds = tilemap.cellBounds;

        // Parcours déterministe :
        // du bas vers le haut,
        // puis de gauche à droite.
        for (int z = bounds.zMin; z < bounds.zMax; z++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    Vector3Int start =
                        new Vector3Int(x, y, z);

                    if (!remaining.Contains(start))
                        continue;

                    // -----------------------------------------
                    // 1. Étend le mur vers la droite.
                    // -----------------------------------------

                    int width = 1;

                    while (remaining.Contains(
                        new Vector3Int(
                            x + width,
                            y,
                            z)))
                    {
                        width++;
                    }

                    // -----------------------------------------
                    // 2. Étend ensuite le rectangle vers le haut
                    // tant que toute la ligne continue.
                    // -----------------------------------------

                    int height = 1;

                    bool canGrow = true;

                    while (canGrow)
                    {
                        int nextY = y + height;

                        for (int currentX = 0;
                             currentX < width;
                             currentX++)
                        {
                            Vector3Int nextCell =
                                new Vector3Int(
                                    x + currentX,
                                    nextY,
                                    z
                                );

                            if (!remaining.Contains(nextCell))
                            {
                                canGrow = false;
                                break;
                            }
                        }

                        if (canGrow)
                            height++;
                    }

                    // -----------------------------------------
                    // 3. Retire les cellules utilisées.
                    // -----------------------------------------

                    for (int removeY = 0;
                         removeY < height;
                         removeY++)
                    {
                        for (int removeX = 0;
                             removeX < width;
                             removeX++)
                        {
                            remaining.Remove(
                                new Vector3Int(
                                    x + removeX,
                                    y + removeY,
                                    z
                                )
                            );
                        }
                    }

                    rectangles.Add(
                        new CellRectangle(
                            x,
                            y,
                            z,
                            width,
                            height
                        )
                    );
                }
            }
        }

        return rectangles;
    }

    // =========================================================
    // CRÉATION DU PARENT
    // =========================================================

    private GameObject CreateGeneratedParent(
        Transform tilemapTransform)
    {
        GameObject parent =
            new GameObject(GeneratedParentName);

        parent.transform.SetParent(
            tilemapTransform,
            false
        );

        parent.transform.localPosition =
            Vector3.zero;

        parent.transform.localRotation =
            Quaternion.identity;

        parent.transform.localScale =
            Vector3.one;

        return parent;
    }

    // =========================================================
    // CRÉATION D'UN BOX COLLIDER 3D
    // =========================================================

    private void CreateNavMeshCollider(
        Tilemap tilemap,
        Transform generatedParent,
        CellRectangle rectangle,
        int index)
    {
        GetRectangleWorldBounds(
            tilemap,
            rectangle,
            out Vector2 center,
            out Vector2 size
        );

        GameObject generatedObject =
            new GameObject(
                $"{GeneratedColliderName}_{index}"
            );

        generatedObject.transform.SetParent(
            generatedParent,
            true
        );

        // X / Y :
        // centre exact du segment de mur.
        //
        // Z :
        // TOUJOURS 1.
        generatedObject.transform.position =
            new Vector3(
                center.x,
                center.y,
                NavMeshPositionZ
            );

        generatedObject.transform.rotation =
            Quaternion.identity;

        generatedObject.transform.localScale =
            Vector3.one;

        BoxCollider boxCollider =
            generatedObject.AddComponent<BoxCollider>();

        boxCollider.center =
            Vector3.zero;

        // X / Y :
        // taille du mur continu.
        //
        // Z :
        // TOUJOURS 1.
        boxCollider.size =
            new Vector3(
                size.x,
                size.y,
                NavMeshSizeZ
            );

        boxCollider.isTrigger = false;
    }

    // =========================================================
    // CONVERSION CELLULES -> MONDE
    // =========================================================

    private void GetRectangleWorldBounds(
        Tilemap tilemap,
        CellRectangle rectangle,
        out Vector2 center,
        out Vector2 size)
    {
        Vector3Int bottomLeft =
            new Vector3Int(
                rectangle.x,
                rectangle.y,
                rectangle.z
            );

        Vector3Int bottomRight =
            new Vector3Int(
                rectangle.x + rectangle.width,
                rectangle.y,
                rectangle.z
            );

        Vector3Int topLeft =
            new Vector3Int(
                rectangle.x,
                rectangle.y + rectangle.height,
                rectangle.z
            );

        Vector3Int topRight =
            new Vector3Int(
                rectangle.x + rectangle.width,
                rectangle.y + rectangle.height,
                rectangle.z
            );

        Vector3 p1 =
            tilemap.CellToWorld(bottomLeft);

        Vector3 p2 =
            tilemap.CellToWorld(bottomRight);

        Vector3 p3 =
            tilemap.CellToWorld(topLeft);

        Vector3 p4 =
            tilemap.CellToWorld(topRight);

        float minX =
            Mathf.Min(
                p1.x,
                p2.x,
                p3.x,
                p4.x
            );

        float maxX =
            Mathf.Max(
                p1.x,
                p2.x,
                p3.x,
                p4.x
            );

        float minY =
            Mathf.Min(
                p1.y,
                p2.y,
                p3.y,
                p4.y
            );

        float maxY =
            Mathf.Max(
                p1.y,
                p2.y,
                p3.y,
                p4.y
            );

        center =
            new Vector2(
                (minX + maxX) * 0.5f,
                (minY + maxY) * 0.5f
            );

        size =
            new Vector2(
                maxX - minX,
                maxY - minY
            );
    }

    // =========================================================
    // SUPPRESSION
    // =========================================================

    [ContextMenu("Delete Tilemap NavMesh Colliders")]
    public void DeleteAllGenerated()
    {
        Tilemap[] tilemaps =
            FindObjectsByType<Tilemap>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        int deleted = 0;

        foreach (Tilemap tilemap in tilemaps)
        {
            Transform existing =
                tilemap.transform.Find(
                    GeneratedParentName
                );

            if (existing == null)
                continue;

            DeleteObject(existing.gameObject);
            deleted++;
        }

        Debug.Log(
            $"[AutoNavMeshTilemap] " +
            $"{deleted} groupes supprimés."
        );
    }

    private void DeleteGeneratedParent(
        Transform tilemapTransform)
    {
        Transform existing =
            tilemapTransform.Find(
                GeneratedParentName
            );

        if (existing == null)
            return;

        DeleteObject(existing.gameObject);
    }

    private void DeleteObject(GameObject obj)
    {
        if (Application.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
            DestroyImmediate(obj);
        }
    }

    // =========================================================
    // RECTANGLE DE CELLULES
    // =========================================================

    private readonly struct CellRectangle
    {
        public readonly int x;
        public readonly int y;
        public readonly int z;

        public readonly int width;
        public readonly int height;

        public CellRectangle(
            int x,
            int y,
            int z,
            int width,
            int height)
        {
            this.x = x;
            this.y = y;
            this.z = z;

            this.width = width;
            this.height = height;
        }
    }
}
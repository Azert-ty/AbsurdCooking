

using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class EnemyConeVisual : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private EnemyVision enemyVision;

    // =========================================================
    // CONE SETTINGS
    // =========================================================

    [Header("Cone Settings")]
    [SerializeField] private float viewDistance = 5f;

    [Tooltip("Même valeur que visionAngle dans EnemyVision.")]
    [SerializeField] private float visionHalfAngle = 30f;

    [SerializeField] private int segments = 30;

    // =========================================================
    // RENDER SETTINGS
    // =========================================================

    [Header("Render Settings")]
    [SerializeField] private string sortingLayerName = "Default";

    [SerializeField] private int sortingOrder = 20;

    [SerializeField] private float zOffset = -0.05f;

    // =========================================================
    // COLORS
    // =========================================================

    [Header("Colors")]
    [SerializeField]
    private Color patrolColor =
        new Color(1f, 1f, 1f, 0.35f);

    [SerializeField]
    private Color alertColor =
        new Color(1f, 0.6f, 0f, 0.45f);

    [SerializeField]
    private Color chaseColor =
        new Color(1f, 0f, 0f, 0.55f);

    [SerializeField]
    private Color searchColor =
        new Color(1f, 1f, 0f, 0.45f);

    // =========================================================
    // INTERNAL
    // =========================================================

    private Mesh mesh;

    private MeshFilter meshFilter;

    private MeshRenderer meshRenderer;

    private Material material;

    private Transform visionOrigin;

    private Transform enemyRoot;

    private LayerMask obstacleMask;

    // Réutilisée pour éviter de créer des listes à chaque frame.
    private readonly List<RaycastHit2D> raycastHits =
        new List<RaycastHit2D>(8);

    private ContactFilter2D obstacleFilter;

    // =========================================================
    // UNITY
    // =========================================================

    private void OnEnable()
    {
        Setup();

        SyncWithEnemyVision();

        GenerateCone();

        ShowPatrol();
    }

    private void LateUpdate()
    {
        SyncWithEnemyVision();

        GenerateCone();
    }

    private void OnValidate()
    {
        if (segments < 3)
        {
            segments = 3;
        }

        Setup();

        SyncWithEnemyVision();

        GenerateCone();

        ApplyRenderSettings();
    }

    // =========================================================
    // SETUP
    // =========================================================

    private void Setup()
    {
        meshFilter =
            GetComponent<MeshFilter>();

        meshRenderer =
            GetComponent<MeshRenderer>();

        if (mesh == null)
        {
            mesh = new Mesh();

            mesh.name =
                "Enemy Vision Cone";
        }

        if (meshFilter.sharedMesh != mesh)
        {
            meshFilter.sharedMesh = mesh;
        }

        if (material == null)
        {
            Shader shader =
                Shader.Find("Sprites/Default");

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Universal Render Pipeline/Unlit"
                    );
            }

            if (shader != null)
            {
                material =
                    new Material(shader);

                material.name =
                    "Enemy Cone Material";
            }
        }

        if (material != null &&
            meshRenderer.sharedMaterial != material)
        {
            meshRenderer.sharedMaterial =
                material;
        }

        ApplyRenderSettings();
    }

    // =========================================================
    // SYNCHRONIZATION
    // =========================================================

    private void SyncWithEnemyVision()
    {
        if (enemyVision == null)
        {
            enemyVision =
                GetComponentInParent<EnemyVision>();
        }

        if (enemyVision == null)
        {
            return;
        }

        // -----------------------------------------------------
        // Même portée que la vraie vision.
        // -----------------------------------------------------

        viewDistance =
            enemyVision.GetDetectionRange();

        // -----------------------------------------------------
        // Même angle que la vraie vision.
        // -----------------------------------------------------

        visionHalfAngle =
            enemyVision.GetVisionHalfAngle();

        // -----------------------------------------------------
        // Même origine que la vraie vision.
        // -----------------------------------------------------

        visionOrigin =
            enemyVision.GetVisionOrigin();

        // -----------------------------------------------------
        // Même masque d'obstacles que la vraie vision.
        // -----------------------------------------------------

        obstacleMask =
            enemyVision.GetObstacleMask();

        // -----------------------------------------------------
        // Racine de l'ennemi.
        //
        // Sert à ignorer ses propres colliders.
        // -----------------------------------------------------

        enemyRoot =
            enemyVision.transform;

        // -----------------------------------------------------
        // Configure le filtre.
        // -----------------------------------------------------

        obstacleFilter =
            new ContactFilter2D();

        obstacleFilter.useLayerMask = true;

        obstacleFilter.SetLayerMask(
            obstacleMask
        );

        // Les triggers ne doivent pas couper le cône.
        obstacleFilter.useTriggers = false;
    }

    // =========================================================
    // RENDER SETTINGS
    // =========================================================

    private void ApplyRenderSettings()
    {
        if (meshRenderer == null)
        {
            return;
        }

        meshRenderer.sortingLayerName =
            sortingLayerName;

        meshRenderer.sortingOrder =
            sortingOrder;
    }

    // =========================================================
    // CONE GENERATION
    // =========================================================

    private void GenerateCone()
    {
        if (mesh == null)
        {
            return;
        }

        if (enemyVision == null)
        {
            return;
        }

        if (visionOrigin == null)
        {
            return;
        }

        Vector3[] vertices =
            new Vector3[segments + 2];

        int[] triangles =
            new int[segments * 3];

        // -----------------------------------------------------
        // VRAIE ORIGINE DU CÔNE
        //
        // On convertit la position monde de visionOrigin
        // vers l'espace local du mesh.
        // -----------------------------------------------------

        Vector3 worldOrigin =
            visionOrigin.position;

        Vector3 localOrigin =
            transform.InverseTransformPoint(
                worldOrigin
            );

        localOrigin.z = zOffset;

        vertices[0] =
            localOrigin;

        // -----------------------------------------------------
        // ANGLES
        // -----------------------------------------------------

        float startAngle =
            -visionHalfAngle;

        float totalAngle =
            visionHalfAngle * 2f;

        float angleStep =
            totalAngle / segments;

        // -----------------------------------------------------
        // GÉNÉRATION DES RAYONS
        // -----------------------------------------------------

        for (int i = 0; i <= segments; i++)
        {
            float angle =
                startAngle +
                angleStep * i;

            float radians =
                angle * Mathf.Deg2Rad;

            Vector3 localDirection =
                new Vector3(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians),
                    0f
                );

            // -------------------------------------------------
            // IMPORTANT :
            //
            // La direction monde vient du vrai visionOrigin.
            //
            // Donc EnemyVision et EnemyConeVisual regardent
            // exactement dans la même direction.
            // -------------------------------------------------

            Vector3 worldDirection =
                visionOrigin
                    .TransformDirection(
                        localDirection
                    )
                    .normalized;

            float distance =
                GetVisibleDistance(
                    worldOrigin,
                    worldDirection
                );

            Vector3 worldEndPoint =
                worldOrigin +
                worldDirection * distance;

            Vector3 localEndPoint =
                transform.InverseTransformPoint(
                    worldEndPoint
                );

            localEndPoint.z =
                zOffset;

            vertices[i + 1] =
                localEndPoint;
        }

        // -----------------------------------------------------
        // TRIANGLES
        // -----------------------------------------------------

        for (int i = 0;
             i < segments;
             i++)
        {
            int triangleIndex =
                i * 3;

            triangles[triangleIndex] =
                0;

            triangles[triangleIndex + 1] =
                i + 1;

            triangles[triangleIndex + 2] =
                i + 2;
        }

        // -----------------------------------------------------
        // APPLY
        // -----------------------------------------------------

        mesh.Clear();

        mesh.vertices =
            vertices;

        mesh.triangles =
            triangles;

        mesh.RecalculateBounds();
    }

    // =========================================================
    // OBSTACLE DISTANCE
    // =========================================================

    private float GetVisibleDistance(
        Vector2 origin,
        Vector2 direction)
    {
        raycastHits.Clear();

        Physics2D.Raycast(
            origin,
            direction,
            obstacleFilter,
            raycastHits,
            viewDistance
        );

        // Les résultats arrivent du plus proche au plus loin.
        // On cherche le premier vrai obstacle
        // qui n'appartient pas à l'ennemi.
        for (int i = 0;
             i < raycastHits.Count;
             i++)
        {
            RaycastHit2D hit =
                raycastHits[i];

            if (hit.collider == null)
            {
                continue;
            }

            // -------------------------------------------------
            // Ignore tous les colliders de l'ennemi.
            // -------------------------------------------------

            if (enemyRoot != null &&
                hit.collider.transform.IsChildOf(enemyRoot))
            {
                continue;
            }

            return hit.distance;
        }

        // Aucun obstacle :
        // le cône va jusqu'à la portée maximale.
        return viewDistance;
    }

    // =========================================================
    // COLORS
    // =========================================================

    public void ShowPatrol()
    {
        SetColor(
            patrolColor
        );
    }

    public void ShowAlert()
    {
        SetColor(
            alertColor
        );
    }

    public void ShowChase()
    {
        SetColor(
            chaseColor
        );
    }

    public void ShowSearch()
    {
        SetColor(
            searchColor
        );
    }

    private void SetColor(Color color)
    {
        Setup();

        if (material == null)
        {
            return;
        }

        material.color =
            color;
    }
}
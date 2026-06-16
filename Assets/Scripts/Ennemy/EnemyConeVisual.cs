using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class EnemyConeVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyVision enemyVision;

    [Header("Cone Settings")]
    [SerializeField] private float viewDistance = 5f;

    [Tooltip("Même valeur que visionAngle dans EnemyVision. Exemple : 30 donne un cône total de 60 degrés.")]
    [SerializeField] private float visionHalfAngle = 30f;

    [SerializeField] private int segments = 30;

    [Header("Obstacle Detection")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("Render Settings")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 20;
    [SerializeField] private float zOffset = -0.05f;

    [Header("Colors")]
    [SerializeField] private Color patrolColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color alertColor = new Color(1f, 0.6f, 0f, 0.45f);
    [SerializeField] private Color chaseColor = new Color(1f, 0f, 0f, 0.55f);
    [SerializeField] private Color searchColor = new Color(1f, 1f, 0f, 0.45f);

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material material;

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
            segments = 3;

        Setup();
        SyncWithEnemyVision();
        GenerateCone();
        ApplyRenderSettings();
    }

    private void Setup()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Enemy Vision Cone";
        }

        if (meshFilter.sharedMesh != mesh)
            meshFilter.sharedMesh = mesh;

        if (material == null)
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            material = new Material(shader);
            material.name = "Enemy Cone Material";
        }

        if (meshRenderer.sharedMaterial != material)
            meshRenderer.sharedMaterial = material;

        ApplyRenderSettings();
    }

    private void SyncWithEnemyVision()
    {
        if (enemyVision == null)
            enemyVision = GetComponentInParent<EnemyVision>();

        if (enemyVision == null)
            return;

        viewDistance = enemyVision.GetDetectionRange();
        visionHalfAngle = enemyVision.GetVisionHalfAngle();
    }

    private void ApplyRenderSettings()
    {
        if (meshRenderer == null)
            return;

        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = sortingOrder;
    }

    private void GenerateCone()
    {
        if (mesh == null)
            return;

        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = new Vector3(0f, 0f, zOffset);

        float startAngle = -visionHalfAngle;
        float totalAngle = visionHalfAngle * 2f;
        float angleStep = totalAngle / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + angleStep * i;
            float radians = angle * Mathf.Deg2Rad;

            Vector3 localDirection =
                new Vector3(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians),
                    0f);

            Vector3 worldDirection =
                transform.TransformDirection(localDirection).normalized;

            float distance = viewDistance;

            RaycastHit2D hit =
                Physics2D.Raycast(
                    transform.position,
                    worldDirection,
                    viewDistance,
                    obstacleMask);

            if (hit.collider != null)
            {
                distance = hit.distance;
            }

            vertices[i + 1] =
                localDirection * distance +
                new Vector3(0f, 0f, zOffset);
        }

        for (int i = 0; i < segments; i++)
        {
            int triangleIndex = i * 3;

            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i + 2;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    public void ShowPatrol()
    {
        SetColor(patrolColor);
    }

    public void ShowAlert()
    {
        SetColor(alertColor);
    }

    public void ShowChase()
    {
        SetColor(chaseColor);
    }

    public void ShowSearch()
    {
        SetColor(searchColor);
    }

    private void SetColor(Color color)
    {
        Setup();

        if (material == null)
            return;

        material.color = color;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class IntroDoor : MonoBehaviour
{
    // =========================================================
    // DOOR VISUAL
    // =========================================================

    [Header("Door Visual")]

    [Tooltip("SpriteRenderer qui affiche la porte.")]
    [SerializeField]
    private SpriteRenderer doorRenderer;

    // =========================================================
    // OPENING FRAMES
    // =========================================================

    [Header("Opening Frames")]

    [Tooltip(
        "Sprites dans l'ordre : " +
        "fermée -> complètement ouverte."
    )]
    [SerializeField]
    private Sprite[] openingFrames;

    // =========================================================
    // ANIMATION
    // =========================================================

    [Header("Animation")]

    [Tooltip("Temps entre deux sprites.")]
    [SerializeField]
    private float frameDuration = 0.08f;

    // =========================================================
    // COLLIDER
    // =========================================================

    [Header("Automatic Door Collider")]

    [Tooltip(
        "PolygonCollider2D qui prend automatiquement " +
        "la forme du sprite actuellement affiché."
    )]
    [SerializeField]
    private PolygonCollider2D doorCollider;

    // =========================================================
    // SOUND
    // =========================================================

    [Header("Sound")]

    [SerializeField]
    private AudioClip openSound;

    [SerializeField]
    private AudioSource audioSource;

    // =========================================================
    // STATE
    // =========================================================

    private Coroutine openingCoroutine;

    private int currentFrame;

    private bool isOpen;

    private bool isOpening;

    // Liste réutilisée pour récupérer
    // les points physiques du sprite.
    private readonly List<Vector2> physicsShapePoints =
        new List<Vector2>(64);

    // =========================================================
    // PUBLIC
    // =========================================================

    public bool IsOpen => isOpen;

    public bool IsOpening => isOpening;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        AutoFindReferences();

        currentFrame = 0;

        isOpen = false;

        isOpening = false;

        // -----------------------------------------------------
        // PORTE FERMÉE
        // -----------------------------------------------------

        ApplyFrame(0);
    }

    // =========================================================
    // AUTO REFERENCES
    // =========================================================

    private void AutoFindReferences()
    {
        // -----------------------------------------------------
        // SPRITE RENDERER
        // -----------------------------------------------------

        if (doorRenderer == null)
        {
            doorRenderer =
                GetComponentInChildren<SpriteRenderer>(true);
        }

        // -----------------------------------------------------
        // POLYGON COLLIDER
        // -----------------------------------------------------

        if (doorCollider == null &&
            doorRenderer != null)
        {
            doorCollider =
                doorRenderer.GetComponent<PolygonCollider2D>();
        }

        // -----------------------------------------------------
        // AUDIO SOURCE
        // -----------------------------------------------------

        if (audioSource == null)
        {
            audioSource =
                GetComponent<AudioSource>();
        }
    }

    // =========================================================
    // OPEN DOOR
    // =========================================================

    public void OpenDoor()
    {
        if (isOpen)
            return;

        if (isOpening)
            return;

        if (openingFrames == null ||
            openingFrames.Length == 0)
        {
            Debug.LogWarning(
                "IntroDoor : aucune frame d'ouverture assignée.",
                this
            );

            return;
        }

        PlayOpenSound();

        openingCoroutine =
            StartCoroutine(
                OpenDoorRoutine()
            );
    }

    // =========================================================
    // OPENING ROUTINE
    // =========================================================

    private IEnumerator OpenDoorRoutine()
    {
        isOpening = true;

        for (
            int i = currentFrame + 1;
            i < openingFrames.Length;
            i++)
        {
            currentFrame = i;

            // -------------------------------------------------
            // CHANGE LE SPRITE
            //
            // ApplyFrame met aussi automatiquement
            // à jour le collider.
            // -------------------------------------------------

            ApplyFrame(currentFrame);

            yield return new WaitForSeconds(
                frameDuration
            );
        }

        // -----------------------------------------------------
        // FIN
        // -----------------------------------------------------

        currentFrame =
            openingFrames.Length - 1;

        ApplyFrame(currentFrame);

        isOpen = true;

        isOpening = false;

        openingCoroutine = null;
    }

    // =========================================================
    // APPLY FRAME
    // =========================================================

    private void ApplyFrame(int frameIndex)
    {
        if (doorRenderer == null)
            return;

        if (openingFrames == null ||
            openingFrames.Length == 0)
        {
            return;
        }

        int safeIndex =
            Mathf.Clamp(
                frameIndex,
                0,
                openingFrames.Length - 1
            );

        Sprite frame =
            openingFrames[safeIndex];

        if (frame == null)
        {
            Debug.LogWarning(
                "IntroDoor : la frame " +
                safeIndex +
                " est vide.",
                this
            );

            return;
        }

        // -----------------------------------------------------
        // 1. CHANGE LE VISUEL
        // -----------------------------------------------------

        doorRenderer.sprite = frame;

        // -----------------------------------------------------
        // 2. LE COLLIDER PREND IMMÉDIATEMENT
        //    LA FORME DU NOUVEAU SPRITE
        // -----------------------------------------------------

        UpdateColliderFromCurrentSprite();
    }

    // =========================================================
    // AUTOMATIC COLLIDER
    // =========================================================

    private void UpdateColliderFromCurrentSprite()
    {
        if (doorRenderer == null)
            return;

        if (doorCollider == null)
            return;

        Sprite currentSprite =
            doorRenderer.sprite;

        if (currentSprite == null)
            return;

        // -----------------------------------------------------
        // NOMBRE DE FORMES PHYSIQUES
        // CONTENUES DANS LE SPRITE
        // -----------------------------------------------------

        int shapeCount =
            currentSprite.GetPhysicsShapeCount();

        if (shapeCount <= 0)
        {
            Debug.LogWarning(
                "IntroDoor : le sprite " +
                currentSprite.name +
                " n'a aucune Physics Shape. " +
                "Active Generate Physics Shape dans l'import du sprite.",
                this
            );

            return;
        }

        // -----------------------------------------------------
        // PRÉPARE LE POLYGON COLLIDER
        // -----------------------------------------------------

        doorCollider.pathCount =
            shapeCount;

        // -----------------------------------------------------
        // COPIE CHAQUE FORME DU SPRITE
        // VERS LE POLYGON COLLIDER
        // -----------------------------------------------------

        for (
            int shapeIndex = 0;
            shapeIndex < shapeCount;
            shapeIndex++)
        {
            physicsShapePoints.Clear();

            currentSprite.GetPhysicsShape(
                shapeIndex,
                physicsShapePoints
            );

            // -------------------------------------------------
            // CAS SIMPLE ET IDÉAL :
            //
            // SpriteRenderer et PolygonCollider2D
            // sont sur le même GameObject.
            // -------------------------------------------------

            if (
                doorCollider.transform ==
                doorRenderer.transform
            )
            {
                doorCollider.SetPath(
                    shapeIndex,
                    physicsShapePoints.ToArray()
                );

                continue;
            }

            // -------------------------------------------------
            // SÉCURITÉ :
            //
            // Si le collider est sur un autre GameObject,
            // on convertit correctement les coordonnées.
            // -------------------------------------------------

            for (
                int pointIndex = 0;
                pointIndex < physicsShapePoints.Count;
                pointIndex++)
            {
                Vector2 spritePoint =
                    physicsShapePoints[pointIndex];

                Vector3 worldPoint =
                    doorRenderer.transform.TransformPoint(
                        spritePoint
                    );

                Vector3 colliderLocalPoint =
                    doorCollider.transform.InverseTransformPoint(
                        worldPoint
                    );

                physicsShapePoints[pointIndex] =
                    colliderLocalPoint;
            }

            doorCollider.SetPath(
                shapeIndex,
                physicsShapePoints.ToArray()
            );
        }
    }

    // =========================================================
    // SOUND
    // =========================================================

    private void PlayOpenSound()
    {
        if (openSound == null)
            return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(
                openSound
            );

            return;
        }

        AudioSource.PlayClipAtPoint(
            openSound,
            transform.position
        );
    }
}
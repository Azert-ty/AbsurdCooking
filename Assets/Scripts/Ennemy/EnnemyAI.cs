using System.Collections;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Capture")]
    [SerializeField] private float captureDistance = 0.5f;
    [SerializeField] private GameObject enemyVisualObject;

    private bool hasCapturedPlayer;

    [Header("References")]
    [SerializeField] private EnemyVision vision;
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private EnemyFeedback feedback;
    [SerializeField] private EnemyConeVisual coneVisual;

    [Header("Alert")]
    [SerializeField] private float alertDuration = 0.4f;

    [Header("Chase")]
    [SerializeField] private float loseSightDelay = 2f;

    [Header("Search")]
    [SerializeField] private float searchPredictionDistance = 2f;
    [SerializeField] private float searchLookDuration = 0.35f;
    [SerializeField] private float searchMoveTimeout = 4f;

    private EnemyState currentState;

    private float lostSightTimer;

    private Vector3 lastKnownPlayerPosition;
    private Vector2 lastKnownPlayerDirection = Vector2.down;

    private bool hasEnteredInitialState;
    private bool aiPaused;

    private void Awake()
    {
        if (vision == null)
            vision = GetComponent<EnemyVision>();

        if (movement == null)
            movement = GetComponent<EnemyMovement>();

        if (feedback == null)
            feedback = GetComponent<EnemyFeedback>();

        if (coneVisual == null)
            coneVisual = GetComponentInChildren<EnemyConeVisual>();

        if (enemyVisualObject == null)
        {
            SpriteRenderer spriteRenderer =
                GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
                enemyVisualObject = spriteRenderer.gameObject;
        }
    }

    private void Start()
    {
        ChangeState(EnemyState.Patrol);
    }

    private void Update()
    {
        if (aiPaused)
            return;

        if (hasCapturedPlayer)
            return;

        if (GameManager.Instance != null &&
            GameManager.Instance.GameEnded)
            return;

        if (currentState != EnemyState.Patrol)
            return;

        if (vision != null && vision.CanSeePlayer())
        {
            RegisterPlayerSeen();
            SaveLastKnownPlayerData();

            ChangeState(EnemyState.Alert);
        }
    }

    private void ChangeState(EnemyState newState, bool forceRestart = false)
    {
        if (!forceRestart &&
            hasEnteredInitialState &&
            currentState == newState)
        {
            return;
        }

        if (currentState == EnemyState.Chase &&
            newState != EnemyState.Chase)
        {
            if (movement != null)
                EnemyChaseCoordinator.Unregister(movement);
        }

        StopAllCoroutines();

        currentState = newState;
        hasEnteredInitialState = true;

        switch (currentState)
        {
            case EnemyState.Patrol:
                EnterPatrol();
                break;

            case EnemyState.Alert:
                EnterAlert();
                break;

            case EnemyState.Chase:
                EnterChase();
                break;

            case EnemyState.Search:
                EnterSearch();
                break;
        }
    }

    private void EnterPatrol()
    {
        if (feedback != null)
            feedback.ShowPatrol();

        if (coneVisual != null)
            coneVisual.ShowPatrol();

        if (movement != null)
        {
            movement.ResetFatigue();
            StartCoroutine(movement.PatrolRoutine());
        }
    }

    private void EnterAlert()
    {
        if (feedback != null)
            feedback.ShowAlert();

        if (coneVisual != null)
            coneVisual.ShowAlert();

        StartCoroutine(AlertRoutine());
    }

    private void EnterChase()
    {
        if (feedback != null)
            feedback.ShowChase();

        if (coneVisual != null)
            coneVisual.ShowChase();

        if (movement != null &&
            vision != null &&
            vision.Player != null)
        {
            EnemyChaseCoordinator.Register(
                movement,
                vision.Player
            );
        }

        StartCoroutine(ChaseRoutine());
    }

    private void EnterSearch()
    {
        if (feedback != null)
            feedback.ShowSearch();

        if (coneVisual != null)
            coneVisual.ShowSearch();

        if (movement != null)
            movement.ResetFatigue();

        StartCoroutine(SearchRoutine());
    }

    // private IEnumerator AlertRoutine()
    // {
    //     if (movement != null)
    //     {
    //         movement.StopMovement();

    //         if (vision != null && vision.Player != null)
    //             movement.FacePosition(vision.Player.position);
    //         else
    //             movement.FacePosition(lastKnownPlayerPosition);
    //     }

    //     yield return new WaitForSeconds(alertDuration);

    //     if (aiPaused)
    //         yield break;

    //     if (TryCapturePlayer())
    //         yield break;

    //     ChangeState(EnemyState.Chase);
    // }

    private IEnumerator AlertRoutine()
{
    if (movement != null)
    {
        movement.StopMovement();

        if (vision != null &&
    vision.Player != null)
{
    movement.FaceTarget(
        vision.Player
    );
}
        else
        {
            movement.FacePosition(
                lastKnownPlayerPosition
            );
        }
    }

    yield return new WaitForSeconds(
        alertDuration
    );

    if (aiPaused)
        yield break;

    ChangeState(EnemyState.Chase);
}

    private IEnumerator ChaseRoutine()
    {
        if (movement == null || vision == null || vision.Player == null)
        {
            ChangeState(EnemyState.Patrol);
            yield break;
        }

        movement.StartChaseFatigue();

        lostSightTimer = 0f;

        while (currentState == EnemyState.Chase)
        {
            if (aiPaused)
                yield break;

            if (TryCapturePlayer())
                yield break;

            movement.UpdateChaseFatigue();

            bool canStillSeePlayer =
                vision.HasLineOfSightToPlayer();

            if (canStillSeePlayer)
            {
                RegisterPlayerSeen();
                SaveLastKnownPlayerData();

                lostSightTimer = 0f;

                EnemyChaseCoordinator.RecalculateRanks();

                movement.ChaseTarget(vision.Player);
            }
            else
            {
                lostSightTimer += Time.deltaTime;

                movement.MoveToAndFace(lastKnownPlayerPosition);

                if (lostSightTimer >= loseSightDelay)
                {
                    ChangeState(EnemyState.Search);
                    yield break;
                }
            }

            yield return null;
        }
    }

    private IEnumerator SearchRoutine()
    {
        if (movement == null || vision == null)
        {
            ChangeState(EnemyState.Patrol);
            yield break;
        }

        Vector2 searchDirection = lastKnownPlayerDirection;

        if (searchDirection.sqrMagnitude < 0.01f)
            searchDirection = Vector2.down;

        Vector3 predictedPosition =
            lastKnownPlayerPosition +
            (Vector3)(searchDirection.normalized * searchPredictionDistance);

        movement.SetPatrolSpeed();
        movement.MoveToAndFace(predictedPosition);

        float timer = 0f;

        while (!movement.ReachedDestination() &&
               timer < searchMoveTimeout)
        {
            if (aiPaused)
                yield break;

            if (TryCapturePlayer())
                yield break;

            if (vision.CanSeePlayer())
            {
                RegisterPlayerSeen();
                SaveLastKnownPlayerData();

                ChangeState(EnemyState.Chase);
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        movement.StopMovement();

        yield return LookAroundRoutine();

        if (!aiPaused)
            ChangeState(EnemyState.Patrol);
    }

    private IEnumerator LookAroundRoutine()
    {
        Vector2 baseDirection = lastKnownPlayerDirection;

        if (baseDirection.sqrMagnitude < 0.01f)
            baseDirection = Vector2.down;

        Vector2 leftDirection =
            new Vector2(-baseDirection.y, baseDirection.x);

        Vector2 rightDirection =
            new Vector2(baseDirection.y, -baseDirection.x);

        yield return LookDirectionForSeconds(
            baseDirection,
            searchLookDuration
        );

        yield return LookDirectionForSeconds(
            leftDirection,
            searchLookDuration
        );

        yield return LookDirectionForSeconds(
            rightDirection,
            searchLookDuration
        );

        yield return LookDirectionForSeconds(
            baseDirection,
            searchLookDuration
        );
    }

    private IEnumerator LookDirectionForSeconds(
        Vector2 direction,
        float duration)
    {
        float timer = 0f;

        if (movement != null)
            movement.FaceDirection(direction);

        while (timer < duration)
        {
            if (aiPaused)
                yield break;

            if (TryCapturePlayer())
                yield break;

            if (vision != null && vision.CanSeePlayer())
            {
                RegisterPlayerSeen();
                SaveLastKnownPlayerData();

                ChangeState(EnemyState.Chase);
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }
    }

    private bool TryCapturePlayer()
    {
        if (hasCapturedPlayer)
            return true;

        // Une capture n'est possible qu'en Chase.
    if (currentState != EnemyState.Chase)
        return false;

        if (vision == null || vision.Player == null)
            return false;

        if (GameManager.Instance != null &&
            GameManager.Instance.GameEnded)
            return true;

        float sqrDistance =
            ((Vector2)transform.position -
             (Vector2)vision.Player.position).sqrMagnitude;

        if (sqrDistance > captureDistance * captureDistance)
            return false;

        CapturePlayer(vision.Player);

        return true;
    }

    private void CapturePlayer(Transform playerTransform)
    {
        if (hasCapturedPlayer)
            return;

        hasCapturedPlayer = true;

        if (movement != null)
            movement.StopMovement();

        if (currentState == EnemyState.Chase &&
            movement != null)
        {
            EnemyChaseCoordinator.Unregister(movement);
        }

        if (GameManager.Instance == null)
            return;

        Vector3 capturePosition =
            (transform.position + playerTransform.position) * 0.5f;

        GameManager.Instance.GameOver(
            capturePosition,
            enemyVisualObject
        );
    }

    private void SaveLastKnownPlayerData()
    {
        if (vision == null)
            return;

        lastKnownPlayerPosition = vision.LastKnownPlayerPosition;
        lastKnownPlayerDirection = vision.LastKnownDirection;

        if (lastKnownPlayerDirection.sqrMagnitude < 0.01f &&
            vision.Player != null)
        {
            lastKnownPlayerDirection =
                ((Vector2)vision.Player.position -
                 (Vector2)transform.position).normalized;
        }

        if (lastKnownPlayerDirection.sqrMagnitude < 0.01f)
            lastKnownPlayerDirection = Vector2.down;
    }
    private void SavePlayerContactData(Transform playerTransform)
{
    if (playerTransform == null)
        return;

    lastKnownPlayerPosition =
        playerTransform.position;

    Vector2 direction =
        (Vector2)playerTransform.position -
        (Vector2)transform.position;

    if (direction.sqrMagnitude > 0.001f)
    {
        lastKnownPlayerDirection =
            direction.normalized;
    }
}

    private void RegisterPlayerSeen()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterPlayerSeen();
    }

    // private void OnTriggerEnter2D(Collider2D other)
    // {
    //     if (!other.CompareTag("Player"))
    //         return;

    //     if (hasCapturedPlayer)
    //         return;

    //     CapturePlayer(other.transform);
    // }


    private void OnTriggerEnter2D(Collider2D other)
{
    if (hasCapturedPlayer)
        return;

    if (aiPaused)
        return;

    if (vision == null || vision.Player == null)
        return;

    // Vérifie que le collider appartient bien au joueur.
    bool isPlayer =
        other.transform == vision.Player ||
        other.transform.IsChildOf(vision.Player);

    if (!isPlayer)
        return;

    // =====================================================
    // CAS 1 :
    // L'ennemi poursuivait déjà le joueur.
    //
    // Contact = capture immédiate.
    // =====================================================

    if (currentState == EnemyState.Chase)
    {
        CapturePlayer(vision.Player);
        return;
    }

    // =====================================================
    // CAS 2 :
    // L'ennemi ne poursuivait pas encore le joueur.
    //
    // Le contact déclenche une alerte.
    // =====================================================

    RegisterPlayerSeen();

    SavePlayerContactData(vision.Player);

    ChangeState(EnemyState.Alert);
}

    public void PauseAI()
    {
        aiPaused = true;

        StopAllCoroutines();

        if (currentState == EnemyState.Chase &&
            movement != null)
        {
            EnemyChaseCoordinator.Unregister(movement);
        }

        if (movement != null)
            movement.StopMovement();
    }

    public void ResumeAI()
    {
        aiPaused = false;

        if (GameManager.Instance != null &&
            GameManager.Instance.GameEnded)
            return;

        ChangeState(EnemyState.Patrol, true);
    }
}
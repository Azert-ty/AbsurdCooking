using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemyAI : MonoBehaviour
{

    
    [Header("References")]
    [SerializeField] private EnemyVision vision;

    [SerializeField] private EnemyMovement movement;

     [SerializeField] 
    private EnemyFeedback feedback;
    
    [SerializeField] 
    private EnemyConeVisual coneVisual;

    [Header("Alert")]
    [SerializeField] private float alertDuration = 0.4f;

    private EnemyState currentState;

    [SerializeField]
    private float searchAngle = 45f;

    [SerializeField]
    private float searchRotationSpeed = 180f;

    [SerializeField]
    private float loseSightDelay = 2f;

   
    private float lostSightTimer;


    private bool aiPaused;


    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (coneVisual == null)
            coneVisual = GetComponentInChildren<EnemyConeVisual>();
        spriteRenderer =
            GetComponent<SpriteRenderer>();

        if (feedback == null)
        feedback = GetComponent<EnemyFeedback>();
        if (coneVisual == null)
        coneVisual = GetComponentInChildren<EnemyConeVisual>();
    }

    private void Start()
    {
        ChangeState(EnemyState.Patrol);
    }

    private void Update()
    {
        if (aiPaused)
            return;
        if (currentState == EnemyState.Patrol)
        {
            if (vision.CanSeePlayer())
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.RegisterPlayerSeen();

                ChangeState(EnemyState.Alert);
            }
        }
    }



    private void ChangeState(EnemyState newState)
    {
        if (currentState == EnemyState.Chase &&
            newState != EnemyState.Chase)
        {
            EnemyChaseCoordinator.Unregister(movement);
        }

        StopAllCoroutines();

        Debug.Log($"{currentState} -> {newState}");

        currentState = newState;

        switch (currentState)
        {
            case EnemyState.Patrol:
                
                if (feedback != null)
                    feedback.ShowPatrol();

                if (coneVisual != null)
                    coneVisual.ShowPatrol();
                movement.ResetFatigue();
                StartCoroutine(
                    movement.PatrolRoutine());

                break;

            case EnemyState.Alert:

                if (feedback != null)
                    feedback.ShowAlert();
                if (coneVisual != null)
                    coneVisual.ShowAlert();

                StartCoroutine(
                    AlertRoutine());

                break;

            case EnemyState.Chase:

                if (feedback != null)
                    feedback.ShowChase();
                if (coneVisual != null)
                    coneVisual.ShowChase();

                EnemyChaseCoordinator.Register(
                    movement,
                    vision.Player);

                StartCoroutine(
                    ChaseRoutine());

                break;

            case EnemyState.Search:

                if (feedback != null)
                    feedback.ShowSearch();

                if (coneVisual != null)
                    coneVisual.ShowSearch();

                movement.ResetFatigue();

                StartCoroutine(
                    SearchRoutine());

                break;
        }
    }
    private IEnumerator AlertRoutine()
    {
        movement.StopMovement();


        yield return new WaitForSeconds(alertDuration);

        ChangeState(EnemyState.Chase);
    }

    private IEnumerator RotateToAngle(float targetZ)
    {
        Quaternion targetRotation =
            Quaternion.Euler(0, 0, targetZ);

        while (
            Quaternion.Angle(
                transform.rotation,
                targetRotation) > 1f)
        {
            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    searchRotationSpeed * Time.deltaTime);
                    if (vision.CanSeePlayer())
                    {
                        ChangeState(EnemyState.Chase);
                        yield break;
                    }

            yield return null;
        }
    }

   
    private IEnumerator ChaseRoutine()
    {
        movement.StartChaseFatigue();

        lostSightTimer = 0f;

        while (currentState == EnemyState.Chase)
        {
            movement.UpdateChaseFatigue();

            if (vision.CanSeePlayer())
            {
                Debug.Log("VOIT JOUEUR");

                lostSightTimer = 0f;

                EnemyChaseCoordinator.RecalculateRanks();

                movement.ChaseTarget(vision.Player);
            }
            else
            {
                Debug.Log("PERDU");

                lostSightTimer += Time.deltaTime;

                if (lostSightTimer >= loseSightDelay)
                {
                    ChangeState(EnemyState.Search);
                }
            }

            yield return null;
        }
    }

    

    private IEnumerator SearchRoutine()
    {

        Vector3 predictedPosition =
        vision.LastKnownPlayerPosition +
        (Vector3)(vision.LastKnownDirection * 2f);

        movement.MoveTo(predictedPosition);

        while (!movement.ReachedDestination())
        {
             if (vision.CanSeePlayer())
            {
                ChangeState(EnemyState.Chase);
                yield break;
            }

            yield return null;
        }

        float baseAngle =
            transform.eulerAngles.z;

        yield return RotateToAngle(
            baseAngle + searchAngle);

        yield return new WaitForSeconds(0.3f);

        yield return RotateToAngle(
            baseAngle - searchAngle);

        yield return new WaitForSeconds(0.3f);

        yield return RotateToAngle(
            baseAngle);

        yield return new WaitForSeconds(0.5f);

        ChangeState(EnemyState.Patrol);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            GameManager.Instance.GameOver();
        }
    }


    public void PauseAI()
    {
        aiPaused = true;

        StopAllCoroutines();

        if (currentState == EnemyState.Chase)
        {
            EnemyChaseCoordinator.Unregister(movement);
        }

        if (movement != null)
            movement.StopMovement();
    }

    public void ResumeAI()
    {
        aiPaused = false;

        ChangeState(EnemyState.Patrol);
    }
}
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyVision vision;

    [SerializeField] private EnemyMovement movement;

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

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer =
            GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        ChangeState(EnemyState.Patrol);
    }

    private void Update()
    {
        if (currentState == EnemyState.Patrol)
        {
            if (vision.CanSeePlayer())
            {
                ChangeState(EnemyState.Alert);
            }
            
        }
    }

    private void ChangeState(EnemyState newState)
    {
        StopAllCoroutines();
        Debug.Log(
        $"{currentState} -> {newState}");
        currentState = newState;

        switch (currentState)
        {
            case EnemyState.Patrol:

                spriteRenderer.color = Color.white;

                StartCoroutine(
                    movement.PatrolRoutine());

                break;

            case EnemyState.Alert:

                StartCoroutine(
                    AlertRoutine());

                break;

            case EnemyState.Chase:
                StartCoroutine(ChaseRoutine());
                break;

            case EnemyState.Search:
                StartCoroutine(SearchRoutine());
                break;
        }
            

            
    }

    private IEnumerator AlertRoutine()
    {
        movement.StopMovement();

        spriteRenderer.color = Color.red;

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
        spriteRenderer.color = Color.red;

        lostSightTimer = 0f;

        while (currentState == EnemyState.Chase)
        {
                if (vision.CanSeePlayer())
                {
                    Debug.Log("VOIT JOUEUR");

                    lostSightTimer = 0f;

                    movement.MoveTo(
                        vision.Player.position);
                }
                else
                {
                    Debug.Log("PERDU");

                    lostSightTimer += Time.deltaTime;

                    // Debug.Log(lostSightTimer);

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
        spriteRenderer.color = Color.yellow;

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
}
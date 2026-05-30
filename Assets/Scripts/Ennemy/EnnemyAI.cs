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
            // if (vision.CanSeePlayerIgnoringAngle())
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
            }
    }

    private IEnumerator AlertRoutine()
    {
        movement.StopMovement();

        spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(alertDuration);

        ChangeState(EnemyState.Chase);
    }

    private IEnumerator ChaseRoutine()
    {
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
                        ChangeState(EnemyState.Patrol);
                    }
                }

            yield return null;
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            GameManager.Instance.GameOver();
        }
    }
}
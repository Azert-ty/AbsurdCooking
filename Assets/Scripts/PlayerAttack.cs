using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{

    public PlayerMovement playerMovement;
    public GameObject attackPoint;

    public LayerMask layerMask;

    private bool canAttack=true;


    private Vector2 attackorientation;
    private float attackduration;
    [SerializeField]
    private float attackRange=0.4f;
    
    void Awake()
    {
        
        
    }

    private void AttackInput(InputAction.CallbackContext context)
    {
        if (playerMovement._currentState != PlayerMovement.PlayerState.Dashing && canAttack==true)
        {
            StartCoroutine(Attack());
        }
    }

    IEnumerator Attack()
    {
        canAttack=false;
        Collider2D [] collider2Ds=Physics2D.OverlapCircleAll(attackPoint.transform.position,attackRange,layerMask);
        foreach (var collider2D in collider2Ds)
        {
            Debug.Log("BOnjour");
        }
        yield return new WaitForSeconds(attackduration);
        canAttack=true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color=Color.red;
        Gizmos.DrawWireSphere(attackPoint.transform.position,attackRange);
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (playerMovement!=null && playerMovement.controls!=null)
        {
            Debug.Log("bien0");
            playerMovement.controls.Player.Attack.performed+=AttackInput;
        }
        else
        {
            Debug.Log("nul");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(playerMovement.moveDirection != Vector2.zero)
        {
            attackPoint.transform.localPosition=playerMovement.moveDirection.normalized;
        }
    }

    void OnEnable()
    {
        
    }

    void OnDisable()
    {
        playerMovement.controls.Player.Attack.performed-=AttackInput;
    }
}

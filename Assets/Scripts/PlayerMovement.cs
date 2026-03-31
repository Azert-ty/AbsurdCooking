using System;
using System.Collections;
using System.Diagnostics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public  PlayerControls controls;

    public Vector2 moveDirection;
    private Vector2 dashDirection;

    private bool candash=true;

    private Rigidbody2D rb2D;

    public enum PlayerState {Idle,Move,Dashing}

    public PlayerState _currentState;

    [SerializeField]
    private float moveSpeed=5;

    [SerializeField]
    private float  _dashForce=20f;
    [SerializeField]
    private float _dashDuration=0.5f;
    [SerializeField]
    private float _dashCooldown=0.9f;


    void Awake()
    {
         controls=new PlayerControls();
         
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb2D=gameObject.GetComponent<Rigidbody2D>();
       

        // controls.Player.Move.performed+=onMove();
    }

    // Update is called once per frame
    void Update()
    {
        moveDirection=controls.Player.Move.ReadValue<Vector2>();
        
    }

    IEnumerator  OnDash()
    {
            candash=false;
            _currentState=PlayerState.Dashing;
            if (dashDirection == Vector2.zero)
            {
                dashDirection=Vector2.left;
            }
            rb2D.linearVelocity=_dashForce*dashDirection;
            yield return new WaitForSeconds(_dashDuration);
            rb2D.linearVelocity=Vector2.zero;
            _currentState=PlayerState.Idle;
            yield return new WaitForSeconds(_dashCooldown);
            candash=true;
        
        
    }


    void onMove()
    {
        moveDirection=moveDirection.normalized;
        rb2D.linearVelocity=moveDirection*moveSpeed;
        dashDirection=moveDirection;
          
    }

    void OnEnable()
    {
        controls.Enable();
        controls.Player.Dash.performed+=HandleDashInput;
    }

    private void HandleDashInput(InputAction.CallbackContext callbackContext)
    {
        if(candash && _currentState != PlayerState.Dashing)
        {
            StartCoroutine(OnDash());
        }
    }

    void OnDisable()
    {
        controls.Player.Dash.performed-=HandleDashInput;
        controls.Disable();
    }

    void OnIdle()
    {
        rb2D.linearVelocity=Vector2.zero;
    }

    
    

     void FixedUpdate()
    {


        if (_currentState == PlayerState.Dashing)
        {
            return;
        }
        if (moveDirection!=Vector2.zero)
        {
           onMove();
        }
        else
        {
            OnIdle();
        }
    
       
    }
}

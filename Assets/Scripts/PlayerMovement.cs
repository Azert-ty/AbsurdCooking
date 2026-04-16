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

    public enum PlayerState {Idle,Move,Dashing,Hit}

    public PlayerState _currentState;

    [SerializeField]
    private float moveSpeed=5;

    [SerializeField]
    private float _hitdelay=0.2f;

    [SerializeField]
    private float  _dashForce=20f;
    [SerializeField]
    private float _dashDuration=0.5f;
    [SerializeField]
    private float _dashCooldown=0.9f;
    
    [SerializeField]
    private float _hitspeed=7f;

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

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Projectile"))
        {
           var projectile= collision.GetComponent<Projectile >();
            StartCoroutine(OnTakehit( projectile));
        }
    }

    IEnumerator  OnTakehit(Projectile projectile)
    {
        _currentState=PlayerState.Hit;
        var rb_projectile=projectile.GetComponent<Rigidbody2D>();
        var direction=(transform.position-projectile.transform.position).normalized;

        float t=0;
        float duration=_hitdelay;

        Vector2 basePosition= rb2D.position;
        Time.timeScale = 0.1f;
        yield return new WaitForSecondsRealtime(0.05f);
        Time.timeScale = 1f;
        while (t<duration)
        {
            float strength=Mathf.Sin((t/duration)*Mathf.PI);
            Vector2 offset=direction*(_hitspeed*0.05f)* strength;
            rb2D.MovePosition(basePosition+offset);
            t+=Time.deltaTime;
            yield return null;
        }
         rb2D.MovePosition(basePosition);

       

        _currentState = PlayerState.Idle;
    }


    
    

     void FixedUpdate()
    {

        if (_currentState == PlayerState.Hit)
        {

            return;
        }
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

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

    private Rigidbody2D rb2D;

    public enum PlayerState {Idle,Move}

    public PlayerState _currentState;

    [SerializeField]
    private float moveSpeed=5;

  

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

    

    void onMove()
    {
        moveDirection=moveDirection.normalized;
        Vector2 targetPosition=rb2D.position+moveDirection*moveSpeed*Time.fixedDeltaTime;
        rb2D.MovePosition(targetPosition);
          
    }

    void OnEnable()
    {
        controls.Enable();
        controls.Player.Dash.performed+=HandleDashInput;
    }

    private void HandleDashInput(InputAction.CallbackContext callbackContext)
    {
       
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
       
    }

    


    
    

     void FixedUpdate()
    {

        
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

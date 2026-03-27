using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private PlayerControls controls;

    private Vector2 moveDirection;

    [SerializeField]
    private Rigidbody2D rb2D;

    [SerializeField]
    private int moveSpeed=5;


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
    private void onMove()
    {
        
        rb2D.AddForce(moveDirection*moveSpeed);
          
    }

    void OnEnable()
    {
        controls.Enable();
    }

    void OnDisable()
    {
        controls.Disable();
    }

    

    void FixedUpdate()
    {
        onMove();
    }
}

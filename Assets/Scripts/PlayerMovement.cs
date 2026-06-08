// using UnityEngine;
// using UnityEngine.InputSystem;

// public class PlayerMovement : MonoBehaviour
// {
//     [SerializeField] private float moveSpeed = 5f;

//     private PlayerControls controls;
//     private Vector2 moveInput;
//     private Rigidbody2D rb;

//     private bool inputEnabled = true;

//     void Awake()
//     {
//         controls = new PlayerControls();
//     }

//     void Start()
//     {
//         rb = GetComponent<Rigidbody2D>();
//     }

//     void Update()
//     {
//         if (!inputEnabled)
//         {
//             moveInput = Vector2.zero;
//             return;
//         }

//         moveInput = controls.Player.Move.ReadValue<Vector2>();
//     }

//     void FixedUpdate()
//     {
//         Vector2 movement = moveInput.normalized * moveSpeed * Time.fixedDeltaTime;
//         rb.MovePosition(rb.position + movement);
//     }

//     public void SetInputEnabled(bool enabled)
//     {
//         inputEnabled = enabled;

//         if (!enabled)
//             moveInput = Vector2.zero;
//     }

//     void OnEnable() => controls.Enable();
//     void OnDisable() => controls.Disable();
// }


using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private PlayerControls controls;
    private Vector2 moveInput;
    private Rigidbody2D rb;

    private bool inputEnabled = true;

    private void Awake()
    {
        controls = new PlayerControls();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!inputEnabled)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = controls.Player.Move.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        Vector2 movement =
            moveInput.normalized * moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + movement);

        rb.linearVelocity = Vector2.zero;
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!enabled)
        {
            moveInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }
}
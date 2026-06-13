using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private PlayerControls controls;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;

    private Rigidbody2D rb;

    private bool inputEnabled = true;

    private void Awake()
    {
        controls = new PlayerControls();

        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!inputEnabled)
        {
            moveInput = Vector2.zero;
            UpdateAnimation();
            return;
        }

        moveInput = controls.Player.Move.ReadValue<Vector2>();

        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        Vector2 movement =
            moveInput.normalized * moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + movement);

        rb.linearVelocity = Vector2.zero;
    }

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        bool isMoving =
            moveInput.sqrMagnitude > 0.01f;

        Vector2 animationDirection = Vector2.zero;

        if (isMoving)
        {
            // Priorité à gauche/droite dès qu'il y a un mouvement horizontal
            if (Mathf.Abs(moveInput.x) > 0.01f)
            {
                animationDirection =
                    new Vector2(
                        Mathf.Sign(moveInput.x),
                        0f);
            }
            else
            {
                animationDirection =
                    new Vector2(
                        0f,
                        Mathf.Sign(moveInput.y));
            }

            lastMoveDirection = animationDirection;
        }

        animator.SetBool(
            "IsMoving",
            isMoving);

        animator.SetFloat(
            "MoveX",
            animationDirection.x);

        animator.SetFloat(
            "MoveY",
            animationDirection.y);

        animator.SetFloat(
            "LastX",
            lastMoveDirection.x);

        animator.SetFloat(
            "LastY",
            lastMoveDirection.y);
    }
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!enabled)
        {
            moveInput = Vector2.zero;

            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            UpdateAnimation();
        }
    }

    private void OnEnable()
    {
        if (controls != null)
            controls.Enable();
    }

    private void OnDisable()
    {
        if (controls != null)
            controls.Disable();
    }
}
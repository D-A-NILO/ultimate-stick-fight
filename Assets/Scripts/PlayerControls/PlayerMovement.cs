using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float jumpHoldForce = 20f;
    [SerializeField] private float maxJumpTime = 0.3f;

    [Header("Dashing")]
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Jump Assistance")]
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Header("Wall Mechanics")]
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(8f, 12f);
    [SerializeField] private float wallJumpControlTime = 0.2f;

    [Header("Wall Detection")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private LayerMask groundLayer;


    private Rigidbody2D rb;
    private Vector2 movement;

    private bool isTouchingWall;
    private bool isWallSliding;
    private float wallJumpCounter;
    private int wallDirection;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    private bool isGrounded;
    private bool isJumping;
    private float jumpTime;

    private bool isDashing;
    private float nextDashTime;
    private float originalGravity;
    private float facingDirection = 1f;
    private Vector2 dashDirection;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        dashAction = InputSystem.actions.FindAction("Dash");
        originalGravity = rb.gravityScale;
    }

    private void Update()
    {
        movement = moveAction.ReadValue<Vector2>();

        CheckWall();
        WallSlide();
        WallJump();

        // Jump when grounded or within coyote time
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f && !isDashing)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );

            isGrounded = false;
            isJumping = true;
            jumpTime = 0f;

            // Reset timers so the jump cannot be reused
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }

        // Stop jump when button is released
        if (jumpAction.WasReleasedThisFrame())
        {
            isJumping = false;
        }

        // Variable jump height
        if (isJumping && jumpAction.IsPressed())
        {
            if (jumpTime < maxJumpTime)
            {
                rb.linearVelocity += Vector2.up * jumpHoldForce * Time.deltaTime;
                jumpTime += Time.deltaTime;
            }
            else
            {
                isJumping = false;
            }
        }

        // Update facing direction
        if (movement.x != 0)
        {
            facingDirection = Mathf.Sign(movement.x);
        }

        // Check for dash input
        if (dashAction.WasPressedThisFrame() &&
            Time.time >= nextDashTime &&
            !isDashing)
        {
            StartCoroutine(Dash());
        }

        // Coyote Time
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Jump Buffering
        if (jumpAction.WasPressedThisFrame())
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
            return;
        }

        if (wallJumpCounter > 0)
        {
            wallJumpCounter -= Time.fixedDeltaTime;
            return;
        }

        // Prevent movement into the wall while sliding
        if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(
                0f,
                Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed)
            );

            return;
        }

        rb.linearVelocity = new Vector2(movement.x * moveSpeed, rb.linearVelocity.y);
    }

    private void CheckWall()
    {
        if (wallJumpCounter > 0)
        {
            isTouchingWall = false;
            return;
        }

        isTouchingWall = Physics2D.Raycast(wallCheck.position, Vector2.right, wallCheckDistance, groundLayer) || Physics2D.Raycast(wallCheck.position, Vector2.left, wallCheckDistance, groundLayer);

        if (Physics2D.Raycast(wallCheck.position, Vector2.right, wallCheckDistance, groundLayer))
        {
            wallDirection = 1;
        }
        else if (Physics2D.Raycast(wallCheck.position, Vector2.left, wallCheckDistance, groundLayer))
        {
            wallDirection = -1;
        }
    }

    private void WallSlide()
    {
        if (isTouchingWall && !isGrounded && !isDashing)
        {
            isWallSliding = true;

            // Limit falling speed
            if (rb.linearVelocity.y < -wallSlideSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
            }
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void WallJump()
    {
        if (jumpAction.WasPressedThisFrame() &&
            isTouchingWall && !isGrounded && !isDashing)
        {
            // Jump away from the wall
            rb.linearVelocity = new Vector2(
                -wallDirection * wallJumpForce.x,
                wallJumpForce.y
            );

            isWallSliding = false;
            isTouchingWall = false;

            // Prevent immediate horizontal steering
            wallJumpCounter = wallJumpControlTime;

            // Reset normal jump timers
            isJumping = false;
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;

            // Prevent wall detection from immediately reactivating
            wallDirection = 0;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("ground"))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("ground"))
        {
            isGrounded = false;
        }
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        nextDashTime = Time.time + dashCooldown;

        // Determine dash direction
        if (movement.sqrMagnitude > 0.01f)
        {
            dashDirection = movement.normalized;
        }
        else
        {
            dashDirection = new Vector2(facingDirection, 0f);
        }

        // Disable gravity during dash
        rb.gravityScale = 0f;

        // Dash
        rb.linearVelocity = dashDirection * dashSpeed;

        yield return new WaitForSeconds(dashDuration);

        // End dash
        rb.gravityScale = originalGravity;
        rb.linearVelocity = Vector2.zero;

        isDashing = false;
    }
}

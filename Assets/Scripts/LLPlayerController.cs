
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class LLPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float walkSpeed = 3f;
    [SerializeField, Min(0f)] private float crawlSpeed = 1.5f;
    [SerializeField, Min(0f)] private float jumpHeight = 1.5f;
    [SerializeField, Min(0f)] private float gravityScale = 3f;

    [Header("Jump Animation")]
    [SerializeField, Min(0f)]
    private float jumpStartupDelay = 0.25f;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayers;
    [SerializeField, Min(0f)] private float jumpBufferDuration = 0.12f;

    [Header("Standing Clearance")]
    [SerializeField] private Transform standCheck;
    [SerializeField, Min(0.01f)] private float standCheckRadius = 0.15f;
    [SerializeField] private LayerMask standingClearanceLayers;

    [Header("Leg Sprites")]
    [SerializeField] private GameObject leftLegSprite;
    [SerializeField] private GameObject rightLegSprite;
    [SerializeField] private bool startsWithLegs = true;

    [Header("Crawling Collider")]
    [SerializeField, Range(0.2f, 1f)]
    private float crawlColliderHeightRatio = 0.55f;

    [Header("Legless Pose")]
    [SerializeField] private Transform visualRoot;
    [SerializeField, Min(0.05f)] private float fallOverDuration = 0.25f;
    [SerializeField] private Transform facingRoot;

    [Header("Legless Visual Offset")]
    [SerializeField] private float leglessVisualOffsetX = 0f;
    [SerializeField] private float leglessVisualOffsetY = 0f;

    [Header("Facing")]
    [SerializeField] private float leftFacingOffsetX = 0f;

    private Rigidbody2D rb;
    private CapsuleCollider2D bodyCollider;
    private Animator animator;

    private Vector2 standingColliderSize;
    private Vector2 standingColliderOffset;
    private CapsuleDirection2D standingColliderDirection;

    private Quaternion originalVisualRotation;
    private Vector3 originalVisualLocalPosition;

    private Coroutine fallOverRoutine;
    private Coroutine standUpRoutine;

    private float horizontalInput;
    private float jumpBufferCounter;
    private float jumpStartupCounter;

    private bool isGrounded;
    private bool isCrawling;
    private bool hasLegs;
    private bool isLegless;

    private bool isJumpStartup;
    private bool jumpReadyToLaunch;
    private bool hasLaunchedJump;

    private int facingDirection = 1;

    public bool IsGrounded => isGrounded;
    public bool IsCrawling => isCrawling;
    public bool HasLegs => hasLegs;

    // The climbing controller sets this while it owns player movement.
    // Normal movement must not overwrite climbing velocity.
    public bool ExternalMovementControl { get; set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<CapsuleCollider2D>();

        if (visualRoot != null)
            animator = visualRoot.GetComponent<Animator>();

        rb.gravityScale = gravityScale;
        rb.freezeRotation = true;

        standingColliderSize = bodyCollider.size;
        standingColliderOffset = bodyCollider.offset;
        standingColliderDirection = bodyCollider.direction;

        if (visualRoot != null)
        {
            originalVisualRotation = visualRoot.localRotation;
            originalVisualLocalPosition = visualRoot.localPosition;
        }

        hasLegs = startsWithLegs;
        isLegless = !hasLegs;

        ApplyLegSprites();
        UpdateCrawlState(false);
        UpdateFacingDirection();
        UpdateAnimatorParameters();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        // While climbing controls movement, do not collect normal movement
        // inputs. Keep visual animation and state updates running.
        if (ExternalMovementControl)
        {
            horizontalInput = 0f;
            jumpBufferCounter = 0f;

            if (isJumpStartup)
                CancelJumpStartup();

            UpdateAnimatorParameters();
            return;
        }

        // No keyboard available: clear input without affecting physics.
        if (keyboard == null)
        {
            horizontalInput = 0f;
            UpdateAnimatorParameters();
            return;
        }

        // Legless characters cannot walk, crawl, or jump through keyboard
        // movement. Their Rigidbody physics and climbing remain active.
        horizontalInput = 0f;

        if (hasLegs && !isLegless)
        {
            if (keyboard.aKey.isPressed)
                horizontalInput -= 1f;

            if (keyboard.dKey.isPressed)
                horizontalInput += 1f;
        }

        if (horizontalInput < 0f)
            facingDirection = -1;
        else if (horizontalInput > 0f)
            facingDirection = 1;

        UpdateFacingDirection();

        // Crawling only applies while the character has legs.
        if (hasLegs && !isLegless)
            UpdateCrawlState(keyboard.leftShiftKey.isPressed);

        bool canUseNormalMovement = hasLegs && !isLegless;

        if (canUseNormalMovement &&
            keyboard.spaceKey.wasPressedThisFrame &&
            !isCrawling)
        {
            jumpBufferCounter = jumpBufferDuration;
        }
        else
        {
            jumpBufferCounter = Mathf.Max(
                0f,
                jumpBufferCounter - Time.deltaTime
            );
        }

        if (isJumpStartup && (!canUseNormalMovement || isCrawling))
            CancelJumpStartup();

        if (!isJumpStartup &&
            !hasLaunchedJump &&
            jumpBufferCounter > 0f &&
            isGrounded &&
            !isCrawling &&
            canUseNormalMovement)
        {
            BeginJumpStartup();
        }

        if (isJumpStartup)
        {
            jumpStartupCounter -= Time.deltaTime;

            if (jumpStartupCounter <= 0f)
                jumpReadyToLaunch = true;
        }

        UpdateAnimatorParameters();
    }

    private void BeginJumpStartup()
    {
        isJumpStartup = true;
        jumpReadyToLaunch = false;
        jumpStartupCounter = jumpStartupDelay;
        jumpBufferCounter = 0f;

        if (animator != null)
        {
            animator.ResetTrigger("Jump");
            animator.SetBool("IsJumping", true);
            animator.SetTrigger("Jump");
        }
    }

    private void CancelJumpStartup()
    {
        isJumpStartup = false;
        jumpReadyToLaunch = false;
        jumpStartupCounter = 0f;
        jumpBufferCounter = 0f;

        if (animator != null)
        {
            animator.ResetTrigger("Jump");
            animator.SetBool("IsJumping", false);
        }
    }

    private void UpdateFacingDirection()
    {
        if (visualRoot == null)
            return;

        Vector3 scale = visualRoot.localScale;
        scale.x = Mathf.Abs(scale.x) * facingDirection;
        visualRoot.localScale = scale;

        // The fall-over animation owns the visual position while legless.
        if (isLegless)
            return;

        Vector3 position = originalVisualLocalPosition;

        if (facingDirection < 0)
            position.x += leftFacingOffsetX;

        visualRoot.localPosition = position;
    }

    private void FixedUpdate()
    {
        CheckGrounded();

        // Do not write to Rigidbody velocity while the climbing controller
        // owns movement. Gravity, joints, collisions, and physics still work.
        if (ExternalMovementControl)
        {
            UpdateAnimatorParameters();
            return;
        }

        Vector2 velocity = rb.linearVelocity;

        if (hasLegs && !isLegless)
        {
            float speed = isCrawling ? crawlSpeed : walkSpeed;
            velocity.x = horizontalInput * speed;
        }

        // Crucially, when legless, do not set velocity.x to zero.
        // Existing momentum and climbing launch velocity are preserved.

        if (jumpReadyToLaunch)
        {
            if (isGrounded &&
                !isCrawling &&
                hasLegs &&
                !isLegless)
            {
                float jumpVelocity = Mathf.Sqrt(
                    2f * Mathf.Abs(
                        Physics2D.gravity.y * rb.gravityScale
                    ) * jumpHeight
                );

                velocity.y = jumpVelocity;

                isGrounded = false;
                hasLaunchedJump = true;
            }

            isJumpStartup = false;
            jumpReadyToLaunch = false;
            jumpStartupCounter = 0f;
        }

        if (hasLaunchedJump &&
            isGrounded &&
            velocity.y <= 0f)
        {
            hasLaunchedJump = false;

            if (animator != null)
                animator.SetBool("IsJumping", false);
        }

        rb.linearVelocity = velocity;

        UpdateAnimatorParameters();
    }

    private void UpdateAnimatorParameters()
    {
        if (animator == null)
            return;

        float animationSpeed = ExternalMovementControl
            ? rb != null ? rb.linearVelocity.magnitude : 0f
            : Mathf.Abs(horizontalInput) *
              (isCrawling ? crawlSpeed : walkSpeed);

        animator.SetFloat("Speed", animationSpeed);
        animator.SetBool("IsCrouching", isCrawling);
        animator.SetBool("IsLegless", isLegless);
        animator.SetBool("IsGrounded", isGrounded);

        animator.SetBool(
            "IsJumping",
            isJumpStartup || hasLaunchedJump
        );
    }

    private void CheckGrounded()
    {
        isGrounded = false;

        if (groundCheck == null)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            groundCheck.position,
            groundCheckRadius,
            groundLayers
        );

        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.isTrigger)
                continue;

            if (hit.attachedRigidbody == rb)
                continue;

            isGrounded = true;
            break;
        }
    }

    private void UpdateCrawlState(bool crawlRequested)
    {
        if (isLegless)
            return;

        bool shouldCrawl = crawlRequested;

        if (!shouldCrawl && isCrawling && !CanStandUp())
            shouldCrawl = true;

        if (isCrawling == shouldCrawl)
            return;

        isCrawling = shouldCrawl;

        if (isCrawling)
        {
            bodyCollider.direction = CapsuleDirection2D.Horizontal;

            bodyCollider.size = new Vector2(
                standingColliderSize.y,
                standingColliderSize.x
            );

            bodyCollider.offset = standingColliderOffset;
            jumpBufferCounter = 0f;
        }
        else
        {
            bodyCollider.direction = standingColliderDirection;
            bodyCollider.size = standingColliderSize;
            bodyCollider.offset = standingColliderOffset;

            if (visualRoot != null)
            {
                visualRoot.localRotation = originalVisualRotation;
                visualRoot.localPosition = originalVisualLocalPosition;
            }
        }
    }

    private bool CanStandUp()
    {
        if (standCheck == null)
            return true;

        float standingHeight = standingColliderSize.y;
        float crawlHeight = standingHeight * crawlColliderHeightRatio;
        float extraHeight = standingHeight - crawlHeight;

        Vector2 checkCenter = (Vector2)transform.position
            + standingColliderOffset
            + Vector2.up * (standingColliderSize.y * 0.5f);

        checkCenter.y -= extraHeight * 0.5f;

        Vector2 checkSize = new Vector2(
            Mathf.Max(0.01f, standingColliderSize.x - 0.02f),
            Mathf.Max(0.01f, extraHeight - 0.02f)
        );

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            checkCenter,
            checkSize,
            0f,
            standingClearanceLayers
        );

        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.isTrigger)
                continue;

            if (hit == bodyCollider || hit.attachedRigidbody == rb)
                continue;

            return false;
        }

        return true;
    }

    private void ApplyLegSprites()
    {
        if (leftLegSprite != null)
            leftLegSprite.SetActive(hasLegs);

        if (rightLegSprite != null)
            rightLegSprite.SetActive(hasLegs);
    }

    public void SetLegsEnabled(bool enabled)
    {
        if (hasLegs == enabled)
            return;

        hasLegs = enabled;
        isLegless = !enabled;

        ApplyLegSprites();
        jumpBufferCounter = 0f;

        if (isJumpStartup)
            CancelJumpStartup();

        if (fallOverRoutine != null)
        {
            StopCoroutine(fallOverRoutine);
            fallOverRoutine = null;
        }

        if (standUpRoutine != null)
        {
            StopCoroutine(standUpRoutine);
            standUpRoutine = null;
        }

        if (isLegless)
        {
            isCrawling = false;
            hasLaunchedJump = false;

            bodyCollider.direction = CapsuleDirection2D.Horizontal;
            bodyCollider.size = new Vector2(
                standingColliderSize.y,
                standingColliderSize.x
            );
            bodyCollider.offset = standingColliderOffset;

            if (visualRoot != null)
                fallOverRoutine = StartCoroutine(FallOver());
        }
        else
        {
            bodyCollider.direction = standingColliderDirection;
            bodyCollider.size = standingColliderSize;
            bodyCollider.offset = standingColliderOffset;

            if (visualRoot != null)
            {
                visualRoot.localRotation = originalVisualRotation;
                visualRoot.localPosition = originalVisualLocalPosition;
            }

            UpdateCrawlState(false);
            UpdateFacingDirection();
        }

        UpdateAnimatorParameters();
    }

    private Vector3 GetLeglessOffsetWorld()
    {
        return transform.TransformVector(
            new Vector3(
                leglessVisualOffsetX,
                leglessVisualOffsetY,
                0f
            )
        );
    }

    private IEnumerator FallOver()
    {
        Quaternion startRotation = visualRoot.localRotation;

        float direction = horizontalInput < 0f ? 1f : -1f;

        Quaternion targetRotation =
            originalVisualRotation *
            Quaternion.Euler(0f, 0f, direction * 90f);

        Vector2 colliderCenter = bodyCollider.offset;

        Vector3 pivotWorld = transform.TransformPoint(colliderCenter);
        Vector3 baseVisualPosition =
            transform.TransformPoint(originalVisualLocalPosition);

        Vector3 visualOffset = baseVisualPosition - pivotWorld;

        float elapsed = 0f;

        while (elapsed < fallOverDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / fallOverDuration);
            t = t * t * (3f - 2f * t);

            visualRoot.localRotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                t
            );

            Vector3 rotatedOffset =
                Quaternion.AngleAxis(
                    direction * 90f * t,
                    transform.forward
                ) * visualOffset;

            visualRoot.position =
                pivotWorld +
                rotatedOffset +
                Vector3.down * (0.2f * t) +
                GetLeglessOffsetWorld() * t;

            yield return null;
        }

        visualRoot.localRotation = targetRotation;

        Vector3 finalOffset =
            Quaternion.AngleAxis(
                direction * 90f,
                transform.forward
            ) * visualOffset;

        visualRoot.position =
            pivotWorld +
            finalOffset +
            Vector3.down * 0.20f +
            GetLeglessOffsetWorld();

        fallOverRoutine = null;
    }

    private IEnumerator StandUp()
    {
        Quaternion startRotation = visualRoot.localRotation;
        Vector3 startPosition = visualRoot.localPosition;

        float elapsed = 0f;

        while (elapsed < fallOverDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / fallOverDuration);
            t = t * t * (3f - 2f * t);

            visualRoot.localRotation = Quaternion.Slerp(
                startRotation,
                originalVisualRotation,
                t
            );

            visualRoot.localPosition = Vector3.Lerp(
                startPosition,
                originalVisualLocalPosition,
                t
            );

            yield return null;
        }

        visualRoot.localRotation = originalVisualRotation;
        visualRoot.localPosition = originalVisualLocalPosition;

        standUpRoutine = null;
    }

    public void ToggleLegs()
    {
        SetLegsEnabled(!hasLegs);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(
                groundCheck.position,
                groundCheckRadius
            );
        }

        if (standCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(
                standCheck.position,
                standCheckRadius
            );
        }
    }
}

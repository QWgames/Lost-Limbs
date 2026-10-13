using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(AudioSource))]
public class LLPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float walkSpeed = 3f;
    [SerializeField, Min(0f)] private float crawlSpeed = 1.5f;
    [SerializeField, Min(0f)] private float jumpHeight = 1.5f;
    [SerializeField, Min(0f)] private float gravityScale = 3f;

    [Header("Jump Animation")]
    [SerializeField, Min(0f)] private float jumpStartupDelay = 0.25f;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayers;
    [SerializeField, Min(0f)] private float jumpBufferDuration = 0.12f;

    [Header("Standing Clearance")]
    [SerializeField] private Transform standCheck;
    [SerializeField] private Vector2 standCheckSize = new Vector2(0.6f, 0.4f);
    [SerializeField] private Vector2 standCheckOffset = new Vector2(0f, 0.5f);
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

    [Header("Legless Visual Offset — Facing Right")]
    [Tooltip("Horizontal offset when the legless character faces right.")]
    [SerializeField] private float rightFacingOffsetX = 0f;

    [Tooltip("Vertical offset when the legless character faces right.")]
    [SerializeField] private float rightFacingOffsetY = 0f;

    [Header("Legless Visual Offset — Facing Left")]
    [Tooltip("Horizontal offset when the legless character faces left.")]
    [SerializeField] private float leftFacingLeglessOffsetX = 0f;

    [Tooltip("Vertical offset when the legless character faces left.")]
    [SerializeField] private float leftFacingLeglessOffsetY = 0f;

    [Header("Facing")]
    [SerializeField] private float leftFacingOffsetX = 0f;

    // =========================================================
    // AUDIO
    // =========================================================

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Movement Sound Effects")]
    [SerializeField] private AudioClip walkSFX;
    [SerializeField] private AudioClip jumpSFX;
    [SerializeField] private AudioClip crouchSFX;
    [SerializeField] private AudioClip crawlSFX;
    [SerializeField] private AudioClip landSFX;

    [Header("Movement Sound Settings")]
    [Tooltip("Delay between repeated walking sounds.")]
    [SerializeField, Min(0.05f)] private float walkSFXInterval = 0.35f;

    [Tooltip("Delay between repeated crawling sounds.")]
    [SerializeField, Min(0.05f)] private float crawlSFXInterval = 0.5f;

    [Tooltip("Minimum downward speed required to play the landing sound.")]
    [SerializeField, Min(0f)] private float landingVelocityThreshold = 1f;

    [Header("Randomized Audio Pitch")]
    [Tooltip("Maximum random pitch variation. 0.08 means +/- 8 percent.")]
    [SerializeField, Range(0f, 1f)] private float pitchVariation = 0.08f;

    private float walkSFXTimer;
    private float crawlSFXTimer;
    private bool wasAirborne;
    private bool wasMovingOnGround;

    // =========================================================
    // COMPONENTS AND STATE
    // =========================================================

    private Rigidbody2D rb;
    private CapsuleCollider2D bodyCollider;
    private Animator animator;

    private Vector2 standingColliderSize;
    private Vector2 standingColliderOffset;
    private CapsuleDirection2D standingColliderDirection;

    private Quaternion originalVisualRotation;
    private Vector3 originalVisualLocalPosition;
    private Vector3 originalVisualLocalScale;

    private Coroutine fallOverRoutine;
    private Coroutine standUpRoutine;

    private float horizontalInput;
    private float jumpBufferCounter;
    private float jumpStartupCounter;

    private bool isGrounded;
    private bool wasGrounded;
    private bool isCrawling;
    private bool hasLegs;
    private bool isLegless;
    private bool isJumpStartup;
    private bool jumpReadyToLaunch;
    private bool hasLaunchedJump;

    private int facingDirection = 1;
    private float leglessRotationDirection = -1f;

    public bool IsGrounded => isGrounded;
    public bool IsCrawling => isCrawling;
    public bool HasLegs => hasLegs;

    public bool ExternalMovementControl { get; set; }
    public bool SwimmingMovementControl { get; set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<CapsuleCollider2D>();

        if (visualRoot != null)
            animator = visualRoot.GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        rb.gravityScale = gravityScale;
        rb.freezeRotation = true;

        standingColliderSize = bodyCollider.size;
        standingColliderOffset = bodyCollider.offset;
        standingColliderDirection = bodyCollider.direction;

        if (visualRoot != null)
        {
            originalVisualRotation = visualRoot.localRotation;
            originalVisualLocalPosition = visualRoot.localPosition;
            originalVisualLocalScale = visualRoot.localScale;
        }

        hasLegs = startsWithLegs;
        isLegless = !hasLegs;

        ApplyLegSprites();
        UpdateCrawlState(false);
        UpdateFacingDirection();

        if (isLegless && visualRoot != null)
        {
            leglessRotationDirection = facingDirection < 0 ? 1f : -1f;
            visualRoot.localPosition = GetLeglessTargetLocalPosition();
            visualRoot.localRotation = GetLeglessTargetLocalRotation();
        }

        UpdateAnimatorParameters();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (ExternalMovementControl || SwimmingMovementControl)
        {
            horizontalInput = 0f;
            jumpBufferCounter = 0f;

            if (isJumpStartup)
                CancelJumpStartup();

            UpdateAnimatorParameters();
            return;
        }

        if (keyboard == null)
        {
            horizontalInput = 0f;
            UpdateAnimatorParameters();
            return;
        }

        horizontalInput = 0f;

        if (hasLegs && !isLegless)
        {
            if (keyboard.aKey.isPressed)
                horizontalInput -= 1f;

            if (keyboard.dKey.isPressed)
                horizontalInput += 1f;
        }

        if (keyboard.aKey.isPressed)
            SetFacingDirection(-1);
        else if (keyboard.dKey.isPressed)
            SetFacingDirection(1);

        UpdateFacingDirection();

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

    private void LateUpdate()
    {
        if (!isLegless || visualRoot == null)
            return;

        if (fallOverRoutine == null)
        {
            visualRoot.localPosition = GetLeglessTargetLocalPosition();
            visualRoot.localRotation = GetLeglessTargetLocalRotation();
        }
    }

    private void SetFacingDirection(int direction)
    {
        if (facingDirection == direction)
            return;

        facingDirection = direction;

        if (isLegless)
        {
            leglessRotationDirection = facingDirection < 0 ? 1f : -1f;

            if (fallOverRoutine == null && visualRoot != null)
            {
                visualRoot.localPosition = GetLeglessTargetLocalPosition();
                visualRoot.localRotation = GetLeglessTargetLocalRotation();
            }
        }
    }

    private Quaternion GetLeglessTargetLocalRotation()
    {
        return originalVisualRotation *
               Quaternion.Euler(0f, 0f, leglessRotationDirection * 90f);
    }

    private void BeginJumpStartup()
    {
        isJumpStartup = true;
        jumpReadyToLaunch = false;
        jumpStartupCounter = jumpStartupDelay;
        jumpBufferCounter = 0f;

        PlaySFX(jumpSFX);

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

        Vector3 scale = originalVisualLocalScale;

        if (isLegless)
        {
            scale.x = Mathf.Abs(originalVisualLocalScale.x);
            visualRoot.localScale = scale;
            return;
        }

        scale.x = Mathf.Abs(originalVisualLocalScale.x) * facingDirection;
        visualRoot.localScale = scale;

        Vector3 position = originalVisualLocalPosition;

        if (facingDirection < 0)
            position.x += leftFacingOffsetX;

        visualRoot.localPosition = position;
        visualRoot.localRotation = originalVisualRotation;
    }

    private void FixedUpdate()
    {
        wasGrounded = isGrounded;

        CheckGrounded();

        // Detect landing after being airborne.
        if (!wasGrounded && isGrounded)
        {
            if (rb.linearVelocity.y <= -landingVelocityThreshold &&
                wasAirborne)
            {
                PlaySFX(landSFX);
            }

            wasAirborne = false;
        }
        else if (!isGrounded)
        {
            wasAirborne = true;
        }

        if (ExternalMovementControl || SwimmingMovementControl)
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
                wasAirborne = true;
            }

            isJumpStartup = false;
            jumpReadyToLaunch = false;
            jumpStartupCounter = 0f;
        }

        if (hasLaunchedJump && isGrounded && velocity.y <= 0f)
        {
            hasLaunchedJump = false;

            if (animator != null)
                animator.SetBool("IsJumping", false);
        }

        rb.linearVelocity = velocity;

        UpdateMovementSFX();
        UpdateAnimatorParameters();
    }

    private void UpdateMovementSFX()
    {
        if (audioSource == null)
            return;

        bool canMoveNormally =
            hasLegs &&
            !isLegless &&
            !ExternalMovementControl &&
            !SwimmingMovementControl;

        bool movingHorizontally = Mathf.Abs(horizontalInput) > 0.01f;
        bool movingOnGround = isGrounded && movingHorizontally && canMoveNormally;

        if (!movingOnGround)
        {
            walkSFXTimer = 0f;
            crawlSFXTimer = 0f;
            wasMovingOnGround = false;
            return;
        }

        if (!wasMovingOnGround)
        {
            walkSFXTimer = 0f;
            crawlSFXTimer = 0f;
            wasMovingOnGround = true;
        }

        if (isCrawling)
        {
            walkSFXTimer = 0f;
            crawlSFXTimer -= Time.fixedDeltaTime;

            if (crawlSFXTimer <= 0f)
            {
                PlaySFX(crawlSFX);
                crawlSFXTimer = crawlSFXInterval;
            }
        }
        else
        {
            crawlSFXTimer = 0f;
            walkSFXTimer -= Time.fixedDeltaTime;

            if (walkSFXTimer <= 0f)
            {
                PlaySFX(walkSFX);
                walkSFXTimer = walkSFXInterval;
            }
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        // Save the AudioSource's original pitch.
        float originalPitch = audioSource.pitch;

        // Apply a new random pitch for this sound.
        float randomizedPitch = Random.Range(
            1f - pitchVariation,
            1f + pitchVariation
        );

        audioSource.pitch = originalPitch * randomizedPitch;

        // PlayOneShot uses the current AudioSource pitch.
        audioSource.PlayOneShot(clip);

        // Restore the original pitch for subsequent sounds.
        audioSource.pitch = originalPitch;
    }

    private void UpdateAnimatorParameters()
    {
        if (animator == null)
            return;

        float animationSpeed = ExternalMovementControl
            ? (rb != null ? rb.linearVelocity.magnitude : 0f)
            : Mathf.Abs(horizontalInput) *
              (isCrawling ? crawlSpeed : walkSpeed);

        animator.SetFloat("Speed", animationSpeed);
        animator.SetBool("IsCrouching", isCrawling);
        animator.SetBool("IsLegless", isLegless);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsJumping", isJumpStartup || hasLaunchedJump);
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
            // Play once when entering the crouched/crawling state.
            PlaySFX(crouchSFX);

            bodyCollider.direction = CapsuleDirection2D.Horizontal;
            bodyCollider.size = new Vector2(
                standingColliderSize.y,
                standingColliderSize.x
            );
            bodyCollider.offset = standingColliderOffset;
            jumpBufferCounter = 0f;

            crawlSFXTimer = 0f;
        }
        else
        {
            bodyCollider.direction = standingColliderDirection;
            bodyCollider.size = standingColliderSize;
            bodyCollider.offset = standingColliderOffset;

            walkSFXTimer = 0f;

            if (visualRoot != null)
            {
                visualRoot.localRotation = originalVisualRotation;
                visualRoot.localPosition = originalVisualLocalPosition;
            }
        }
    }

    private bool CanStandUp()
    {
        Vector2 checkCenter = standCheck != null
            ? (Vector2)standCheck.position + standCheckOffset
            : (Vector2)transform.position + standCheckOffset;

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            checkCenter,
            standCheckSize,
            0f,
            standingClearanceLayers
        );

        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.isTrigger)
                continue;

            if (hit.attachedRigidbody == rb)
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

            leglessRotationDirection = facingDirection < 0 ? 1f : -1f;

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

    private Vector3 GetLeglessTargetLocalPosition()
    {
        float offsetX;
        float offsetY;

        if (facingDirection < 0)
        {
            offsetX = leftFacingLeglessOffsetX;
            offsetY = leftFacingLeglessOffsetY;
        }
        else
        {
            offsetX = rightFacingOffsetX;
            offsetY = rightFacingOffsetY;
        }

        return originalVisualLocalPosition + new Vector3(
            offsetX,
            offsetY,
            0f
        );
    }

    private IEnumerator FallOver()
    {
        Vector3 startPosition = visualRoot.localPosition;
        Quaternion startRotation = visualRoot.localRotation;

        leglessRotationDirection = facingDirection < 0 ? 1f : -1f;

        float elapsed = 0f;

        while (elapsed < fallOverDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / fallOverDuration);
            t = t * t * (3f - 2f * t);

            Vector3 targetPosition = GetLeglessTargetLocalPosition();
            Quaternion targetRotation = GetLeglessTargetLocalRotation();

            visualRoot.localPosition = Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

            visualRoot.localRotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                t
            );

            yield return null;
        }

        visualRoot.localPosition = GetLeglessTargetLocalPosition();
        visualRoot.localRotation = GetLeglessTargetLocalRotation();

        fallOverRoutine = null;
    }

    private IEnumerator StandUp()
    {
        Vector3 startPosition = visualRoot.localPosition;
        Quaternion startRotation = visualRoot.localRotation;

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

        Vector2 standCenter = standCheck != null
            ? (Vector2)standCheck.position + standCheckOffset
            : (Vector2)transform.position + standCheckOffset;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(standCenter, standCheckSize);
    }
}


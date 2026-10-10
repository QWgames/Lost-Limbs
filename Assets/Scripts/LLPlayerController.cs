
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class LLPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float walkSpeed = 5f;
    [SerializeField, Min(0f)] private float crawlSpeed = 2.5f;
    [SerializeField, Min(0f)] private float jumpHeight = 2.5f;
    [SerializeField, Min(0f)] private float gravityScale = 3f;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField, Min(0f)] private float jumpBufferDuration = 0.12f;

    [Header("Crawl Mode")]
    [SerializeField] private Transform hitboxTransform;
    [SerializeField] private CapsuleCollider2D bodyCollider;
    [SerializeField] private Vector2 standingHitboxOffset = Vector2.zero;
    [SerializeField] private Vector2 crawlingHitboxOffset = Vector2.zero;
    [SerializeField] private float crawlingRotation = 90f;
    [SerializeField] private bool rotateHitboxWhenCrawling = true;

    [Header("Visual Rotation")]
    [SerializeField] private Transform visualTransform;
    [SerializeField] private bool rotateVisualWhenCrawling = true;
    [SerializeField] private float crawlingVisualRotation = 90f;

    [Header("Legs")]
    [SerializeField] private bool startsWithLegs = true;
    [SerializeField] private GameObject legsObject;

    [Header("Crawling Legs Position")]
    [SerializeField]
    private Vector2 crawlingLegsOffset =
        new Vector2(0f, -0.25f);
    [SerializeField] private float crawlingLegsRotation = 0f;

    [Header("Standing Clearance")]
    [SerializeField] private LayerMask standingClearanceLayers = ~0;

    [Header("Animation (Optional)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string groundedParameter = "Grounded";
    [SerializeField] private string crawlingParameter = "Crawling";
    [SerializeField] private string hasLegsParameter = "HasLegs";
    [SerializeField] private string jumpParameter = "Jump";
    [SerializeField] private string facingParameter = "Facing";

    private Rigidbody2D rb;

    private float horizontalInput;
    private float jumpBufferCounter;

    private bool isGrounded;
    private bool isCrawling;
    private bool hasLegs;
    private bool facingRight = true;

    private Vector3 originalLegsPosition;
    private Quaternion originalLegsRotation;

    public bool IsGrounded => isGrounded;
    public bool IsCrawling => isCrawling;
    public bool HasLegs => hasLegs;
    public float HorizontalInput => horizontalInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = gravityScale;
        rb.freezeRotation = true;

        if (hitboxTransform == null && bodyCollider != null)
            hitboxTransform = bodyCollider.transform;

        // Remember the legs' original standing pose.
        if (legsObject != null)
        {
            originalLegsPosition =
                legsObject.transform.localPosition;

            originalLegsRotation =
                legsObject.transform.localRotation;
        }

        hasLegs = startsWithLegs;

        if (legsObject != null)
            legsObject.SetActive(hasLegs);

        UpdateCrawlState(false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        // Keep leg state synchronized if changed externally.
        if (legsObject != null &&
            legsObject.activeSelf != hasLegs)
        {
            SetLegsEnabled(legsObject.activeSelf);
        }

        horizontalInput = 0f;

        if (keyboard.aKey.isPressed)
            horizontalInput -= 1f;

        if (keyboard.dKey.isPressed)
            horizontalInput += 1f;

        // Shift requests crawling. The character may remain
        // crawling after Shift is released if standing is blocked.
        UpdateCrawlState(keyboard.leftShiftKey.isPressed);

        if (keyboard.spaceKey.wasPressedThisFrame &&
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

        UpdateFacing();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        CheckGrounded();

        float movement = hasLegs ? horizontalInput : 0f;
        float speed = isCrawling ? crawlSpeed : walkSpeed;

        Vector2 velocity = rb.linearVelocity;
        velocity.x = movement * speed;

        if (jumpBufferCounter > 0f &&
            isGrounded &&
            !isCrawling)
        {
            float jumpVelocity = Mathf.Sqrt(
                2f *
                Mathf.Abs(
                    Physics2D.gravity.y * rb.gravityScale
                ) *
                jumpHeight
            );

            velocity.y = jumpVelocity;

            isGrounded = false;
            jumpBufferCounter = 0f;

            if (animator != null &&
                !string.IsNullOrEmpty(jumpParameter))
            {
                animator.SetTrigger(jumpParameter);
            }
        }

        rb.linearVelocity = velocity;
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

            // Ignore the player's own colliders.
            if (hit.attachedRigidbody == rb ||
                hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                continue;
            }

            isGrounded = true;
            break;
        }
    }

    private void UpdateCrawlState(bool crawlRequested)
    {
        bool shouldCrawl = !hasLegs || crawlRequested;

        // Do not stand up if the standing hitbox would overlap
        // a solid object above or around the character.
        if (!shouldCrawl && isCrawling && !CanStandUp())
        {
            shouldCrawl = true;
        }

        isCrawling = shouldCrawl;

        if (hitboxTransform != null)
        {
            hitboxTransform.localRotation = Quaternion.Euler(
                0f,
                0f,
                rotateHitboxWhenCrawling && isCrawling
                    ? crawlingRotation
                    : 0f
            );

            hitboxTransform.localPosition = isCrawling
                ? crawlingHitboxOffset
                : standingHitboxOffset;
        }

        if (visualTransform != null)
        {
            visualTransform.localRotation = Quaternion.Euler(
                0f,
                0f,
                rotateVisualWhenCrawling && isCrawling
                    ? crawlingVisualRotation
                    : 0f
            );
        }

        // Move and rotate the legs relative to their original pose.
        if (legsObject != null)
        {
            Transform legs = legsObject.transform;

            legs.localPosition = originalLegsPosition +
                (isCrawling
                    ? (Vector3)crawlingLegsOffset
                    : Vector3.zero);

            legs.localRotation = isCrawling
                ? originalLegsRotation *
                  Quaternion.Euler(
                      0f, 0f, crawlingLegsRotation
                  )
                : originalLegsRotation;
        }

        if (isCrawling)
            jumpBufferCounter = 0f;
    }



    private bool CanStandUp()
    {
        if (bodyCollider == null || hitboxTransform == null)
            return true;

        // Remember the top of the current crawling hitbox.
        float crawlingTop = bodyCollider.bounds.max.y;

        Vector3 originalPosition = hitboxTransform.localPosition;
        Quaternion originalRotation = hitboxTransform.localRotation;

        // Temporarily set the collider to its standing pose.
        hitboxTransform.localPosition = standingHitboxOffset;
        hitboxTransform.localRotation = Quaternion.identity;

        Physics2D.SyncTransforms();

        Vector2 center = bodyCollider.transform.TransformPoint(
            bodyCollider.offset
        );

        Vector2 size = Vector2.Scale(
            bodyCollider.size,
            new Vector2(
                Mathf.Abs(bodyCollider.transform.lossyScale.x),
                Mathf.Abs(bodyCollider.transform.lossyScale.y)
            )
        );

        Collider2D[] hits = Physics2D.OverlapCapsuleAll(
            center,
            size,
            bodyCollider.direction,
            bodyCollider.transform.eulerAngles.z,
            standingClearanceLayers
        );

        bool blocked = false;

        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.isTrigger)
                continue;

            if (hit.attachedRigidbody == rb ||
                hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                continue;
            }

            // Ignore the floor and objects that don't extend into
            // the additional space required to stand.
            if (hit.bounds.max.y <= crawlingTop + 0.02f)
                continue;

            blocked = true;
            break;
        }

        // Restore the crawling hitbox.
        hitboxTransform.localPosition = originalPosition;
        hitboxTransform.localRotation = originalRotation;

        Physics2D.SyncTransforms();

        return !blocked;
    }



    private void UpdateFacing()
    {
        if (horizontalInput > 0f)
            facingRight = true;
        else if (horizontalInput < 0f)
            facingRight = false;
    }

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        if (!string.IsNullOrEmpty(speedParameter))
        {
            animator.SetFloat(
                speedParameter,
                Mathf.Abs(hasLegs ? horizontalInput : 0f)
            );
        }

        if (!string.IsNullOrEmpty(groundedParameter))
            animator.SetBool(groundedParameter, isGrounded);

        if (!string.IsNullOrEmpty(crawlingParameter))
            animator.SetBool(crawlingParameter, isCrawling);

        if (!string.IsNullOrEmpty(hasLegsParameter))
            animator.SetBool(hasLegsParameter, hasLegs);

        if (!string.IsNullOrEmpty(facingParameter))
        {
            animator.SetFloat(
                facingParameter,
                facingRight ? 1f : -1f
            );
        }
    }

    public void SetLegsEnabled(bool enabled)
    {
        hasLegs = enabled;

        if (legsObject != null &&
            legsObject.activeSelf != enabled)
        {
            legsObject.SetActive(enabled);
        }

        if (!hasLegs)
            jumpBufferCounter = 0f;

        UpdateCrawlState(false);
    }

    public void ToggleLegs()
    {
        SetLegsEnabled(!hasLegs);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded
                ? Color.green
                : Color.yellow;

            Gizmos.DrawWireSphere(
                groundCheck.position,
                groundCheckRadius
            );
        }

        if (bodyCollider != null &&
            hitboxTransform != null)
        {
            Gizmos.color = Color.cyan;

            Transform parent = hitboxTransform.parent;

            Vector3 position = parent != null
                ? parent.TransformPoint(standingHitboxOffset)
                : standingHitboxOffset;

            Quaternion rotation = parent != null
                ? parent.rotation
                : Quaternion.identity;

            Gizmos.matrix = Matrix4x4.TRS(
                position,
                rotation,
                Vector3.one
            );

            Vector3 scaledSize = new Vector3(
                bodyCollider.size.x *
                    Mathf.Abs(hitboxTransform.lossyScale.x),
                bodyCollider.size.y *
                    Mathf.Abs(hitboxTransform.lossyScale.y),
                1f
            );

            Gizmos.DrawWireCube(
                Vector3.zero,
                scaledSize
            );
        }
    }
}

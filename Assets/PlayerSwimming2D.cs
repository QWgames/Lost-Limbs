
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Animations;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(LLPlayerController))]
public class PlayerSwimming2D : MonoBehaviour
{
    [Header("Swimming Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip swimmingAnimation;
    [SerializeField] private Transform visualToFlip;

    [Header("Swimming Movement")]
    [SerializeField, Min(0f)] private float swimAcceleration = 12f;
    [SerializeField, Min(0f)] private float maximumSwimSpeed = 4f;
    [SerializeField, Min(0f)] private float waterResistance = 3f;

    [Header("Buoyancy")]
    [SerializeField, Min(0f)] private float swimmingGravityScale = 0.35f;
    [SerializeField, Min(0f)] private float buoyancyForce = 2f;

    [Header("Water Entry")]
    [SerializeField, Range(0f, 1f)]
    private float entryVelocityMultiplier = 0.6f;

    private Rigidbody2D rb;
    private LLPlayerController playerController;

    private float originalGravityScale;
    private Vector3 originalVisualScale;
    private bool isSwimming;
    private int overlappingWaterZones;
    private int facingDirection = 1;

    private PlayableGraph swimmingGraph;

    public bool IsSwimming => isSwimming;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<LLPlayerController>();

        originalGravityScale = rb.gravityScale;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (visualToFlip != null)
            originalVisualScale = visualToFlip.localScale;
    }

    private void Update()
    {
        if (!isSwimming)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        // Remember the last horizontal direction pressed.
        if (keyboard.aKey.isPressed)
            SetFacingDirection(-1);
        else if (keyboard.dKey.isPressed)
            SetFacingDirection(1);
    }

    private void FixedUpdate()
    {
        if (!isSwimming || playerController.ExternalMovementControl)
            return;

        Keyboard keyboard = Keyboard.current;
        Vector2 input = Vector2.zero;

        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed) input.x -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
        }

        if (input.x < 0f)
            SetFacingDirection(-1);
        else if (input.x > 0f)
            SetFacingDirection(1);

        if (input.sqrMagnitude > 1f)
            input.Normalize();

        Vector2 velocity = rb.linearVelocity;
        velocity += input * swimAcceleration * Time.fixedDeltaTime;

        // Slow down when no movement key is held.
        if (input.sqrMagnitude < 0.01f)
        {
            velocity = Vector2.MoveTowards(
                velocity,
                Vector2.zero,
                waterResistance * Time.fixedDeltaTime
            );

            // Gentle upward buoyancy when not actively moving vertically.
            velocity.y += buoyancyForce * Time.fixedDeltaTime;
        }
        else
        {
            velocity = Vector2.MoveTowards(
                velocity,
                velocity * 0.98f,
                waterResistance * 0.15f * Time.fixedDeltaTime
            );
        }

        velocity = Vector2.ClampMagnitude(velocity, maximumSwimSpeed);
        rb.linearVelocity = velocity;
    }

    private void SetFacingDirection(int direction)
    {
        facingDirection = direction;

        if (visualToFlip == null)
            return;

        Vector3 scale = originalVisualScale;
        scale.x = Mathf.Abs(originalVisualScale.x) * facingDirection;
        visualToFlip.localScale = scale;
    }

    private void StartSwimmingAnimation()
    {
        if (animator == null || swimmingAnimation == null)
            return;

        StopSwimmingAnimation();

        swimmingGraph = PlayableGraph.Create("Swimming Animation");
        swimmingGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        AnimationPlayableOutput output =
            AnimationPlayableOutput.Create(
                swimmingGraph,
                "Swimming Output",
                animator
            );

        AnimationClipPlayable clipPlayable =
            AnimationClipPlayable.Create(swimmingGraph, swimmingAnimation);

        output.SetSourcePlayable(clipPlayable);
        swimmingGraph.Play();
    }

    private void StopSwimmingAnimation()
    {
        if (swimmingGraph.IsValid())
            swimmingGraph.Destroy();
    }

    public void EnterWater()
    {
        overlappingWaterZones++;

        if (isSwimming)
            return;

        isSwimming = true;
        playerController.SwimmingMovementControl = true;

        rb.gravityScale = swimmingGravityScale;
        rb.linearVelocity *= entryVelocityMultiplier;

        StartSwimmingAnimation();
    }

    public void ExitWater()
    {
        overlappingWaterZones = Mathf.Max(0, overlappingWaterZones - 1);

        if (overlappingWaterZones > 0 || !isSwimming)
            return;

        isSwimming = false;
        playerController.SwimmingMovementControl = false;

        rb.gravityScale = originalGravityScale;

        StopSwimmingAnimation();
        if (animator != null)
            animator.Update(0f);
    }

    private void OnDisable()
    {
        StopSwimmingAnimation();

        if (rb != null)
            rb.gravityScale = originalGravityScale;

        if (playerController != null)
            playerController.SwimmingMovementControl = false;

        isSwimming = false;
        overlappingWaterZones = 0;
    }
}

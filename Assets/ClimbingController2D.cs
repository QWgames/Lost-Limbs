
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class ClimbingController2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D playerBody;
    [SerializeField] private LLPlayerController playerController;
    [SerializeField] private MouseArmIK armIK;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private LineRenderer aimLine;

    [Header("Grabbing")]
    [SerializeField, Min(0.01f)] private float grabRadius = 0.18f;
    [SerializeField, Min(0.1f)] private float maxGrabDistance = 1.8f;
    [SerializeField] private LayerMask climbableLayers;
    [SerializeField, Min(0.05f)] private float minimumTetherLength = 0.3f;
    [SerializeField, Min(0f)] private float regrabDelay = 0.12f;

    [Header("Launch")]
    [SerializeField, Min(0f)] private float launchSpeed = 9f;
    [SerializeField, Min(0f)] private float inheritedMomentum = 0.65f;
    [SerializeField, Min(1f)] private float maximumSpeed = 16f;

    [Tooltip("How long normal movement stays disabled after launching.")]
    [SerializeField, Min(0f)] private float launchControlDuration = 0.3f;

    [Header("Aim")]
    [SerializeField, Min(0.5f)] private float aimLineLength = 2f;

    private DistanceJoint2D grabJoint;
    private Vector2 grabAnchor;
    private Vector2 aimDirection = Vector2.up;
    private float nextGrabTime;
    private float launchControlUntil;
    private bool isGrabbing;

    public bool IsGrabbing => isGrabbing;

    private void Awake()
    {
        if (playerBody == null)
            playerBody = GetComponent<Rigidbody2D>();

        if (playerController == null)
            playerController = GetComponent<LLPlayerController>();

        if (armIK == null)
            armIK = GetComponentInChildren<MouseArmIK>();

        if (gameCamera == null)
            gameCamera = Camera.main;

        if (aimLine != null)
        {
            aimLine.enabled = false;
            aimLine.positionCount = 2;
            aimLine.useWorldSpace = true;
        }
    }

    private void Update()
    {
        // Keep player input disabled briefly after a launch.
        // This lets launch momentum survive the next physics updates.
        if (!isGrabbing && playerController != null)
        {
            if (Time.time < launchControlUntil)
            {
                playerController.ExternalMovementControl = true;
            }
            else if (launchControlUntil > 0f)
            {
                launchControlUntil = 0f;
                playerController.ExternalMovementControl = false;
            }
        }

        if (Mouse.current == null || gameCamera == null)
            return;

        Vector2 mouseWorld = GetMouseWorldPosition();

        if (!isGrabbing)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
                TryGrab(mouseWorld);

            return;
        }

        // Keep the hand attached to the grab point.
        if (armIK != null)
            armIK.SetExternalHandTarget(grabAnchor);

        // Aim from the player toward the mouse.
        Vector2 toMouse = mouseWorld - playerBody.position;

        if (toMouse.sqrMagnitude > 0.001f)
            aimDirection = toMouse.normalized;

        UpdateAimLine();

        // Left-click again to launch.
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Launch();
            return;
        }

        // Right-click releases without launching.
        if (Mouse.current.rightButton.wasPressedThisFrame)
            ReleaseGrip();
    }

    private void TryGrab(Vector2 mouseWorld)
    {
        if (Time.time < nextGrabTime)
            return;

        Collider2D surface = Physics2D.OverlapCircle(
            mouseWorld,
            grabRadius,
            climbableLayers
        );

        if (surface == null)
            return;

        if (Vector2.Distance(playerBody.position, mouseWorld)
            > maxGrabDistance)
            return;

        grabAnchor = surface.ClosestPoint(mouseWorld);

        // Create a physical tether at the selected point.
        grabJoint = playerBody.gameObject.AddComponent<DistanceJoint2D>();
        grabJoint.autoConfigureConnectedAnchor = false;
        grabJoint.connectedBody = null;
        grabJoint.connectedAnchor = grabAnchor;
        grabJoint.autoConfigureDistance = false;
        grabJoint.maxDistanceOnly = false;
        grabJoint.enableCollision = true;

        grabJoint.anchor = Vector2.zero;
        grabJoint.distance = Mathf.Max(
            minimumTetherLength,
            Vector2.Distance(playerBody.position, grabAnchor)
        );

        isGrabbing = true;
        launchControlUntil = 0f;

        if (playerController != null)
            playerController.ExternalMovementControl = true;

        if (armIK != null)
            armIK.SetExternalHandTarget(grabAnchor);

        Vector2 initialAim = mouseWorld - playerBody.position;

        if (initialAim.sqrMagnitude > 0.001f)
            aimDirection = initialAim.normalized;

        UpdateAimLine();
    }

    private void Launch()
    {
        Vector2 direction = aimDirection.normalized;

        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.up;

        // Combine the chosen launch direction with existing momentum.
        Vector2 launchVelocity =
            direction * launchSpeed
            + playerBody.linearVelocity * inheritedMomentum;

        // Release the tether but retain external movement control.
        ReleaseGrip(true);

        // Apply the launch velocity after releasing the tether.
        playerBody.linearVelocity = Vector2.ClampMagnitude(
            launchVelocity,
            maximumSpeed
        );

        // Give the launch a short period without walking input
        // overwriting its horizontal velocity.
        launchControlUntil = Time.time + launchControlDuration;

        if (playerController != null)
            playerController.ExternalMovementControl = true;
    }

    private void ReleaseGrip(bool keepMovementControl = false)
    {
        if (grabJoint != null)
        {
            Destroy(grabJoint);
            grabJoint = null;
        }

        isGrabbing = false;
        nextGrabTime = Time.time + regrabDelay;

        if (playerController != null && !keepMovementControl)
            playerController.ExternalMovementControl = false;

        if (armIK != null)
            armIK.ClearExternalHandTarget();

        if (aimLine != null)
            aimLine.enabled = false;
    }

    private Vector2 GetMouseWorldPosition()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();

        Vector3 screenPosition = new Vector3(
            mouseScreen.x,
            mouseScreen.y,
            -gameCamera.transform.position.z
        );

        return gameCamera.ScreenToWorldPoint(screenPosition);
    }

    private void UpdateAimLine()
    {
        if (aimLine == null)
            return;

        aimLine.enabled = isGrabbing;

        if (!isGrabbing)
            return;

        Vector2 start = playerBody.position;
        Vector2 end = start + aimDirection * aimLineLength;

        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, end);
    }

    private void OnDisable()
    {
        if (grabJoint != null)
        {
            Destroy(grabJoint);
            grabJoint = null;
        }

        isGrabbing = false;
        launchControlUntil = 0f;

        if (playerController != null)
            playerController.ExternalMovementControl = false;

        if (armIK != null)
            armIK.ClearExternalHandTarget();

        if (aimLine != null)
            aimLine.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxGrabDistance);

        if (isGrabbing)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(grabAnchor, grabRadius);
        }
    }
}

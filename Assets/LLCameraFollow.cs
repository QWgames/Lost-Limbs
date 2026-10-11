using UnityEngine;

public class LLCameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Camera Settings")]
    public float offsetY = 0f;
    public float baseSmoothTime = 0.12f;
    public float maxSmoothTime = 0.4f;
    public float speedForMaxLag = 6f;

    [Header("Mouse Influence")]
    [Range(0f, 1f)]
    public float mouseInfluence = 0.2f;

    public float maxMouseOffset = 1.5f;

    private Vector3 velocity = Vector3.zero;
    private Rigidbody2D playerRb;

    void Start()
    {
        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody2D>();
        }
    }

    void LateUpdate()
    {
        if (player == null)
            return;

        // Base target: player position
        Vector3 targetPosition = new Vector3(
            player.position.x,
            player.position.y + offsetY,
            transform.position.z
        );

        // Calculate mouse position in world space
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(
            Input.mousePosition
        );

        // Calculate direction from player to mouse
        Vector2 mouseDirection = new Vector2(
            mousePosition.x - player.position.x,
            mousePosition.y - player.position.y
        );

        // Limit mouse influence to a maximum distance
        Vector2 mouseOffset = Vector2.ClampMagnitude(
            mouseDirection * mouseInfluence,
            maxMouseOffset
        );

        // Add subtle mouse offset to camera target
        targetPosition.x += mouseOffset.x;
        targetPosition.y += mouseOffset.y;

        // Get player speed
        float playerSpeed = 0f;

        if (playerRb != null)
        {
            playerSpeed = playerRb.linearVelocity.magnitude;
        }

        // Increase cinematic lag as player speed increases
        float speedPercent = Mathf.Clamp01(
            playerSpeed / speedForMaxLag
        );

        float smoothTime = Mathf.Lerp(
            baseSmoothTime,
            maxSmoothTime,
            speedPercent
        );

        // Smoothly follow the combined target
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );
    }
}

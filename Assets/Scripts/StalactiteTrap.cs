
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class StalactiteTrap : MonoBehaviour
{
    [Header("Detection Range")]
    [SerializeField] private BoxCollider2D detectionZone;

    [Header("Warning Rumble")]
    [SerializeField, Min(0f)] private float warningDuration = 1f;
    [SerializeField, Min(0f)] private float shakeAmount = 0.06f;
    [SerializeField, Min(1f)] private float shakeSpeed = 30f;
    [SerializeField] private Transform visualTransform;

    [Header("Falling")]
    [SerializeField, Min(0f)] private float fallGravityScale = 3f;
    [SerializeField] private bool freezeRotation = true;

    [Header("Shattering")]
    [SerializeField] private GameObject shatterEffect;
    [SerializeField, Min(0f)] private float destroyDelay = 0.05f;

    private Rigidbody2D rb;
    private Vector3 originalVisualPosition;

    private bool triggered;
    private bool hasImpacted;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Keep the stalactite stationary until triggered.
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = freezeRotation;
        rb.collisionDetectionMode =
            CollisionDetectionMode2D.Continuous;

        if (visualTransform != null)
            originalVisualPosition = visualTransform.localPosition;

        if (detectionZone != null)
            detectionZone.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTrigger(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Also detects the player if already inside the zone.
        TryTrigger(other);
    }

    private void TryTrigger(Collider2D other)
    {
        if (triggered || hasImpacted)
            return;

        LLPlayerController player =
            other.GetComponentInParent<LLPlayerController>();

        if (player == null)
            return;

        triggered = true;

        // Prevent additional detection while the stalactite is falling.
        if (detectionZone != null)
            detectionZone.enabled = false;

        StartCoroutine(RumbleThenFall());
    }

    private IEnumerator RumbleThenFall()
    {
        float elapsed = 0f;

        while (elapsed < warningDuration)
        {
            if (visualTransform != null)
            {
                float offsetX =
                    Mathf.Sin(elapsed * shakeSpeed) * shakeAmount;

                float offsetY =
                    Mathf.Cos(elapsed * shakeSpeed * 1.3f)
                    * shakeAmount * 0.5f;

                visualTransform.localPosition =
                    originalVisualPosition +
                    new Vector3(offsetX, offsetY, 0f);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset the visual before the fall begins.
        if (visualTransform != null)
            visualTransform.localPosition = originalVisualPosition;

        // Activate gravity so the stalactite drops.
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallGravityScale;
        rb.linearVelocity = Vector2.zero;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Prevent multiple impacts and duplicate effects.
        if (hasImpacted)
            return;

        hasImpacted = true;

        // Remove the player's legs if the stalactite hits them.
        LLPlayerController player =
            collision.collider.GetComponentInParent<LLPlayerController>();

        if (player != null)
            player.SetLegsEnabled(false);

        // Stop movement before destroying the stalactite.
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Static;

        // Spawn the optional shatter effect.
        if (shatterEffect != null)
        {
            Instantiate(
                shatterEffect,
                transform.position,
                transform.rotation
            );
        }

        // Destroy the entire stalactite object.
        Destroy(gameObject, destroyDelay);
    }

    private void OnDisable()
    {
        if (visualTransform != null)
            visualTransform.localPosition = originalVisualPosition;
    }

    private void OnDrawGizmosSelected()
    {
        if (detectionZone == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.matrix = detectionZone.transform.localToWorldMatrix;

        Gizmos.DrawWireCube(
            detectionZone.offset,
            detectionZone.size
        );
    }
}

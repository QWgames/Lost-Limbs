
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
    [SerializeField, Min(0.1f)] private float maxFallDuration = 3f;

    [Header("Shattering")]
    [SerializeField] private GameObject shatterEffect;
    [SerializeField, Min(0f)] private float destroyDelay = 0.05f;

    private Rigidbody2D rb;
    private Vector3 originalVisualPosition;

    private bool triggered;
    private bool hasImpacted;
    private float fallTimer;

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

        if (visualTransform != null)
            visualTransform.localPosition = originalVisualPosition;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallGravityScale;
        rb.linearVelocity = Vector2.zero;

        // Begin timing only once the stalactite starts falling.
        fallTimer = 0f;
    }



    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasImpacted)
            return;

        LLPlayerController player =
            collision.collider.GetComponentInParent<LLPlayerController>();

        if (player == null && collision.otherCollider != null)
        {
            player = collision.otherCollider
                .GetComponentInParent<LLPlayerController>();
        }

        // Ignore collisions that do not involve the player.
        if (player == null)
            return;

        hasImpacted = true;

        // Remove the player's legs on impact.
        player.SetLegsEnabled(false);

        Shatter();
    }

    private void Shatter()
    {
        if (hasImpacted == false)
            hasImpacted = true;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Static;

        if (shatterEffect != null)
        {
            Instantiate(
                shatterEffect,
                transform.position,
                transform.rotation
            );
        }

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

    private void Update()
    {
        // Only count time after the stalactite begins falling.
        if (hasImpacted || !triggered)
            return;

        if (rb.bodyType != RigidbodyType2D.Dynamic)
            return;

        fallTimer += Time.deltaTime;

        if (fallTimer >= maxFallDuration)
        {
            hasImpacted = true;
            Shatter();
        }
    }

}



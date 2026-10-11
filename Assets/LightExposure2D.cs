
using UnityEngine;

public class LightExposure2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InsanityEffect2D insanityEffect;

    [Header("Light Protection")]
    [SerializeField, Range(0f, 1f)]
    private float exposureStrength = 1f;

    private Collider2D zoneCollider;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();

        if (zoneCollider == null)
            Debug.LogError($"Light zone '{name}' needs a Collider2D.", this);

        if (zoneCollider != null && !zoneCollider.isTrigger)
            Debug.LogWarning(
                $"Light zone '{name}' should have Is Trigger enabled.", this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
            return;

        if (insanityEffect == null)
        {
            Debug.LogWarning(
                $"Assign the Canvas's InsanityEffect2D to light zone '{name}'.",
                this);
            return;
        }

        insanityEffect.RegisterLightZone(this, exposureStrength);
        Debug.Log($"Entered safe light zone: {name}", this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
            return;

        if (insanityEffect != null)
            insanityEffect.UnregisterLightZone(this);
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        // Check whether this collider belongs to the player.
        return other.GetComponentInParent<LLPlayerController>() != null;
    }
}

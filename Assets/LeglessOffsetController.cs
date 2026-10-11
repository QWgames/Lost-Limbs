using UnityEngine;

public class LeglessOffsetController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LLPlayerController playerController;
    [SerializeField] private Transform visualRoot;

    [Header("Legless Visual Offset")]
    [SerializeField] private float leglessOffsetX = 0f;
    [SerializeField] private float leglessOffsetY = 0f;

    // Tracks the offset applied by this component so it can
    // be removed before applying the next frame's offset.
    private Vector3 previouslyAppliedOffset;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<LLPlayerController>();

        if (visualRoot == null && playerController != null)
        {
            Debug.LogError(
                "LeglessOffsetController: Assign Visual Root " +
                "in the Inspector.",
                this
            );
        }

        if (playerController == null)
        {
            Debug.LogError(
                "LeglessOffsetController: Assign the Player Controller.",
                this
            );

            enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (playerController == null || visualRoot == null)
            return;

        // Remove our previous adjustment first.
        visualRoot.position -= previouslyAppliedOffset;
        previouslyAppliedOffset = Vector3.zero;

        // Leave the normal legged pose entirely to LLPlayerController.
        if (playerController.HasLegs)
            return;

        // Apply the offset in the Player's coordinate directions,
        // without changing the Player or its Rigidbody2D.
        Vector3 offset = transform.TransformVector(
            new Vector3(leglessOffsetX, leglessOffsetY, 0f)
        );

        visualRoot.position += offset;
        previouslyAppliedOffset = offset;
    }

    private void OnDisable()
    {
        if (visualRoot != null && previouslyAppliedOffset != Vector3.zero)
        {
            visualRoot.position -= previouslyAppliedOffset;
            previouslyAppliedOffset = Vector3.zero;
        }
    }
}
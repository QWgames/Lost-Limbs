using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerTriggeredLight : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] private Light2D targetLight;

    [Header("Flicker Settings")]
    [SerializeField] private float flickerDuration = 0.6f;
    [SerializeField] private float flickerInterval = 0.06f;

    private bool hasTriggered;

    private void Awake()
    {
        if (targetLight == null)
            targetLight = GetComponentInChildren<Light2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered)
            return;

        if (other.GetComponentInParent<LLPlayerController>() == null)
            return;

        hasTriggered = true;
        StartCoroutine(FlickerOut());
    }

    private IEnumerator FlickerOut()
    {
        float elapsed = 0f;

        while (elapsed < flickerDuration)
        {
            if (targetLight != null)
                targetLight.enabled = !targetLight.enabled;

            yield return new WaitForSeconds(flickerInterval);
            elapsed += flickerInterval;
        }

        if (targetLight != null)
            targetLight.enabled = false;
    }
}


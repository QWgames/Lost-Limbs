
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InsanityEffect2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image screenOverlay;
    [SerializeField] private HeadlampBattery2D headlamp;

    [Header("Insanity")]
    [SerializeField, Range(0f, 1f)] private float startingInsanity;
    [SerializeField, Min(0f)] private float darknessBuildRate = 0.12f;
    [SerializeField, Min(0f)] private float lightRecoveryRate = 0.3f;
    [SerializeField, Range(0f, 1f)] private float maximumInsanityAlpha = 0.8f;

    [Header("Effect Appearance")]
    [SerializeField] private Color insanityColor = new Color(0.3f, 0.02f, 0.12f, 1f);
    [SerializeField, Range(0f, 1f)] private float noiseStrength = 0.12f;
    [SerializeField, Min(64)] private int textureSize = 256;

    private readonly Dictionary<LightExposure2D, float> lightZones =
        new Dictionary<LightExposure2D, float>();

    private float insanity;
    private Texture2D overlayTexture;
    private Sprite overlaySprite;

    public float InsanityNormalized => insanity;

    private void Start()
    {
        insanity = startingInsanity;

        if (screenOverlay != null)
        {
            CreateOverlayTexture();
            screenOverlay.sprite = overlaySprite;
            screenOverlay.color = new Color(1f, 1f, 1f, 0f);
            screenOverlay.raycastTarget = false;
            screenOverlay.preserveAspect = false;
        }
    }

    private void Update()
    {
        float exposure = 0f;

        // The headlamp provides full protection while it is on.
        if (headlamp != null && headlamp.IsOn)
            exposure = 1f;

        // Environmental light zones provide partial or full protection.
        foreach (float strength in lightZones.Values)
            exposure = Mathf.Max(exposure, strength);

        float targetInsanity = 1f - exposure;
        float rate = targetInsanity > insanity
            ? darknessBuildRate
            : lightRecoveryRate;

        insanity = Mathf.MoveTowards(
            insanity,
            targetInsanity,
            rate * Time.deltaTime
        );

        UpdateOverlay();
    }

    public void RegisterLightZone(LightExposure2D zone, float strength)
    {
        if (zone != null)
            lightZones[zone] = Mathf.Clamp01(strength);
    }

    public void UnregisterLightZone(LightExposure2D zone)
    {
        if (zone != null)
            lightZones.Remove(zone);
    }

    private void UpdateOverlay()
    {
        if (screenOverlay == null)
            return;

        Color tint = Color.white;
        tint.a = insanity * maximumInsanityAlpha;
        screenOverlay.color = tint;
    }

    private void CreateOverlayTexture()
    {
        int size = Mathf.Max(64, textureSize);
        overlayTexture = new Texture2D(
            size, size, TextureFormat.RGBA32, false
        );

        overlayTexture.wrapMode = TextureWrapMode.Clamp;
        overlayTexture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (float)(size - 1)) * 2f - 1f;
                float ny = (y / (float)(size - 1)) * 2f - 1f;

                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                float vignette = Mathf.SmoothStep(0.25f, 1.15f, distance);
                float noise = Random.Range(1f - noiseStrength, 1f);

                float alpha = Mathf.Clamp01(vignette * noise);

                pixels[y * size + x] = new Color(
                    insanityColor.r,
                    insanityColor.g,
                    insanityColor.b,
                    alpha
                );
            }
        }

        overlayTexture.SetPixels(pixels);
        overlayTexture.Apply();

        overlaySprite = Sprite.Create(
            overlayTexture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f)
        );
    }

    private void OnDestroy()
    {
        if (overlaySprite != null)
            Destroy(overlaySprite);

        if (overlayTexture != null)
            Destroy(overlayTexture);
    }
}

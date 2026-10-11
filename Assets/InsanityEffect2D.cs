
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InsanityEffect2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image screenOverlay;
    [SerializeField] private HeadlampBattery2D headlamp;

    [Header("Insanity")]
    [SerializeField, Range(0f, 1f)] private float startingInsanity = 0f;
    [SerializeField, Min(0f)] private float darknessBuildRate = 0.12f;
    [SerializeField, Min(0f)] private float lightRecoveryRate = 0.3f;
    [SerializeField, Range(0f, 1f)] private float maximumInsanityAlpha = 0.9f;

    [Header("Dark Vignette")]
    [SerializeField]
    private Color insanityColor =
        new Color(0.005f, 0.004f, 0.012f, 1f);
    [SerializeField, Range(0f, 1f)] private float vignetteStrength = 0.95f;
    [SerializeField, Min(64)] private int textureSize = 256;

    [Header("Random Insanity Eyes")]
    [Tooltip("One transparent sprite containing a pair of eyes.")]
    [SerializeField] private Sprite eyeSprite;
    [SerializeField, Min(1)] private int maximumVisibleEyes = 5;
    [SerializeField, Range(0f, 1f)] private float eyesAppearAt = 0.2f;
    [SerializeField, Range(0f, 1f)] private float eyesFullyVisibleAt = 0.85f;
    [SerializeField, Range(0f, 1f)] private float maximumEyeAlpha = 0.85f;
    [SerializeField, Min(0.1f)] private float eyeFadeSpeed = 2f;
    [SerializeField, Min(0.1f)] private float minimumEyeLifetime = 1.5f;
    [SerializeField, Min(0.1f)] private float maximumEyeLifetime = 4f;
    [SerializeField, Min(0.1f)] private float minimumSpawnInterval = 0.5f;
    [SerializeField, Min(0.1f)] private float maximumSpawnInterval = 2f;
    [SerializeField] private Vector2 eyeSize = new Vector2(110f, 55f);
    [SerializeField, Range(0f, 0.3f)] private float screenEdgeMargin = 0.08f;

    private readonly Dictionary<LightExposure2D, float> lightZones =
        new Dictionary<LightExposure2D, float>();

    private class EyeInstance
    {
        public RectTransform rect;
        public Image image;
        public float alpha;
        public float lifetime;
        public float age;
        public float fadeInDuration;
        public float fadeOutDuration;
        public float blinkTimer;
        public float blinkDuration;
        public float driftSeed;
        public Vector2 basePosition;
    }

    private readonly List<EyeInstance> spawnedEyes =
        new List<EyeInstance>();

    private float insanity;
    private Texture2D overlayTexture;
    private Sprite overlaySprite;
    private RectTransform overlayParent;
    private float nextEyeSpawnTime;

    public float InsanityNormalized => insanity;

    private void Start()
    {
        insanity = startingInsanity;

        if (screenOverlay != null)
        {
            CreateOverlayTexture();

            screenOverlay.sprite = overlaySprite;
            screenOverlay.color = Color.white;
            screenOverlay.raycastTarget = false;
            screenOverlay.preserveAspect = false;

            overlayParent = screenOverlay.rectTransform.parent as RectTransform;
        }

        if (overlayParent == null)
        {
            Debug.LogWarning(
                "InsanityEffect2D: Screen Overlay needs a parent RectTransform.",
                this
            );
        }

        nextEyeSpawnTime = Time.time + Random.Range(1f, 3f);
    }

    private void Update()
    {
        float exposure = 0f;

        if (headlamp != null && headlamp.IsOn)
            exposure = 1f;

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
        UpdateEyes();
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
                float vignette = Mathf.SmoothStep(0.2f, 1.2f, distance);

                pixels[y * size + x] = new Color(
                    insanityColor.r,
                    insanityColor.g,
                    insanityColor.b,
                    vignette * vignetteStrength
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

    private void UpdateOverlay()
    {
        if (screenOverlay == null)
            return;

        Color color = Color.white;
        color.a = insanity * maximumInsanityAlpha;
        screenOverlay.color = color;
    }

    private void UpdateEyes()
    {
        float visibility = Mathf.InverseLerp(
            eyesAppearAt,
            eyesFullyVisibleAt,
            insanity
        );

        if (visibility <= 0f)
        {
            for (int i = spawnedEyes.Count - 1; i >= 0; i--)
            {
                EyeInstance eye = spawnedEyes[i];

                eye.alpha = Mathf.MoveTowards(
                    eye.alpha,
                    0f,
                    eyeFadeSpeed * Time.deltaTime
                );

                SetEyeAlpha(eye, eye.alpha);

                if (eye.alpha <= 0.01f)
                    RemoveEyeAt(i);
            }

            return;
        }

        if (eyeSprite != null &&
            overlayParent != null &&
            spawnedEyes.Count < maximumVisibleEyes &&
            Time.time >= nextEyeSpawnTime)
        {
            SpawnEye();

            float interval = Mathf.Lerp(
                maximumSpawnInterval,
                minimumSpawnInterval,
                visibility
            );

            nextEyeSpawnTime = Time.time + Random.Range(
                interval * 0.65f,
                interval * 1.35f
            );
        }

        for (int i = spawnedEyes.Count - 1; i >= 0; i--)
        {
            EyeInstance eye = spawnedEyes[i];
            eye.age += Time.deltaTime;

            float targetAlpha;

            if (eye.age < eye.fadeInDuration)
            {
                targetAlpha = maximumEyeAlpha *
                    Mathf.Clamp01(eye.age / eye.fadeInDuration);
            }
            else if (eye.age < eye.lifetime - eye.fadeOutDuration)
            {
                targetAlpha = maximumEyeAlpha;
            }
            else
            {
                float fadeProgress = Mathf.InverseLerp(
                    eye.lifetime,
                    eye.lifetime - eye.fadeOutDuration,
                    eye.age
                );

                targetAlpha = maximumEyeAlpha *
                    Mathf.Clamp01(fadeProgress);
            }

            targetAlpha *= visibility;

            eye.blinkTimer -= Time.deltaTime;

            if (eye.blinkTimer <= 0f)
            {
                eye.blinkDuration = 0.12f;
                eye.blinkTimer = Random.Range(1.5f, 5f);
            }

            if (eye.blinkDuration > 0f)
            {
                eye.blinkDuration -= Time.deltaTime;
                targetAlpha *= 0.08f;
            }

            eye.alpha = Mathf.MoveTowards(
                eye.alpha,
                targetAlpha,
                eyeFadeSpeed * Time.deltaTime
            );

            SetEyeAlpha(eye, eye.alpha);

            float t = Time.time * 0.7f + eye.driftSeed;

            Vector2 drift = new Vector2(
                Mathf.Sin(t),
                Mathf.Cos(t * 0.8f)
            ) * 8f * visibility;

            eye.rect.anchoredPosition = eye.basePosition + drift;

            if (eye.age >= eye.lifetime && eye.alpha <= 0.01f)
                RemoveEyeAt(i);
        }
    }

    private void SpawnEye()
    {
        if (eyeSprite == null || overlayParent == null)
            return;

        GameObject eyeObject = new GameObject(
            "InsanityEye",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        eyeObject.transform.SetParent(overlayParent, false);

        RectTransform rect = eyeObject.GetComponent<RectTransform>();
        rect.sizeDelta = eyeSize;

        float width = overlayParent.rect.width;
        float height = overlayParent.rect.height;
        float marginX = width * screenEdgeMargin;
        float marginY = height * screenEdgeMargin;

        Vector2 position;

        switch (Random.Range(0, 4))
        {
            case 0:
                position = new Vector2(
                    Random.Range(-width * 0.4f, width * 0.4f),
                    height * 0.5f - marginY
                );
                break;

            case 1:
                position = new Vector2(
                    Random.Range(-width * 0.4f, width * 0.4f),
                    -height * 0.5f + marginY
                );
                break;

            case 2:
                position = new Vector2(
                    -width * 0.5f + marginX,
                    Random.Range(-height * 0.4f, height * 0.4f)
                );
                break;

            default:
                position = new Vector2(
                    width * 0.5f - marginX,
                    Random.Range(-height * 0.4f, height * 0.4f)
                );
                break;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;

        rect.localRotation = Quaternion.Euler(
            0f, 0f, Random.Range(-20f, 20f)
        );

        Image image = eyeObject.GetComponent<Image>();
        image.sprite = eyeSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = new Color(1f, 1f, 1f, 0f);

        eyeObject.transform.SetAsLastSibling();

        float lifetime = Random.Range(
            minimumEyeLifetime,
            Mathf.Max(minimumEyeLifetime, maximumEyeLifetime)
        );

        spawnedEyes.Add(new EyeInstance
        {
            rect = rect,
            image = image,
            alpha = 0f,
            lifetime = lifetime,
            age = 0f,
            fadeInDuration = Mathf.Min(0.5f, lifetime * 0.3f),
            fadeOutDuration = Mathf.Min(0.8f, lifetime * 0.35f),
            blinkTimer = Random.Range(1.5f, 5f),
            blinkDuration = 0f,
            driftSeed = Random.Range(0f, 100f),
            basePosition = position
        });
    }

    private void SetEyeAlpha(EyeInstance eye, float alpha)
    {
        if (eye.image == null)
            return;

        Color color = eye.image.color;
        color.a = alpha;
        eye.image.color = color;
    }

    private void RemoveEyeAt(int index)
    {
        if (spawnedEyes[index].rect != null)
            Destroy(spawnedEyes[index].rect.gameObject);

        spawnedEyes.RemoveAt(index);
    }

    private void OnDisable()
    {
        ClearEyes();
    }

    private void OnDestroy()
    {
        ClearEyes();

        if (overlaySprite != null)
            Destroy(overlaySprite);

        if (overlayTexture != null)
            Destroy(overlayTexture);
    }

    private void ClearEyes()
    {
        foreach (EyeInstance eye in spawnedEyes)
        {
            if (eye.rect != null)
                Destroy(eye.rect.gameObject);
        }

        spawnedEyes.Clear();
    }
}

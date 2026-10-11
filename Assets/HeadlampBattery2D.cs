
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class HeadlampBattery2D : MonoBehaviour
{
    [Header("Headlamp Lights")]
    [SerializeField] private Light2D[] headlampLights;

    [Header("Battery")]
    [SerializeField, Min(1f)] private float batteryLife = 60f;
    [SerializeField, Min(1f)] private float rechargeTime = 12f;
    [SerializeField, Min(0f)] private float toggleRechargeDelay = 0.5f;

    [Header("Flicker")]
    [SerializeField, Min(0f)] private float normalFlicker = 0.08f;
    [SerializeField, Min(0f)] private float lowBatteryFlicker = 0.35f;
    [SerializeField, Min(0f)] private float shakeAmount = 0.025f;
    [SerializeField, Min(0f)] private float shakeSpeed = 18f;

    [Header("Controls")]
    [SerializeField] private Key toggleKey = Key.F;

    private float battery;
    private float rechargeDelayTimer;
    private bool isOn;
    private bool depleted;
    private float[] originalIntensities;
    private Vector3[] originalPositions;

    public float BatteryNormalized => batteryLife > 0f
        ? battery / batteryLife
        : 0f;

    public bool IsOn => isOn;
    public bool IsDepleted => depleted;

    private void Awake()
    {
        battery = batteryLife;

        if (headlampLights == null)
            headlampLights = new Light2D[0];

        originalIntensities = new float[headlampLights.Length];
        originalPositions = new Vector3[headlampLights.Length];

        for (int i = 0; i < headlampLights.Length; i++)
        {
            if (headlampLights[i] == null)
                continue;

            originalIntensities[i] = headlampLights[i].intensity;
            originalPositions[i] = headlampLights[i].transform.localPosition;
        }

        SetLightsEnabled(false);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (Keyboard.current != null &&
            Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            ToggleLamp();
        }

        if (isOn)
        {
            battery -= dt;

            if (battery <= 0f)
            {
                battery = 0f;
                depleted = true;
                isOn = false;
                rechargeDelayTimer = 0f;
                SetLightsEnabled(false);
            }
        }
        else
        {
            if (rechargeDelayTimer > 0f)
                rechargeDelayTimer -= dt;

            // A depleted battery must recharge fully before reuse.
            // A manually switched-off lamp can be turned on anytime.
            if (depleted || rechargeDelayTimer <= 0f)
            {
                battery = Mathf.MoveTowards(
                    battery,
                    batteryLife,
                    batteryLife / rechargeTime * dt
                );

                if (depleted && battery >= batteryLife)
                    depleted = false;
            }
        }

        UpdateLightAnimation();
    }

    private void ToggleLamp()
    {
        if (isOn)
        {
            isOn = false;
            rechargeDelayTimer = toggleRechargeDelay;
            SetLightsEnabled(false);
            return;
        }

        if (depleted || battery <= 0f)
            return;

        isOn = true;
        SetLightsEnabled(true);
    }

    private void SetLightsEnabled(bool enabledState)
    {
        if (headlampLights == null)
            return;

        foreach (Light2D lamp in headlampLights)
        {
            if (lamp != null)
                lamp.enabled = enabledState;
        }
    }

    private void UpdateLightAnimation()
    {
        if (!isOn)
            return;

        float batteryLevel = 1f - BatteryNormalized;
        float flickerAmount = Mathf.Lerp(
            normalFlicker,
            lowBatteryFlicker,
            batteryLevel
        );

        // Smooth random movement makes the lamp feel handheld.
        float time = Time.time * shakeSpeed;
        Vector3 shake = new Vector3(
            Mathf.PerlinNoise(time, 0.2f) - 0.5f,
            Mathf.PerlinNoise(0.7f, time) - 0.5f,
            0f
        ) * shakeAmount;

        for (int i = 0; i < headlampLights.Length; i++)
        {
            Light2D lamp = headlampLights[i];

            if (lamp == null)
                continue;

            lamp.transform.localPosition = originalPositions[i] + shake;

            float noise = Mathf.PerlinNoise(time + i * 12.3f, 4.7f);
            float multiplier = 1f - flickerAmount * (1f - noise);

            // Occasional stronger flicker near empty.
            if (batteryLevel > 0.85f && Random.value < 0.04f)
                multiplier *= Random.Range(0.45f, 0.8f);

            lamp.intensity = originalIntensities[i] * multiplier;
        }
    }

    private void OnDisable()
    {
        isOn = false;
        SetLightsEnabled(false);
        RestoreLightTransforms();
    }

    private void RestoreLightTransforms()
    {
        if (headlampLights == null || originalPositions == null)
            return;

        for (int i = 0; i < headlampLights.Length; i++)
        {
            if (headlampLights[i] == null)
                continue;

            headlampLights[i].transform.localPosition = originalPositions[i];
            headlampLights[i].intensity = originalIntensities[i];
        }
    }
}



using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class LightGroupController : MonoBehaviour
{
    [Header("Target Lights")]
    public List<Light> targetLights = new List<Light>();

    [Header("Control Lights")]
    [Range(0f, 1f)] public float globalIntensity = 1f;
    public bool forceLightsOff = false;

    private Dictionary<Light, float> originalIntensities = new Dictionary<Light, float>();
    private bool isPlaying = false;

    void OnEnable()
    {
        // Prevent Initialize if Editor
        if (!Application.isPlaying) return;
        
        StoreOriginalIntensities();
        isPlaying = true;
    }

    void Update()
    {
        if (!isPlaying) return;
        UpdateLightGroup();
    }

    void OnDisable()
    {
        if (!Application.isPlaying) return;
        ResetIntensities();
        isPlaying = false;
    }

    void StoreOriginalIntensities()
    {
        originalIntensities.Clear();
        foreach (Light light in targetLights)
        {
            if (light != null)
            {
                originalIntensities[light] = light.intensity;
            }
        }
    }

    void UpdateLightGroup()
    {
        foreach (Light light in targetLights)
        {
            if (light == null) continue;

            float calculatedIntensity = originalIntensities[light] * globalIntensity;
            light.intensity = forceLightsOff ? 0 : calculatedIntensity;
            light.enabled = !forceLightsOff;
        }
    }

    void ResetIntensities()
    {
        foreach (var keyValuePair in originalIntensities)
        {
            if (keyValuePair.Key != null)
            {
                keyValuePair.Key.intensity = keyValuePair.Value;
                keyValuePair.Key.enabled = true;
            }
        }
    }
}

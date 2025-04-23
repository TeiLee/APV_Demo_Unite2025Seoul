using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

[ExecuteAlways]
public class TODManager : MonoBehaviour
{
    [System.Serializable]
    public struct EnvironmentLighting
    {
        public AmbientMode source;
        [ColorUsage(true, true)]
        public Color ambientColor;
    };

    [System.Serializable]
    public struct LightAnimationSettings
    {
        public Light light;
    }

    
    [System.Serializable]
    public struct TimeOfDay
    {
        public string name;
        public List<Volume> volumes;
    };

    //[Header("DebugToggles")]
    [HideInInspector]public bool enableAllTimeOfDayBlending = true;
    [HideInInspector]public bool enableAPVBlending = true;
    [HideInInspector]public bool enableReflectionProbeBlending = true;
    //[Space(20)]
    [Range(0.0f, 1.0f)]
    public float timeOfDay;
    private float _currentTimeOfDay;
    [Space(10)]
    //[Header("UpdateIntervals")]
    public float updateInterval = 0.1f;
    [HideInInspector]public float updateInterval_RealtimeReflectionProbe = 0.1f;
    
    [Header("Lights")]
    [SerializeField]
    private LightAnimationSettings sun;
    [SerializeField]
    private LightAnimationSettings moon;

    
    [Header("ReflectionProbes")]
    [SerializeField]
    private List<ReflectionProbe> reflectionProbes;


    UnityEngine.Rendering.ProbeReferenceVolume _probeRefVolume;

    [Header("LightScenarios")]
    public EnvironmentLighting environmentLighting;
    public Color fogColor;
    [SerializeField]
    private TimeOfDay scenario1;
    [SerializeField]
    private TimeOfDay scenario2;
    [Min(1)] public int numberOfCellsBlendedPerFrame = 1;
    
    private bool _isInitialized = false;
    private float _lastUpdateTime = 0;
    private float _lastReflectionProbeUpdateTime = 0;
    private float currentTime;

    void OnEnable()
    {
        Initialize();
        
    }

    void Initialize()
    {
        if (_isInitialized) return;
        
        _probeRefVolume = UnityEngine.Rendering.ProbeReferenceVolume.instance;
        _probeRefVolume.numberOfCellsBlendedPerFrame = numberOfCellsBlendedPerFrame;
        // Init all volumes
        foreach (var vol in scenario1.volumes) vol.weight = 1;
        foreach (var vol in scenario2.volumes) vol.weight = 0;       
        UpdateTimeOfDay(timeOfDay);
        foreach (ReflectionProbe probe in reflectionProbes)
            probe.RenderProbe();       
        
        _isInitialized = true;
    }

    void Update()
    {
        
        currentTime = Time.time;
        float intervalTime = currentTime - _lastUpdateTime;
        
        if (enableAllTimeOfDayBlending == true)
        {
            if (intervalTime >= updateInterval)
            {
                UpdateTime();
                _lastUpdateTime = currentTime;
            }
        }


        
        if (enableReflectionProbeBlending == true)
        {
            float intervalTimeReflectionProbe = currentTime - _lastReflectionProbeUpdateTime;
            
            if (intervalTimeReflectionProbe >= updateInterval_RealtimeReflectionProbe)
            {
                foreach (ReflectionProbe probe in reflectionProbes)
                    probe.RenderProbe();
                _lastReflectionProbeUpdateTime = currentTime;
            }
        }
        
        
        
    }

    void UpdateTime()
    {
        
        if (timeOfDay != _currentTimeOfDay)
        {
            _currentTimeOfDay = timeOfDay;
            UpdateTimeOfDay(timeOfDay);
        }
        
    }

    void UpdateTimeOfDay (float time)
    {
        time = Mathf.Clamp01(time);

        UpdateLensFlare(sun);
        UpdateLensFlare(moon);

        // 3step blending logic
        if (time <= 0.5f) // Dawn(0) → Day(0.5)
        {
            float blendFactor = time / 0.5f;
            for (int i = 0; i < scenario1.volumes.Count && i < scenario2.volumes.Count; i++)
            {
                scenario1.volumes[i].weight = 1 - blendFactor;
                scenario2.volumes[i].weight = blendFactor;
            }    
            
        }
        else // Day(0.5) → Night(1.0)
        {
            float blendFactor = (time - 0.5f) / 0.5f;

            for (int i = 0; i < scenario1.volumes.Count && i < scenario2.volumes.Count; i++)
            {
                scenario2.volumes[i].weight = 1 - blendFactor;
                scenario1.volumes[i].weight = blendFactor;
            }    
        }
        if (enableAPVBlending == true)
            UpdateAPV(time);

        RenderSettings.ambientLight = environmentLighting.ambientColor;
        RenderSettings.fogColor = fogColor;
    }

    
    void UpdateLensFlare(LightAnimationSettings lightAnimationSettings)
    {
        LensFlareComponentSRP lensFlare = lightAnimationSettings.light.GetComponent<LensFlareComponentSRP>();
        if (lensFlare)
            lensFlare.intensity = lightAnimationSettings.light.intensity;
    }

    void UpdateAPV(float time)
    {
        if (_probeRefVolume == null)
            _probeRefVolume = UnityEngine.Rendering.ProbeReferenceVolume.instance;


        // 3step blending logic
        if (time <= 0.5f) 
        {
            // Dawn -> Day blend (0.0~0.5)
            _probeRefVolume.lightingScenario = scenario1.name;
            float blendFactor = Mathf.Clamp01(time / 0.5f);
            _probeRefVolume.BlendLightingScenario(scenario2.name, blendFactor);
        }
        else 
        {
            // Day -> Night blend (0.5~1.0)
            _probeRefVolume.lightingScenario = scenario2.name;
            float blendFactor = Mathf.Clamp01((time - 0.5f) / 0.5f);
            _probeRefVolume.BlendLightingScenario(scenario1.name, blendFactor);
        }

        // safe edge value 
        if(time >= 1.0f) _probeRefVolume.lightingScenario = scenario1.name;
        if(time <= 0f) _probeRefVolume.lightingScenario = scenario1.name;
    }
}

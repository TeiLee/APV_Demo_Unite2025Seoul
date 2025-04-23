using UnityEngine;

//[ExecuteAlways]
public class SkyboxToCubemap : MonoBehaviour
{
    public Material skyboxMaterial;
    public RenderTexture cubemapRT;
    public float updateInterval = 0.1f;

    private Camera _cubemapCamera;
    private GameObject _camObj;
    private bool _isInitialized = false;
    private float _lastUpdateTime;

    void OnEnable()
    {
        InitializeResources();
    }

    void InitializeResources()
    {
        if (_isInitialized) return;

        _camObj = new GameObject("Cubemap Camera");
        _cubemapCamera = _camObj.AddComponent<Camera>();
        _cubemapCamera.enabled = false;
        _cubemapCamera.transform.position = transform.position;
        _cubemapCamera.farClipPlane = 1000f;
        _cubemapCamera.clearFlags = CameraClearFlags.Skybox;
        _cubemapCamera.cullingMask = 0;

        RenderSettings.skybox = skyboxMaterial;
        RenderSettings.customReflectionTexture = cubemapRT;

        _isInitialized = true;
    }
    
    void Update()
    {
        if (Time.time - _lastUpdateTime >= updateInterval)
        {
            UpdateCubemap();
            _lastUpdateTime = Time.time;
        }
    }
    
    void UpdateCubemap()
    {
            _cubemapCamera.RenderToCubemap(cubemapRT);
    }

    void OnDisable()
    {
        if (_camObj != null)
        {
            DestroyImmediate(_camObj);
            _isInitialized = false;
        }
    }
    
}
using UnityEngine;
using System.Collections.Generic;

//[ExecuteInEditMode]
//[ExecuteAlways]
public class TODManager_BlendCubemap : MonoBehaviour
{


    [Range(0.0f, 1.0f)]
    public float timeOfDay = 0.5f;
    public Cubemap cubeMap1;
    public Cubemap cubeMap2;
    public float updateInterval = 1.0f;
        
    public RenderTexture _blendedCubeRT; 
    private Material _blendMaterial;
    private bool _isInitialized;
    private float _lastUpdateTime;
    
    private static readonly Dictionary<CubemapFace, Vector3> _faceDirections = new()
    {
        { CubemapFace.PositiveX, Vector3.right },
        { CubemapFace.NegativeX, Vector3.left },
        { CubemapFace.PositiveY, Vector3.up },
        { CubemapFace.NegativeY, Vector3.down },
        { CubemapFace.PositiveZ, Vector3.forward },
        { CubemapFace.NegativeZ, Vector3.back }
    };
    void Initialize()
    {
        if(_isInitialized || cubeMap1 == null || cubeMap2 == null) return;

        _blendMaterial = new Material(Shader.Find("Hidden/CubemapBlender"));
        
        _blendMaterial.SetTexture("_Cube1", cubeMap1);
        _blendMaterial.SetTexture("_Cube2", cubeMap2);

        
        
        RenderSettings.customReflectionTexture = _blendedCubeRT;
        _isInitialized = true;
    }

    void UpdateCubemapBlend()
    {
        if(!_isInitialized) Initialize();
        if(_blendMaterial == null || _blendedCubeRT == null) return;

        
        _blendMaterial.SetFloat("_Blend", timeOfDay);
        
        

        foreach(CubemapFace face in _faceDirections.Keys)
        {
            _blendMaterial.SetVector("_FaceDirection", _faceDirections[face]);
            Graphics.SetRenderTarget(_blendedCubeRT, 0, face);
            Graphics.Blit(null, _blendMaterial, 0);
        }

        _blendedCubeRT.GenerateMips();
    }
    
    void Update()
    {
        if(Time.time - _lastUpdateTime >= updateInterval)
        {
            UpdateCubemapBlend();
            _lastUpdateTime = Time.time;
        }
    }

    void OnDisable()
    {
        if(_blendMaterial != null) DestroyImmediate(_blendMaterial,true);
        _isInitialized = false;
    }
    
}

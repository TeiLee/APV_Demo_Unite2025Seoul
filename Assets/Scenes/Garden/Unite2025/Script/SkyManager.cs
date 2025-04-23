using UnityEngine;
using System;


[ExecuteAlways]
public class SkyManager : MonoBehaviour
{
    //[Header("Donminant Lights")]
    public GameObject _Sun;
    public GameObject _Moon;
    
    private Vector3 _SunLightVector;
    private Vector3 _MoonLightVector;
    //[Header("Sky Color Gradient")]
    public Gradient _NightSkyGradient = new Gradient();
    public Gradient _SunupSkyGradient = new Gradient();
    public Gradient _DaySkyGradient = new Gradient();


    private Texture2D _rampTexture;
    private static readonly int SkyGradient = Shader.PropertyToID("_GradientA");

    
    private const int TEXTURE_WIDTH = 64; 
    private const int TEXTURE_HEIGHT = 12;
    private Color[] cols = new Color[TEXTURE_WIDTH * TEXTURE_HEIGHT]; 
    
    //[Header("Debug")]
    public bool showDebugTexture = false;
    [HideInInspector]public Texture2D debugTexture;
    
    

    void Start()
    {
        GenerateLUT();
    }

    void Update()
    {
    #if UNITY_EDITOR
            if (Application.isPlaying == false)
                GenerateLUT();
    #endif
        UpdateLightVectors();
    }

    void UpdateLightVectors()
    {
        if (_Sun != null)
        {
            _SunLightVector = -_Sun.transform.forward;
            Shader.SetGlobalVector("_SunLightVector", _SunLightVector);
        }
        
        if (_Moon != null)
        {
            _MoonLightVector = -_Moon.transform.forward;
            Shader.SetGlobalVector("_MoonLightVector", _MoonLightVector);
        }
    }
    private void OnEnable()
    {
        GenerateLUT();
    }

    private void OnDisable()
    {
        Shader.SetGlobalTexture(SkyGradient, null);
    }

    private void OnValidate()
    {
        if (showDebugTexture && debugTexture == null)
        {
            debugTexture = new Texture2D(TEXTURE_WIDTH, TEXTURE_WIDTH, TextureFormat.RGB24, false);
            debugTexture.filterMode = FilterMode.Bilinear;
        }
    
        //Display Resized(Square) LookUpTable , Component UI Only  
        if (showDebugTexture)
        {
            Color[] resizedCols = new Color[TEXTURE_WIDTH * TEXTURE_WIDTH];

            for (int y = 0; y < TEXTURE_WIDTH; y++)
            {
                for (int x = 0; x < TEXTURE_WIDTH; x++)
                {
                    float u = (float)x / (TEXTURE_WIDTH - 1);
                    float v = (float)y / (TEXTURE_WIDTH - 1);
                    float origX = u * (TEXTURE_WIDTH - 1);
                    float origY = v * (TEXTURE_HEIGHT - 1);
                    int x0 = Mathf.FloorToInt(origX);
                    int y0 = Mathf.FloorToInt(origY);
                    int x1 = Mathf.Min(x0 + 1, TEXTURE_WIDTH - 1);
                    int y1 = Mathf.Min(y0 + 1, TEXTURE_HEIGHT - 1);
        
                    float tx = origX - x0;
                    float ty = origY - y0;
        
                    Color c00 = cols[y0 * TEXTURE_WIDTH + x0];
                    Color c10 = cols[y0 * TEXTURE_WIDTH + x1];
                    Color c01 = cols[y1 * TEXTURE_WIDTH + x0];
                    Color c11 = cols[y1 * TEXTURE_WIDTH + x1];
        
                    Color interpolatedColor = Color.Lerp(
                        Color.Lerp(c00, c10, tx),
                        Color.Lerp(c01, c11, tx),
                        ty
                    );
        
                    resizedCols[y * TEXTURE_WIDTH + x] = interpolatedColor;
                }
            }

            debugTexture.SetPixels(resizedCols);
            debugTexture.Apply();
        }
        else
        {
            if (debugTexture != null)
            {
                DestroyImmediate(debugTexture);
                debugTexture = null;
            }
        }
    }
    
    //Generate LookUpTable
    private void GenerateLUT()
    {
        if (_rampTexture == null)
        {
            _rampTexture = new Texture2D(TEXTURE_WIDTH, TEXTURE_HEIGHT, TextureFormat.RGB24, false);
            _rampTexture.wrapMode = TextureWrapMode.Clamp;
        }

        float index = 1.0f / (TEXTURE_WIDTH);

        for (int y = 0; y < TEXTURE_HEIGHT; y++)
        {
            for (int x = 0; x < TEXTURE_WIDTH; x++)
            {
                float time = x * index;
                Color color;

                if (y == 0) color = _DaySkyGradient.Evaluate(time);
                else if (y < 6) color = Color.Lerp(_DaySkyGradient.Evaluate(time), _SunupSkyGradient.Evaluate(time), 1 - (float)Math.Exp(-( y / 5f ) *1));
                else if (y == 6) color = _SunupSkyGradient.Evaluate(time);
                else if (y < 12) color = Color.Lerp(_SunupSkyGradient.Evaluate(time), _NightSkyGradient.Evaluate(time), 1 - (float)Math.Exp(-( (y - 6)/ 5f) *4));
                else color = _NightSkyGradient.Evaluate(time);

                cols[y * TEXTURE_WIDTH + x] = color;
            }
        }
        _rampTexture.SetPixels(cols);
        _rampTexture.Apply();
        Shader.SetGlobalTexture(SkyGradient, _rampTexture);
        OnValidate();
    }
    
    //For Communicate with Custom Editor
    public void Validate()
    {
        OnValidate();
    }

}

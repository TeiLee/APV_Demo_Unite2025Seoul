Shader "Sky_Gradient"
{
    Properties
    {
        [Header(Sun)] 
        _SunScale("Sun Disk Scale", Range(0, 0.5)) = 0.1
        _SunPower("Sun Disk Power", Range(0.1, 10)) = 1.0
        _SunDiskIntensity("Sun Disk Intensity", Range(0, 1000)) = 100.0

        [Header(Moon)]
        _MoonScale("Moon Disk Scale", Range(0, 0.5)) = 0.1
        _MoonPower("Moon Disk Power", Range(0.1, 10)) = 1.0
        _MoonDiskIntensity("Moon Disk Intensity", Range(0, 1000)) = 100.0

        [HideInInspector]_HorizontalFalloff("Horizontal Glow Falloff", Range(0, 1)) = 0.35
        [HideInInspector]_HorizontalGradient("Horizontal Glow Intensity", Range(0, 100)) = 1
        
        [Header(Other)]
        _GroundColor("Ground", Color) = (0.44, 0.41, 0.39, 1)
        _FogStrength("Fog Strength", Range(0, 1)) = 0

        [HideInInspector] _SkyTimeMulA("Sky Time A", Range(0,1)) = 0.54
        [HideInInspector] _SkyTimeMulB("Sky Time B", Range(0,1)) = 0.46
    }

    SubShader
    {
        Tags { "RenderType"="Background" "RenderPipeline"="UniversalPipeline" "PreviewType"="Skybox" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 uv : TEXCOORD1;
            };

            // Texture
            TEXTURE2D(_GradientA); SAMPLER(sampler_GradientA);
            
            CBUFFER_START(UnityPerMaterial)
                // Common Param
                half4 _GroundColor;
                half _FogStrength;

                // Sun Param
                half _SunScale, _SunPower, _SunDiskIntensity;

                // Moon Param
                half _MoonScale, _MoonPower, _MoonDiskIntensity;

                // Sunset Param
                half _HorizontalFalloff, _HorizontalGradient;

                // Debug Param
                float _SkyTimeMulA, _SkyTimeMulB;
            CBUFFER_END
                half3 _SunLightVector;
                half3 _MoonLightVector;
            
            // Generate Disk
            half CalcCelestialDisk(float3 lightDir, float3 rayDir, half scale, half power)
            {
                half dist = distance(lightDir, rayDir);
                half falloff = 1.0 - smoothstep(0.0, scale, dist);
                return pow(abs(falloff), power);
            }

            //ReRange 
            half Remap(half value, half2 inRange, half2 outRange)
            {
                return outRange.x + (value - inRange.x) * (outRange.y - outRange.x) / (inRange.y - inRange.x);
            }

            //Sunset
            half CalculateSunset(float3 lightDir, float3 positionOS, float3 uv, half falloff)
            {
                half verticalMask = 1.0 - abs(uv.g);
                half horizontalDist = Remap(distance(normalize(positionOS), lightDir), half2(0,2), half2(0,1));
                half horizontalMask = pow(horizontalDist, falloff);
                
                half blendFactor = smoothstep(horizontalMask, 1.0, verticalMask);
                half lightInfluence = smoothstep(-0.2, 0.5, lightDir.g);
                
                return saturate(pow((1.0 - horizontalMask)*4,4) * blendFactor * lightInfluence);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.uv = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                //Common
                half3 viewDir = normalize(IN.positionWS);
                half absUVg = abs(IN.uv.g);
                half skyMask = saturate(absUVg * 30.0);
                half fogMask = saturate(absUVg * 10.0);

                //SkyTime
                half skyTimeDot = dot(_SunLightVector, half3(0, -1, 0));
                half skyTime = lerp(
                    skyTimeDot * _SkyTimeMulA + _SkyTimeMulB,
                    skyTimeDot * _SkyTimeMulB + _SkyTimeMulB,
                    step(0, skyTimeDot)
                );

                //Sky Color
                half4 skyColor = SAMPLE_TEXTURE2D(_GradientA, sampler_GradientA, float2(IN.uv.y, skyTime));
                half4 sky = lerp(_GroundColor, skyColor, saturate(absUVg * 25.0));

                //CelestialDisk
                half sunIntensity = CalcCelestialDisk(normalize(_SunLightVector), viewDir, _SunScale, _SunPower) * _SunDiskIntensity * skyMask;
                half moonIntensity = CalcCelestialDisk(normalize(_MoonLightVector), viewDir, _MoonScale, _MoonPower) * _MoonDiskIntensity * skyMask;

                //Final color
                half3 celestialColor = (sunIntensity + moonIntensity) * GetMainLight().color;
                half3 sunsetColor = CalculateSunset(_SunLightVector, IN.uv, IN.uv, _HorizontalFalloff) * GetMainLight().color * _HorizontalGradient;
                
                half4 finalColor = half4(sunsetColor + ((IN.uv.g < 0) ? _GroundColor.xyz : sky.xyz + celestialColor ),1);
                return lerp(finalColor, unity_FogColor, (1.0 - fogMask) * _FogStrength);
            }
            ENDHLSL
        }
    }
}

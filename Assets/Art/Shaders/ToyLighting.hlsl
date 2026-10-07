#ifndef PIXEL_DEFENSE_TOY_LIGHTING_INCLUDED
#define PIXEL_DEFENSE_TOY_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Scene-wide look, pushed once by ToyLook (Shader.SetGlobal*), so every toy material shares one style.
half4 _PD_ShadowTint;   // rgb: tint of shaded sides, a: tint strength
half4 _PD_RimColor;     // rgb: rim light color, a: strength
half4 _PD_LightParams;  // x: wrap, y: specular power, z: specular strength, w: ambient multiplier

// Soft "toy plastic" lighting: wrapped diffuse, tinted shade, a gentle highlight and a rim.
half3 ToyShade(half3 albedo, float3 positionWS, half3 normalWS, half3 viewDirWS)
{
    float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
    Light light = GetMainLight(shadowCoord);

    half wrap = _PD_LightParams.x;
    half ndl = dot(normalWS, light.direction);
    half diffuse = saturate((ndl + wrap) / (1.0h + wrap));
    half shadow = light.shadowAttenuation * light.distanceAttenuation;
    half direct = diffuse * shadow;

    half3 ambient = SampleSH(normalWS) * _PD_LightParams.w;
    half3 shadeTint = lerp(half3(1.0h, 1.0h, 1.0h), _PD_ShadowTint.rgb, _PD_ShadowTint.a * (1.0h - direct));
    half3 color = albedo * (light.color * direct + ambient * shadeTint);

    half3 halfDir = SafeNormalize(light.direction + viewDirWS);
    half specular = pow(saturate(dot(normalWS, halfDir)), max(_PD_LightParams.y, 1.0h)) * _PD_LightParams.z * shadow;
    color += specular * light.color;

    half rim = pow(1.0h - saturate(dot(normalWS, viewDirWS)), 3.0h) * _PD_RimColor.a;
    color += rim * _PD_RimColor.rgb * (0.35h + 0.65h * diffuse);
    return color;
}

// Shared shadow-caster position (normal bias toward the light), mirroring URP's ShadowCasterPass.
float3 _LightDirection;
float3 _LightPosition;

float4 ToyShadowPositionCS(float3 positionOS, float3 normalOS)
{
    float3 positionWS = TransformObjectToWorld(positionOS);
    float3 normalWS = TransformObjectToWorldNormal(normalOS);
#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
    return ApplyShadowClamping(positionCS);
}

#endif

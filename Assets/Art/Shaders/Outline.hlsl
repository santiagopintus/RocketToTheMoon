#ifndef OUTLINE_INCLUDED
#define OUTLINE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _OutlineColor;
    float _OutlineWidth;
    float _OcclusionTolerance;
CBUFFER_END

// Largest rim radius the composite loop supports, in pixels (after resolution scaling).
#define OUTLINE_MAX_RADIUS 24

#ifdef OUTLINE_COMPOSITE
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
#endif
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

// Eye depth (distance along the view direction) of the opaque scene at a pixel of the camera
// target. Valid for perspective and orthographic cameras.
float SceneEyeDepth(int2 pixel)
{
    float rawDepth = LoadSceneDepth(uint2(pixel));
    if (unity_OrthoParams.w == 0)
        return LinearEyeDepth(rawDepth, _ZBufferParams);

    #if UNITY_REVERSED_Z
        rawDepth = 1.0 - rawDepth;
    #endif
    return lerp(_ProjectionParams.y, _ProjectionParams.z, rawDepth);
}

#ifndef OUTLINE_COMPOSITE

// ---------------------------------------------------------------------------------------------
// Pass 0 - mask. Writes the eye depth of the outlined objects' nearest *visible* surface. The mask
// is cleared to 0, so 0 means "no visible outlined object here".
//
// Only pixels where the object is actually visible count (tested against the scene depth
// texture). Otherwise geometry hidden below the floor - e.g. an object resting on terrain whose
// rendered surface sits a few mm above its collider - would extend the silhouette, and the rim
// would wrap that invisible part instead of the edge the player sees.
// ---------------------------------------------------------------------------------------------

struct MaskAttributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct MaskVaryings
{
    float4 positionCS : SV_POSITION;
    float eyeDepth : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

MaskVaryings MaskVertex(MaskAttributes input)
{
    MaskVaryings output = (MaskVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS = TransformWorldToHClip(positionWS);
    output.eyeDepth = -TransformWorldToView(positionWS).z;
    return output;
}

float MaskFragment(MaskVaryings input) : SV_Target
{
    // The object wrote its own depth in the opaque pass, so its visible surface matches the scene
    // depth; the small relative bias absorbs depth-buffer precision.
    float sceneDepth = SceneEyeDepth(int2(input.positionCS.xy));
    if (input.eyeDepth > sceneDepth + max(0.01, input.eyeDepth * 0.01))
        discard;

    return max(input.eyeDepth, 1e-4);
}

#else

// ---------------------------------------------------------------------------------------------
// Pass 1 - composite (fullscreen, _BlitTexture = mask, same size/orientation as the target).
// ---------------------------------------------------------------------------------------------

float LoadMask(int2 pixel)
{
    // Clamp instead of relying on out-of-bounds loads returning 0 (not guaranteed on every API).
    int2 maxPixel = int2(_ScaledScreenParams.xy) - 1;
    return LOAD_TEXTURE2D_X(_BlitTexture, clamp(pixel, int2(0, 0), maxPixel)).r;
}

half4 CompositeFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    int2 pixel = int2(input.positionCS.xy);

    // Inside the silhouette: the object itself is visible there, no rim on top of it.
    if (LoadMask(pixel) > 0)
        discard;

    // Width is defined at 1080p and scaled with the render height, so the rim covers the same
    // fraction of the screen at any resolution / render scale.
    float radius = clamp(_OutlineWidth * (_ScaledScreenParams.y / 1080.0), 0.5, OUTLINE_MAX_RADIUS);
    int searchRadius = (int)ceil(radius + 0.5);

    // Nearest silhouette pixel within a disk (not a square - a square would make the rim thicker
    // at the diagonals) and the nearest outlined surface depth around this pixel.
    float nearestDistance = 1e6;
    float objectDepth = 1e6;

    [loop]
    for (int y = -searchRadius; y <= searchRadius; y++)
    {
        [loop]
        for (int x = -searchRadius; x <= searchRadius; x++)
        {
            float distance = length(float2(x, y));
            if (distance > radius + 0.5)
                continue;

            float maskDepth = LoadMask(pixel + int2(x, y));
            if (maskDepth > 0)
            {
                nearestDistance = min(nearestDistance, distance);
                objectDepth = min(objectDepth, maskDepth);
            }
        }
    }

    // 1 inside the rim, fading over the last pixel for an anti-aliased outer edge.
    float coverage = saturate(radius + 0.5 - nearestDistance);
    if (coverage <= 0)
        discard;

    // Occlusion: hide the rim only where the scene is clearly in front of the outlined object.
    // The floor an object stands on sits at (almost) the object's own depth around its base, so
    // the rim stays visible there; walls, props or the player's hands in front still hide it.
    if (SceneEyeDepth(pixel) < objectDepth - _OcclusionTolerance)
        discard;

    return half4(_OutlineColor.rgb, _OutlineColor.a * coverage);
}

#endif

#endif

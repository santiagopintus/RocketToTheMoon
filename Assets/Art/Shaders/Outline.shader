// Outline around the rocket part under the cursor (copied from the ONLINE-PARTY-TEMPLATE project).
// Drawn by OutlineRendererFeature around everything on the "Outline" layer (see PartOutline).
Shader "Custom/Outline"
{
    // _OutlineWidth: rim width in pixels at 1080p; scales with the render height so it looks the
    //   same at any resolution.
    // _OcclusionTolerance: how far (metres) scene geometry may be in front of the outlined object
    //   before it hides the rim - keeps the rim visible over the floor the object stands on, while
    //   walls and props in front still hide it.
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0.949, 0.863, 0.702, 1)
        _OutlineWidth("Outline Width (px at 1080p)", Range(0.5, 16)) = 5
        _OcclusionTolerance("Occlusion Tolerance (m)", Float) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        // 0: outlined objects -> mask (eye depth of their nearest surface; 0 = no object).
        Pass
        {
            Name "OutlineMask"

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex MaskVertex
            #pragma fragment MaskFragment
            #pragma multi_compile_instancing
            #include "Outline.hlsl"
            ENDHLSL
        }

        // 1: fullscreen rim composited over the camera colour.
        Pass
        {
            Name "OutlineComposite"

            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment CompositeFragment
            #define OUTLINE_COMPOSITE
            #define USE_FULL_PRECISION_BLIT_TEXTURE
            #include "Outline.hlsl"
            ENDHLSL
        }
    }
}

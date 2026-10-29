using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace RocketToTheMoon.Core
{
    // Draws an outline around everything on the "Outline" layer (copied from the ONLINE-PARTY-TEMPLATE project).
    // It is added to PC_Renderer and Mobile_Renderer with the Outline material (shader Custom/Outline).
    // 1. Mask: the outlined meshes are drawn into a hidden texture that stores how far away they are.
    // 2. Composite: every pixel close to that shape, but outside it, gets the outline colour.
    public class OutlineRendererFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class OutlineSettings
        {
            public LayerMask layerMask = 0;
            public Material material;
            // After transparents, because the sky, hills and moon are transparent and would paint over the outline.
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public OutlineSettings settings = new OutlineSettings();

        OutlinePass pass;

        // Creates the render pass.
        public override void Create()
        {
            pass = new OutlinePass();
        }

        // Adds the pass for the game camera, only while a part has the outline.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.material == null || settings.material.passCount < 2 || settings.layerMask.value == 0) return;
            if (PartOutline.Current == null) return;

            CameraType cameraType = renderingData.cameraData.cameraType;
            if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection) return;
            if (renderingData.cameraData.renderType == CameraRenderType.Overlay) return;

            pass.Setup(settings);
            renderer.EnqueuePass(pass);
        }

        class OutlinePass : ScriptableRenderPass
        {
            const int MaskPassIndex = 0;
            const int CompositePassIndex = 1;

            static readonly List<ShaderTagId> ShaderTags = new List<ShaderTagId>
            {
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
            };

            Material material;
            LayerMask layerMask;

            class MaskPassData
            {
                public RendererListHandle rendererList;
            }

            class CompositePassData
            {
                public TextureHandle mask;
                public Material material;
            }

            // The mask is read by pixel position, so the pass needs a texture the same way up as the mask (not the screen itself).
            public OutlinePass()
            {
                profilingSampler = new ProfilingSampler("Outline");
                requiresIntermediateTexture = true;
            }

            // Reads the settings and asks for the scene depth, which decides whether the outline is hidden behind something.
            public void Setup(OutlineSettings settings)
            {
                material = settings.material;
                layerMask = settings.layerMask;
                renderPassEvent = settings.renderPassEvent;
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            // Draws the mask, then the outline on top of the camera picture.
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();

                if (resourceData.isActiveTargetBackBuffer || !resourceData.cameraDepthTexture.IsValid()) return;

                RenderTextureDescriptor maskDesc = cameraData.cameraTargetDescriptor;
                maskDesc.msaaSamples = 1;
                maskDesc.depthStencilFormat = GraphicsFormat.None;
                maskDesc.graphicsFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R32_SFloat, GraphicsFormatUsage.Render)
                    ? GraphicsFormat.R32_SFloat
                    : GraphicsFormat.R16_SFloat;
                TextureHandle mask = UniversalRenderer.CreateRenderGraphTexture(renderGraph, maskDesc, "_OutlineMask", true);

                RenderTextureDescriptor depthDesc = cameraData.cameraTargetDescriptor;
                depthDesc.msaaSamples = 1;
                depthDesc.graphicsFormat = GraphicsFormat.None;
                depthDesc.depthStencilFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
                TextureHandle maskDepth = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDesc, "_OutlineMaskDepth", true);

                using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>("Outline Mask", out var passData, profilingSampler))
                {
                    DrawingSettings drawing = RenderingUtils.CreateDrawingSettings(ShaderTags, renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);
                    drawing.overrideMaterial = material;
                    drawing.overrideMaterialPassIndex = MaskPassIndex;
                    var filtering = new FilteringSettings(RenderQueueRange.all, layerMask);

                    passData.rendererList = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawing, filtering));
                    builder.UseRendererList(passData.rendererList);
                    builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(maskDepth, AccessFlags.Write);
                    builder.SetRenderFunc((MaskPassData data, RasterGraphContext context) =>
                        context.cmd.DrawRendererList(data.rendererList));
                }

                using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>("Outline Composite", out var passData, profilingSampler))
                {
                    passData.mask = mask;
                    passData.material = material;

                    builder.UseTexture(mask, AccessFlags.Read);
                    builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                    builder.SetRenderFunc((CompositePassData data, RasterGraphContext context) =>
                        Blitter.BlitTexture(context.cmd, data.mask, new Vector4(1f, 1f, 0f, 0f), data.material, CompositePassIndex));
                }
            }
        }
    }
}

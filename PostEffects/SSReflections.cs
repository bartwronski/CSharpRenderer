using System;
using System.Collections.Generic;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using SlimDX;

namespace CSharpRenderer
{
    class SSReflectionsEffectPass
    {
        RenderTargetSet.RenderTargetDescriptor m_ResultsRTDescriptor;

        public SSReflectionsEffectPass(SlimDX.Direct3D11.Device device, int resolutionX, int resolutionY)
        {
            m_ResultsRTDescriptor = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = Format.R16G16B16A16_Float,
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = resolutionY,
                m_Width = resolutionX
            };

            TemporalSurfaceManager.InitializeRenderTarget("SSReflections", m_ResultsRTDescriptor);
        }

        public void ExecutePass(DeviceContext context, RenderTargetSet linearDepth, RenderTargetSet motionVectors, RenderTargetSet surfaceNormals, RenderTargetSet sourceColor, Camera sceneCam, DepthOperationsPass depthOps)
        {
            RenderTargetSet ssrCurrent = TemporalSurfaceManager.GetRenderTargetCurrent("SSReflections");
            RenderTargetSet ssrHistory = TemporalSurfaceManager.GetRenderTargetHistory("SSReflections");
            RenderTargetSet traceRT = RenderTargetManager.RequestRenderTargetFromPool(m_ResultsRTDescriptor);

            if (DebugManager.IsFeatureOn("SSReflections"))
            {
                using (new GpuProfilePoint(context, "SSReflections"))
                {
                    using (new GpuProfilePoint(context, "SSReflectionsRaytrace"))
                    {
                        context.PixelShader.SetShaderResource(PostEffectHelper.m_RandomNumbersBuffer.m_ShaderResourceView, 39);
                        linearDepth.BindSRV(context, 0);
                        surfaceNormals.BindSRV(context, 1);
                        sourceColor.BindSRV(context, 2);
                        motionVectors.BindSRV(context, 3);
                        traceRT.BindAsRenderTarget(context);
                        PostEffectHelper.RenderFullscreenTriangle(context, "SSReflectionsRaytrace");
                        RenderTargetSet.BindNull(context);
                        ContextHelper.ClearSRVs(context);
                    }

                    using (new GpuProfilePoint(context, "SSReflectionsBlur"))
                    {
                        linearDepth.BindSRV(context, 0);
                        surfaceNormals.BindSRV(context, 1);
                        traceRT.BindSRV(context, 2);
                        motionVectors.BindSRV(context, 3);
                        ssrHistory.BindSRV(context, 4);
                        ssrCurrent.BindAsRenderTarget(context);
                        PostEffectHelper.RenderFullscreenTriangle(context, "SSReflectionsBlur");
                        RenderTargetSet.BindNull(context);
                        ContextHelper.ClearSRVs(context);
                    }

                }
            }
            else
            {
                traceRT.Clear(context, new Color4(0.0f, 0.0f, 0.0f, 0.0f));
                ssrCurrent.Clear(context, new Color4(0.0f, 0.0f, 0.0f, 0.0f));
            }

            DebugManager.RegisterDebug(context, "SSRRaytrace", traceRT);
            DebugManager.RegisterDebug(context, "SSRBlur", ssrCurrent);

            RenderTargetManager.ReleaseRenderTargetToPool(traceRT);
        }
    }
}

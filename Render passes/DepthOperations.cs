using System;
using System.Collections.Generic;
using SlimDX.Direct3D11;
using SlimDX.DXGI;

namespace CSharpRenderer
{
    class DepthOperationsPass
    {
        public RenderTargetSet m_LinearDepth;
        public RenderTargetSet m_HalfLinearDepth;
        public RenderTargetSet m_HalfNormals;
        public RenderTargetSet m_BilateralUpsampleOffsets;

        public DepthOperationsPass(int resolutionX, int resolutionY)
        {
            RenderTargetSet.RenderTargetDescriptor linearDepthDescriptor = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = Format.R16_Float,
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = resolutionY,
                m_Width = resolutionX,
            };

            m_LinearDepth = RenderTargetManager.RequestRenderTargetFromPool(linearDepthDescriptor);

            RenderTargetSet.RenderTargetDescriptor halfLinearDepthDescriptor = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = Format.R16_Float,
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = resolutionY / 2,
                m_Width = resolutionX / 2,
            };

            m_HalfLinearDepth = RenderTargetManager.RequestRenderTargetFromPool(halfLinearDepthDescriptor);
            
            RenderTargetSet.RenderTargetDescriptor normalsDescriptor = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = Format.R8G8B8A8_UNorm,
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = resolutionY / 2,
                m_Width = resolutionX / 2,
            };

            m_HalfNormals = RenderTargetManager.RequestRenderTargetFromPool(normalsDescriptor);

            m_HalfLinearDepth.m_RenderTargets[1] = m_HalfNormals.m_RenderTargets[0];
            m_HalfLinearDepth.m_NumRTs++;

            RenderTargetSet.RenderTargetDescriptor bilateralUpsampleDescriptor = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = Format.R8G8_SNorm,
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = resolutionY,
                m_Width = resolutionX,
            };

            m_BilateralUpsampleOffsets = RenderTargetManager.RequestRenderTargetFromPool(bilateralUpsampleDescriptor);

        }

        public void ExecutePass(DeviceContext context, RenderTargetSet sourceSet)
        {
            PostEffectHelper.LinearizeDepth(context, m_LinearDepth, sourceSet);
            DebugManager.RegisterDebug(context, "LinearDepth", m_LinearDepth);

            using (new GpuProfilePoint(context, "DownsampleDepthNormals"))
            {
                m_LinearDepth.BindSRV(context, 0);
                sourceSet.BindSRV(context, 1);

                m_HalfLinearDepth.BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, "DownsampleDepthNormals");
                RenderTargetSet.BindNull(context);
                ContextHelper.ClearSRVs(context);
            }
            DebugManager.RegisterDebug(context, "HalfLinearDepth", m_HalfLinearDepth);
            DebugManager.RegisterDebug(context, "HalfNormals", m_HalfLinearDepth, 1);

            using (new GpuProfilePoint(context, "OutputBilateralOffsets"))
            {
                m_LinearDepth.BindSRV(context, 0);
                m_HalfLinearDepth.BindSRV(context, 2);

                m_BilateralUpsampleOffsets.BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, "OutputBilateralOffsets");
                RenderTargetSet.BindNull(context);
                ContextHelper.ClearSRVs(context);
            }

            DebugManager.RegisterDebug(context, "BilateralOffsets", m_BilateralUpsampleOffsets);
        }
    }
}

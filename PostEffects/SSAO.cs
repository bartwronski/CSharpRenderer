using System;
using System.Collections.Generic;
using SlimDX.Direct3D11;
using SlimDX.DXGI;

namespace CSharpRenderer
{
    class SSAOEffectPass
    {
        CustomConstantBufferInstance m_SSAOBuffer;
        RenderTargetSet.RenderTargetDescriptor m_RTDescriptor;
        RenderTargetSet.RenderTargetDescriptor m_UpsampleDebugRTDescriptor;

        public SSAOEffectPass(SlimDX.Direct3D11.Device device, int resolutionX, int resolutionY)
        {
            m_RTDescriptor = new RenderTargetSet.RenderTargetDescriptor()
                {
                    m_Format = Format.R16G16_Float,
                    m_HasDepth = false,
                    m_NumSurfaces = 1,
                    m_Height = resolutionY,
                    m_Width = resolutionX
                };

            m_UpsampleDebugRTDescriptor = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = Format.R8_UNorm,
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = resolutionY * 2,
                m_Width = resolutionX * 2
            };

            TemporalSurfaceManager.InitializeRenderTarget("SSAO", m_RTDescriptor);

            m_SSAOBuffer = ShaderManager.CreateConstantBufferInstance("SSAOBuffer", device);
        }

        public void ExecutePass(DeviceContext context, RenderTargetSet target, RenderTargetSet linearDepth, RenderTargetSet motionVectors, RenderTargetSet surfaceNormals, DepthOperationsPass depthOps)
        {
            RenderTargetSet ssaoCurrent = TemporalSurfaceManager.GetRenderTargetCurrent("SSAO");
            RenderTargetSet ssaoHistory = TemporalSurfaceManager.GetRenderTargetHistory("SSAO");

            if (DebugManager.IsFeatureOn("SSAO"))
            {
                RenderTargetSet tempBlurBuffer = RenderTargetManager.RequestRenderTargetFromPool(m_RTDescriptor);

                Random rand = new Random();

                dynamic scb = m_SSAOBuffer;
                scb.g_SSAOPhase = (float)rand.NextDouble() * 3.1415f;
                m_SSAOBuffer.CompileAndBind(context);

                using (new GpuProfilePoint(context, "SSAO"))
                {
                    using (new GpuProfilePoint(context, "SSAOCalculate"))
                    {
                        linearDepth.BindSRV(context, 0);
                        ssaoHistory.BindSRV(context, 1);
                        motionVectors.BindSRV(context, 2);
                        surfaceNormals.BindSRV(context, 3);
                        ssaoCurrent.BindAsRenderTarget(context);
                        PostEffectHelper.RenderFullscreenTriangle(context, "SSAOCalculate");
                        RenderTargetSet.BindNull(context);
                        ContextHelper.ClearSRVs(context);
                    }

                    DebugManager.RegisterDebug(context, "SSAOMain", ssaoCurrent);

                    using (new GpuProfilePoint(context, "SSAOBlur"))
                    {
                        ssaoCurrent.BindSRV(context, 1);
                        tempBlurBuffer.BindAsRenderTarget(context);
                        PostEffectHelper.RenderFullscreenTriangle(context, "SSAOBlurHorizontal");
                        RenderTargetSet.BindNull(context);
                        ContextHelper.ClearSRVs(context);

                        DebugManager.RegisterDebug(context, "SSAOBlurH", tempBlurBuffer);

                        tempBlurBuffer.BindSRV(context, 1);
                        target.BindAsRenderTarget(context);
                        PostEffectHelper.RenderFullscreenTriangle(context, "SSAOBlurVertical");
                        RenderTargetSet.BindNull(context);
                        ContextHelper.ClearSRVs(context);

                        DebugManager.RegisterDebug(context, "SSAOBlurV", target);
                    }
                }

                RenderTargetManager.ReleaseRenderTargetToPool(tempBlurBuffer);
            }
            else
            {
                target.Clear(context, new SlimDX.Color4(1.0f, 1.0f, 1.0f, 1.0f));
            }


            if (DebugManager.IsDebugging("UpsampledSSAO"))
            {
                RenderTargetSet tempFullResBuffer = RenderTargetManager.RequestRenderTargetFromPool(m_UpsampleDebugRTDescriptor);

                target.BindSRV(context, 0);
                depthOps.m_BilateralUpsampleOffsets.BindSRV(context, 10);
                tempFullResBuffer.BindAsRenderTarget(context);
                
                PostEffectHelper.RenderFullscreenTriangle(context, "BilateralUpsample");
                RenderTargetSet.BindNull(context);
                ContextHelper.ClearSRVs(context);

                DebugManager.RegisterDebug(context, "UpsampledSSAO", tempFullResBuffer);
                RenderTargetManager.ReleaseRenderTargetToPool(tempFullResBuffer);
            }
        }
    }
}

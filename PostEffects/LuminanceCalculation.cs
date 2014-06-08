using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.Direct3D11;
using SlimDX.DXGI;

namespace CSharpRenderer
{
    class LuminanceCalculations
    {
        RenderTargetSet.RenderTargetDescriptor m_RTDescriptorLuminance4x4;
        RenderTargetSet.RenderTargetDescriptor m_RTDescriptorLuminance16x16;
        RenderTargetSet.RenderTargetDescriptor m_RTDescriptorLuminance32x32;
        RenderTargetSet.RenderTargetDescriptor m_RTDescriptorLuminance64x64;
        RenderTargetSet.RenderTargetDescriptor m_RTDescriptorLuminance128x128;
        RenderTargetSet.RenderTargetDescriptor m_RTDescriptorLuminance256x256;

        RenderTargetSet.RenderTargetDescriptor m_RTDescriptorFinalLuminance;

        RenderTargetSet m_FinalLuminance;

        public LuminanceCalculations(SlimDX.Direct3D11.Device device, int resolutionX, int resolutionY)
        {
            Format luminanceFormat = Format.R16_Float;
            m_RTDescriptorLuminance4x4 = new RenderTargetSet.RenderTargetDescriptor()
                {
                    m_Format = luminanceFormat,
                    m_HasDepth = false,
                    m_NumSurfaces = 1,
                    m_Height = resolutionY / 4,
                    m_Width = resolutionX / 4
                };

            m_RTDescriptorLuminance16x16 = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = luminanceFormat,
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = resolutionY / 16,
                m_Width = resolutionX / 16
            };

            m_RTDescriptorLuminance32x32 = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = luminanceFormat,
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = resolutionY / 32,
                m_Width = resolutionX / 32
            };

            m_RTDescriptorLuminance64x64 = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = luminanceFormat,
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = resolutionY / 64,
                m_Width = resolutionX / 64
            };

            m_RTDescriptorLuminance128x128 = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = luminanceFormat,
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = resolutionY / 128,
                m_Width = resolutionX / 128
            };

            m_RTDescriptorLuminance256x256 = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = luminanceFormat,
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = resolutionY / 256,
                m_Width = resolutionX / 256
            };

            m_RTDescriptorFinalLuminance = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = Format.R32_Float, // to be able to read and write at the same time...
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = 1,
                m_Width = 1
            };

            m_FinalLuminance = RenderTargetManager.RequestRenderTargetFromPool(m_RTDescriptorFinalLuminance);
        }

        public RenderTargetSet ExecutePass(DeviceContext context, RenderTargetSet source)
        {
            RenderTargetSet[] luminanceTargets = new RenderTargetSet[6];
            luminanceTargets[0] = RenderTargetManager.RequestRenderTargetFromPool(m_RTDescriptorLuminance4x4);
            luminanceTargets[1] = RenderTargetManager.RequestRenderTargetFromPool(m_RTDescriptorLuminance16x16);
            luminanceTargets[2] = RenderTargetManager.RequestRenderTargetFromPool(m_RTDescriptorLuminance32x32);
            luminanceTargets[3] = RenderTargetManager.RequestRenderTargetFromPool(m_RTDescriptorLuminance64x64);
            luminanceTargets[4] = RenderTargetManager.RequestRenderTargetFromPool(m_RTDescriptorLuminance128x128);
            luminanceTargets[5] = RenderTargetManager.RequestRenderTargetFromPool(m_RTDescriptorLuminance256x256);

            using (new GpuProfilePoint(context, "Calculate luminance"))
            {
                source.BindSRV(context, 0);
                luminanceTargets[0].BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, "Downsample4x4CalculateLuminance");
                RenderTargetSet.BindNull(context);
                ContextHelper.ClearSRVs(context);

                luminanceTargets[0].BindSRV(context, 0);
                luminanceTargets[1].BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, "Downsample4x4Luminance");
                RenderTargetSet.BindNull(context);
                ContextHelper.ClearSRVs(context);

                luminanceTargets[1].BindSRV(context, 0);
                luminanceTargets[2].BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, "Downsample2x2Luminance");
                RenderTargetSet.BindNull(context);
                ContextHelper.ClearSRVs(context);


                luminanceTargets[2].BindSRV(context, 0);
                luminanceTargets[3].BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, "Downsample2x2Luminance");
                RenderTargetSet.BindNull(context);
                ContextHelper.ClearSRVs(context);

                luminanceTargets[3].BindSRV(context, 0);
                luminanceTargets[4].BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, "Downsample2x2Luminance");
                RenderTargetSet.BindNull(context);
                ContextHelper.ClearSRVs(context);

                luminanceTargets[4].BindSRV(context, 0);
                luminanceTargets[5].BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, "Downsample2x2Luminance");
                RenderTargetSet.BindNull(context);
                ContextHelper.ClearSRVs(context);

                luminanceTargets[5].BindSRV(context, 0);
                context.ComputeShader.SetUnorderedAccessView(m_FinalLuminance.m_RenderTargets[0].m_UnorderedAccessView, 0);
                ShaderManager.ExecuteComputeForSize(context, 1, 1, 1, "FinalCalculateAverageLuminance");
                ContextHelper.ClearSRVs(context);
                ContextHelper.ClearCSContext(context);
            }

            foreach (var lt in luminanceTargets)
            {
                RenderTargetManager.ReleaseRenderTargetToPool(lt);
            }

            return m_FinalLuminance;
        }
    }
}

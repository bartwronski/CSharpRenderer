using System;
using System.Collections.Generic;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using SlimDX;
using Device = SlimDX.Direct3D11.Device;
using Resource = SlimDX.Direct3D11.Resource;


namespace CSharpRenderer
{
    class VolumetricFog
    {
        public TextureObject m_DensityTexture;
        public TextureObject m_LightingTexturePing;
        public TextureObject m_LightingTexturePong;
        public TextureObject m_ScatteringTexture;

        int m_VolumeX;
        int m_VolumeY;
        int m_VolumeZ;

        bool m_Phase;

        public VolumetricFog()
        {
            m_DensityTexture = null;
            m_LightingTexturePing = null;
            m_LightingTexturePong = null;
            m_ScatteringTexture = null;
            m_Phase = false;
        }

        public void Initialize(Device device)
        {
            m_VolumeX = (int)ShaderManager.GetUIntShaderDefine("VOLUME_WIDTH");
            m_VolumeY = (int)ShaderManager.GetUIntShaderDefine("VOLUME_HEIGHT");
            m_VolumeZ = (int)ShaderManager.GetUIntShaderDefine("VOLUME_DEPTH");

            m_DensityTexture = TextureObject.CreateTexture3D(device, m_VolumeX, m_VolumeY, m_VolumeZ, Format.R16_Float);
            m_LightingTexturePing = TextureObject.CreateTexture3D(device, m_VolumeX, m_VolumeY, m_VolumeZ, Format.R16G16B16A16_Float);
            m_LightingTexturePong = TextureObject.CreateTexture3D(device, m_VolumeX, m_VolumeY, m_VolumeZ, Format.R16G16B16A16_Float);
            m_ScatteringTexture = TextureObject.CreateTexture3D(device, m_VolumeX, m_VolumeY, m_VolumeZ, Format.R16G16B16A16_Float);
        }

        public void RenderVolumetricFog(DeviceContext context, TextureObject shadowMap, GlobalIlluminationRenderer giRenderer)
        {
            // Not using temporal manager as it was designed for render target sets; TODO
            TextureObject previousFrameAccumulationTexture = m_Phase ? m_LightingTexturePing : m_LightingTexturePong;
            TextureObject currentFrameAccumulationTexture =  m_Phase ? m_LightingTexturePong : m_LightingTexturePing;
            m_Phase = !m_Phase;

            using (new GpuProfilePoint(context, "VolumetricFog"))
            {
#if false
                using (new GpuProfilePoint(context, "DensityEstimation"))
                {
                    context.ComputeShader.SetUnorderedAccessView(m_DensityTexture.m_UnorderedAccessView, 0);
                    ShaderManager.ExecuteComputeForResource(context, m_DensityTexture, "CalculateDensity");
                    ContextHelper.ClearCSContext(context);
                }
#endif

                using (new GpuProfilePoint(context, "LitFogVolume"))
                {
                    context.ComputeShader.SetShaderResource(giRenderer.m_GIVolumeR.m_ShaderResourceView, 5);
                    context.ComputeShader.SetShaderResource(giRenderer.m_GIVolumeG.m_ShaderResourceView, 6);
                    context.ComputeShader.SetShaderResource(giRenderer.m_GIVolumeB.m_ShaderResourceView, 7);

                    context.ComputeShader.SetShaderResource(previousFrameAccumulationTexture.m_ShaderResourceView, 8);

                    context.ComputeShader.SetShaderResource(m_DensityTexture.m_ShaderResourceView, 0);
                    context.ComputeShader.SetShaderResource(shadowMap.m_ShaderResourceView, 1);
                    context.ComputeShader.SetUnorderedAccessView(currentFrameAccumulationTexture.m_UnorderedAccessView, 1);
                    ShaderManager.ExecuteComputeForResource(context, currentFrameAccumulationTexture, "LitFogVolume");
                    ContextHelper.ClearCSContext(context);
                }

                using (new GpuProfilePoint(context, "ComputeScattering"))
                {
                    context.ComputeShader.SetShaderResource(currentFrameAccumulationTexture.m_ShaderResourceView, 0);
                    context.ComputeShader.SetUnorderedAccessView(m_ScatteringTexture.m_UnorderedAccessView, 2);
                    ShaderManager.ExecuteComputeForSize(context, m_VolumeX, m_VolumeY, 1, "ComputeScattering");
                    ContextHelper.ClearCSContext(context);
                }

            }
        }
    }
}

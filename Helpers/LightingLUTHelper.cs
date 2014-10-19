using System;
using System.Collections.Generic;
using System.Text;
using SlimDX;
using SlimDX.Direct3D11;
using Device = SlimDX.Direct3D11.Device;
using Resource = SlimDX.Direct3D11.Resource;
using Math = System.Math;
using SlimDX.DXGI;

namespace CSharpRenderer
{
    static class LightingLUTHelper
    {
        static RenderTargetSet m_EnvLightingLUT;
        static RenderTargetSet m_GGXLightingDLUT;
        static RenderTargetSet m_GGXLightingFVLUT;

        static LightingLUTHelper()
        {
        }

        public static void Initialize(Device device, DeviceContext context)
        {
            // Create helper textures
            m_EnvLightingLUT = RenderTargetSet.CreateRenderTargetSet(device, 128, 128, Format.R16G16_Float, 1, false);
            m_GGXLightingDLUT = RenderTargetSet.CreateRenderTargetSet(device, 256, 128, Format.R16_Float, 1, false);
            m_GGXLightingFVLUT = RenderTargetSet.CreateRenderTargetSet(device, 256, 128, Format.R16G16_Float, 1, false);

            m_GGXLightingDLUT.BindAsRenderTarget(context);
            PostEffectHelper.RenderFullscreenTriangle(context, "GenerateGGXLightingDLUT");
            RenderTargetSet.BindNull(context);

            m_GGXLightingFVLUT.BindAsRenderTarget(context);
            PostEffectHelper.RenderFullscreenTriangle(context, "GenerateGGXLightingFVLUT");
            RenderTargetSet.BindNull(context);

            m_EnvLightingLUT.BindAsRenderTarget(context);
            context.PixelShader.SetShaderResource(PostEffectHelper.m_RandomNumbersBuffer.m_ShaderResourceView, 39);
            PostEffectHelper.RenderFullscreenTriangle(context, "GenerateEnvLightingLUT");
            RenderTargetSet.BindNull(context);
        }

        public static void BindTextures(DeviceContext context)
        {
            m_GGXLightingDLUT.BindSRV(context, 40);
            m_GGXLightingFVLUT.BindSRV(context, 41);
            m_EnvLightingLUT.BindSRV(context, 42);
        }

    }
}

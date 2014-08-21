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
    static class PerlinNoiseRenderHelper
    {
        static RenderTargetSet m_PermTexture;
        static RenderTargetSet m_PermTexture2D;
        static RenderTargetSet m_GradTexture;
        static RenderTargetSet m_PermGradTexture;
        static RenderTargetSet m_PermGrad4DTexture;
        static RenderTargetSet m_GradTexture4D;

        static PerlinNoiseRenderHelper()
        {
        }

        public static void Initialize(Device device, DeviceContext context)
        {
            // Create helper textures
            m_PermTexture = RenderTargetSet.CreateRenderTargetSet(device, 256, 1, Format.R8_UNorm, 1, false);
            m_PermTexture2D = RenderTargetSet.CreateRenderTargetSet(device, 256, 256, Format.R8G8B8A8_UNorm, 1, false);
            m_GradTexture = RenderTargetSet.CreateRenderTargetSet(device, 16, 1, Format.R8G8B8A8_SNorm, 1, false);
            m_PermGradTexture = RenderTargetSet.CreateRenderTargetSet(device, 256, 1, Format.R8G8B8A8_SNorm, 1, false);
            m_PermGrad4DTexture = RenderTargetSet.CreateRenderTargetSet(device, 256, 1, Format.R8G8B8A8_SNorm, 1, false);
            m_GradTexture4D = RenderTargetSet.CreateRenderTargetSet(device, 32, 1, Format.R8G8B8A8_SNorm, 1, false);

            m_PermTexture.BindAsRenderTarget(context);
            PostEffectHelper.RenderFullscreenTriangle(context, "GeneratePermTexture");
            RenderTargetSet.BindNull(context);

            m_PermTexture2D.BindAsRenderTarget(context);
            PostEffectHelper.RenderFullscreenTriangle(context, "GeneratePermTexture2d");
            RenderTargetSet.BindNull(context);

            m_GradTexture.BindAsRenderTarget(context);
            PostEffectHelper.RenderFullscreenTriangle(context, "GenerateGradTexture");
            RenderTargetSet.BindNull(context);

            m_PermGradTexture.BindAsRenderTarget(context);
            PostEffectHelper.RenderFullscreenTriangle(context, "GeneratePermGradTexture");
            RenderTargetSet.BindNull(context);

            m_PermGrad4DTexture.BindAsRenderTarget(context);
            PostEffectHelper.RenderFullscreenTriangle(context, "GeneratePermGrad4dTexture");
            RenderTargetSet.BindNull(context);

            m_GradTexture4D.BindAsRenderTarget(context);
            PostEffectHelper.RenderFullscreenTriangle(context, "GenerateGradTexture4d");
            RenderTargetSet.BindNull(context);
        }

        public static void BindTextures(DeviceContext context)
        {
            m_PermTexture.BindSRV(context, 50);
            m_PermTexture2D.BindSRV(context, 51);
            m_GradTexture.BindSRV(context,52);
            m_PermGradTexture.BindSRV(context,53);
            m_PermGrad4DTexture.BindSRV(context, 54);
            m_GradTexture4D.BindSRV(context, 55);
        }

    }
}

using System;
using System.Collections.Generic;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using SlimDX;

namespace CSharpRenderer
{
    class ShadowEVSMGenerator
    {
        public RenderTargetSet m_EVSMTexture;

        public ShadowEVSMGenerator()
        {
            m_EVSMTexture = null;
        }


        public RenderTargetSet RenderShadows(DeviceContext context, SimpleSceneWrapper simpleSceneWrapper)
        {
            RenderTargetSet.RenderTargetDescriptor evsmDescriptor =
                new RenderTargetSet.RenderTargetDescriptor()
                {
                    m_Format = Format.R32G32B32A32_Float,
                    m_HasDepth = false,
                    m_NumSurfaces = 1,
                    m_Height = 1024,
                    m_Width = 1024
                };

            RenderTargetSet EVSMTexture = m_EVSMTexture == null ? RenderTargetManager.RequestRenderTargetFromPool(evsmDescriptor) : m_EVSMTexture;

            {
                RenderTargetSet.RenderTargetDescriptor shadowMapDescriptor =
                    new RenderTargetSet.RenderTargetDescriptor()
                    {
                        m_Format = Format.Unknown,
                        m_HasDepth = true,
                        m_NumSurfaces = 0,
                        m_Height = 2048,
                        m_Width = 2048
                    };

                RenderTargetSet shadowBuffer = RenderTargetManager.RequestRenderTargetFromPool(shadowMapDescriptor);

                using (new GpuProfilePoint(context, "Shadowmap"))
                {
                    // set the shaders
                    context.VertexShader.Set(ShaderManager.GetVertexShader("VertexShadow"));
                    context.PixelShader.Set(null);

                    shadowBuffer.Clear(context, new Color4(), true);
                    shadowBuffer.BindAsRenderTarget(context, true);

                    ContextHelper.SetDepthStencilState(context, ContextHelper.DepthConfigurationType.DepthWriteCompare);

                    // render triangles
                    simpleSceneWrapper.RenderNoMaterials(context);

                    RenderTargetSet.BindNull(context);
                }

                using (new GpuProfilePoint(context, "EVSM Resolve"))
                {
                    shadowBuffer.BindDepthAsSRV(context, 0);
                    EVSMTexture.BindAsRenderTarget(context);
                    PostEffectHelper.RenderFullscreenTriangle(context, "ReconstructEVSM");
                    RenderTargetSet.BindNull(context);
                    ContextHelper.ClearSRVs(context);
                }

                RenderTargetSet EVSMTextureTemp = RenderTargetManager.RequestRenderTargetFromPool(evsmDescriptor);

                using (new GpuProfilePoint(context, "EVSM Blur"))
                {
                    EVSMTexture.BindSRV(context, 1);
                    EVSMTextureTemp.BindAsRenderTarget(context);
                    PostEffectHelper.RenderFullscreenTriangle(context, "BlurEVSMHorizontal");
                    RenderTargetSet.BindNull(context);
                    ContextHelper.ClearSRVs(context);

                    EVSMTextureTemp.BindSRV(context, 1);
                    EVSMTexture.BindAsRenderTarget(context);
                    PostEffectHelper.RenderFullscreenTriangle(context, "BlurEVSMVertical");
                    RenderTargetSet.BindNull(context);
                    ContextHelper.ClearSRVs(context);
                }

                RenderTargetManager.ReleaseRenderTargetToPool(shadowBuffer);
                RenderTargetManager.ReleaseRenderTargetToPool(EVSMTextureTemp);
            }

            m_EVSMTexture = EVSMTexture;
            return EVSMTexture;
        }
    }
}

using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using SlimDX.Windows;
using Device = SlimDX.Direct3D11.Device;


namespace CSharpRenderer
{
    class ScatterDOFPass
    {
        public bool m_DebugBokeh;

        Texture2D                               m_BokehSpriteTexture;
        ShaderResourceView                      m_BokehSpriteTextureSRV;
        RenderTargetSet.RenderTargetDescriptor  m_HalfResDescriptor;
        RenderTargetSet.RenderTargetDescriptor  m_HalfHeightDescriptor;
        int                                     m_NumQuads;

        public void Initialize(Device device, int resolutionX, int resolutionY)
        {
            m_BokehSpriteTexture = Texture2D.FromFile(device, "textures\\bokeh.dds");
            m_BokehSpriteTextureSRV = new ShaderResourceView(device, m_BokehSpriteTexture);

            m_HalfResDescriptor = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = Format.R16G16B16A16_Float,
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = resolutionY / 2,
                m_Width = resolutionX / 2
            };

            m_HalfHeightDescriptor = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = Format.R16G16B16A16_Float,
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = resolutionY / 2,
                m_Width = resolutionX
            };

            m_NumQuads = resolutionX * resolutionY / 4;
        }

        public void ExecutePass(DeviceContext context, RenderTargetSet target, RenderTargetSet source, RenderTargetSet depthSource)
        {
            using (new GpuProfilePoint(context, "Bokeh DoF"))
            {
                RenderTargetSet halfResColorCoC = RenderTargetManager.RequestRenderTargetFromPool(m_HalfResDescriptor);
                RenderTargetSet bokehAccumulate = RenderTargetManager.RequestRenderTargetFromPool(m_HalfHeightDescriptor);

                using (new GpuProfilePoint(context, "Downsample"))
                {
                    source.BindSRV(context, 0);
                    depthSource.BindDepthAsSRV(context, 1);
                    halfResColorCoC.BindAsRenderTarget(context);
                    PostEffectHelper.RenderFullscreenTriangle(context, "DownsampleColorCoC");
                    ContextHelper.ClearSRVs(context);
                }

                using (new GpuProfilePoint(context, "Sprites"))
                {
                    bokehAccumulate.Clear(context, new Color4(0.0f, 0.0f, 0.0f, 0.0f));
                    bokehAccumulate.BindAsRenderTarget(context, false);
                    halfResColorCoC.BindSRV(context, 0);
                    context.PixelShader.SetShaderResource(m_BokehSpriteTextureSRV, 4);

                    context.VertexShader.Set(ShaderManager.GetVertexShader("VertexFullScreenDofGrid"));
                    context.PixelShader.Set(ShaderManager.GetPixelShader("BokehSprite"));

                    ContextHelper.SetBlendState(context, ContextHelper.BlendType.Additive);
                    PostEffectHelper.RenderFullscreenGrid(context, m_NumQuads);
                    ContextHelper.SetBlendState(context, ContextHelper.BlendType.None);
                    ContextHelper.ClearSRVs(context);
                }

                using (new GpuProfilePoint(context, "ResolveBokeh"))
                {
                    target.BindAsRenderTarget(context);
                    source.BindSRV(context, 0);
                    bokehAccumulate.BindSRV(context, 2);
                    halfResColorCoC.BindSRV(context, 3);
                    PostEffectHelper.RenderFullscreenTriangle(context, m_DebugBokeh ? "ResolveBokehDebug" : "ResolveBokeh");
                    ContextHelper.ClearSRVs(context);
                }

                RenderTargetManager.ReleaseRenderTargetToPool(halfResColorCoC);
                RenderTargetManager.ReleaseRenderTargetToPool(bokehAccumulate);

                RenderTargetSet.BindNull(context);
            }
        }
    }
}
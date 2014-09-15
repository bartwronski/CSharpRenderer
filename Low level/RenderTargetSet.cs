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
    class RenderTargetSet
    {
        public struct RenderTargetDescriptor
        {
            public int         m_Width;
            public int         m_Height;
            public Format      m_Format;
            public int         m_NumSurfaces;
            public bool        m_HasDepth;

            public static bool operator==(RenderTargetDescriptor first, RenderTargetDescriptor second)
            {
                return (first.m_Width == second.m_Width) &&
                    (first.m_Height == second.m_Height) &&
                    (first.m_Format == second.m_Format) &&
                    (first.m_NumSurfaces == second.m_NumSurfaces) &&
                    (first.m_HasDepth == second.m_HasDepth);
            }

            public static bool operator !=(RenderTargetDescriptor first, RenderTargetDescriptor second)
            {
                return !(first == second);
            }

            // override object.Equals
            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                {
                    return false;
                }

                return this == (RenderTargetDescriptor)obj;
            }

            // override object.GetHashCode
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

        };

        const int MAX_RENDERTARGETS = 8;
        const int MAX_UAVS = 4;
        public TextureObject[] m_RenderTargets = new TextureObject[MAX_RENDERTARGETS];
        public TextureObject[] m_UAVs = new TextureObject[MAX_UAVS];
        public TextureObject m_DepthStencil;
        public static DepthStencilState m_DepthStencilState;
        public static DepthStencilState m_DepthStencilStateNoDepth;
        public Viewport m_Viewport;
        public int m_NumRTs;
        public int m_UAVsNum;
        public RenderTargetDescriptor m_Descriptor;

        public RenderTargetSet()
        {
            for (int i = 0; i < MAX_RENDERTARGETS; ++i )
            {
                m_RenderTargets[i] = null;
            }

            for (int i = 0; i < MAX_UAVS; ++i)
            {
                m_UAVs[i] = null;
            }

            m_DepthStencil = null;

            m_NumRTs = 0;
            m_UAVsNum = 0;
            m_Viewport = new Viewport();
        }

        public void BindSRV(DeviceContext context, int slot, int surface = 0)
        {
            context.PixelShader.SetShaderResource(m_RenderTargets[surface].m_ShaderResourceView, slot);
            context.VertexShader.SetShaderResource(m_RenderTargets[surface].m_ShaderResourceView, slot);
            context.GeometryShader.SetShaderResource(m_RenderTargets[surface].m_ShaderResourceView, slot);
            context.ComputeShader.SetShaderResource(m_RenderTargets[surface].m_ShaderResourceView, slot);
        }

        public void BindDepthAsSRV(DeviceContext context, int slot)
        {
            context.PixelShader.SetShaderResource(m_DepthStencil.m_ShaderResourceView, slot);
            context.VertexShader.SetShaderResource(m_DepthStencil.m_ShaderResourceView, slot);
            context.GeometryShader.SetShaderResource(m_DepthStencil.m_ShaderResourceView, slot);
            context.ComputeShader.SetShaderResource(m_DepthStencil.m_ShaderResourceView, slot);
        }

        public static void BindTextureAsRenderTarget(DeviceContext context, RenderTargetView tex, DepthStencilView depth, int width, int height)
        {
            RenderTargetView[] renderTargetViews = new RenderTargetView[1];
            renderTargetViews[0] = tex;
            context.OutputMerger.SetTargets(depth, renderTargetViews);
            context.OutputMerger.DepthStencilState = depth != null ? m_DepthStencilState : m_DepthStencilStateNoDepth;
            context.Rasterizer.SetViewports(new Viewport(0, 0, width, height));
        }

        public void BindAsRenderTarget(DeviceContext context, bool depth = false, bool color = true)
        {
            RenderTargetView[] renderTargetViews = new RenderTargetView[m_NumRTs];
            UnorderedAccessView[] unorderedAccessViews = new UnorderedAccessView[m_UAVsNum];

            for (int i = 0; i < m_NumRTs; ++i)
            {
                renderTargetViews[i] = m_RenderTargets[i].m_RenderTargetView;
            }

            for (int i = 0; i < m_UAVsNum; ++i)
            {
                unorderedAccessViews[i] = m_UAVs[i].m_UnorderedAccessView;
            }

            if (m_UAVsNum > 0)
            {
                context.OutputMerger.SetTargets(depth ? m_DepthStencil.m_DepthStencilView : null, color ? m_NumRTs : 0, color ? unorderedAccessViews : null, color ? renderTargetViews : null);
            }
            else
            {
                if (SurfaceDebugManager.m_GPUDebugOn)
                {
                    UnorderedAccessView[] unorderedAccessViewsDebug = new UnorderedAccessView[1];
                    unorderedAccessViewsDebug[0] = SurfaceDebugManager.m_DebugAppendBuffer.m_UnorderedAccessView;

                    int[] initialLenghts = new int[1];
                    initialLenghts[0] = SurfaceDebugManager.m_FirstCallThisFrame ? 0 : -1;
                    SurfaceDebugManager.m_FirstCallThisFrame = false;

                    context.OutputMerger.SetTargets(depth ? m_DepthStencil.m_DepthStencilView : null, 7, unorderedAccessViewsDebug, initialLenghts, color ? renderTargetViews : null);
                }
                else
                {
                    context.OutputMerger.SetTargets(depth ? m_DepthStencil.m_DepthStencilView : null, renderTargetViews);
                }
            }
            
            context.Rasterizer.SetViewports(m_Viewport);

            context.OutputMerger.DepthStencilState = depth ? m_DepthStencilState : m_DepthStencilStateNoDepth;
        }

        public void Clear(DeviceContext context, Color4 color, bool depth = false)
        {
            for (int i = 0; i < m_NumRTs; ++i)
            {
                context.ClearRenderTargetView(m_RenderTargets[i].m_RenderTargetView, color);
            }

            if (depth && m_DepthStencil.m_DepthStencilView != null)
            {
                context.ClearDepthStencilView(m_DepthStencil.m_DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
            }
        }

        public void AddUAV(TextureObject uavSource)
        {
            m_UAVs[m_UAVsNum++] = uavSource;
        }

        public static void BindNull(DeviceContext context)
        {
            RenderTargetView[] renderTargetViews = new RenderTargetView[8];

            for (int i = 0; i < 8; ++i)
            {
                renderTargetViews[i] = null;
            }

            context.OutputMerger.SetTargets((DepthStencilView)null, renderTargetViews);
        }

        public static RenderTargetSet CreateRenderTargetSet( Device device, RenderTargetDescriptor descriptor )
        {
            return CreateRenderTargetSet(device, descriptor.m_Width, descriptor.m_Height, descriptor.m_Format, descriptor.m_NumSurfaces, descriptor.m_HasDepth);
        }

        public static RenderTargetSet CreateRenderTargetSet( Device device, int width, int height, Format format, int numSurfaces, bool needsDepth )
        {
            RenderTargetSet rt = new RenderTargetSet();

            rt.m_Descriptor = new RenderTargetDescriptor()
            {
                m_Format = format,
                m_HasDepth = needsDepth,
                m_Height = height,
                m_NumSurfaces = numSurfaces,
                m_Width = width
            };

            rt.m_NumRTs = numSurfaces;
            for (int i = 0; i < numSurfaces; ++i)
            {
                rt.m_RenderTargets[i] = TextureObject.CreateTexture(device, width, height, 1, format, false, true);
            }
            
            if (needsDepth)
            {
                rt.m_DepthStencil = TextureObject.CreateTexture(device, width, height, 1, Format.R32_Typeless, true, false);
            }

            rt.m_Viewport = new Viewport(0, 0, width, height, 0.0f, 1.0f);

            if (m_DepthStencilState == null)
            {
                DepthStencilStateDescription dsStateDesc = new DepthStencilStateDescription()
                {
                    IsDepthEnabled = true,
                    IsStencilEnabled = false,
                    DepthWriteMask = DepthWriteMask.All,
                    DepthComparison = Comparison.Less,
                };

                m_DepthStencilState = DepthStencilState.FromDescription(device, dsStateDesc);

                dsStateDesc = new DepthStencilStateDescription()
                {
                    IsDepthEnabled = false,
                    IsStencilEnabled = false,
                    DepthWriteMask = DepthWriteMask.Zero,
                    DepthComparison = Comparison.Always,
                };

                m_DepthStencilStateNoDepth = DepthStencilState.FromDescription(device, dsStateDesc);
            }
            

            return rt;
        }
    }
}

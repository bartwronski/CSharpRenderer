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
    static class ContextHelper
    {
        // todo
        public enum BlendType
        {
            None,
            Additive,
        }

        static BlendState[] m_BlendStates;

        // todo
        public enum DepthConfigurationType
        {
            NoDepth,
            DepthWriteCompare,
            DepthCompare,
        }

        static DepthStencilState[] m_DepthStencilStates;
        
        static ContextHelper()
        {
            m_BlendStates = new BlendState[Enum.GetNames(typeof(BlendType)).Length];
            m_DepthStencilStates = new DepthStencilState[Enum.GetNames(typeof(DepthConfigurationType)).Length];
        }


        public static void ClearCSContext( DeviceContext context )
        {
            context.ComputeShader.SetUnorderedAccessView(null, 0);
            context.ComputeShader.SetUnorderedAccessView(null, 1);
            context.ComputeShader.SetUnorderedAccessView(null, 2);
            context.ComputeShader.SetUnorderedAccessView(null, 3);
            context.ComputeShader.SetUnorderedAccessView(null, 4);
            context.ComputeShader.SetShaderResource(null, 0);
            context.ComputeShader.SetShaderResource(null, 1);
            context.ComputeShader.SetShaderResource(null, 2);
            context.ComputeShader.SetShaderResource(null, 3);
            context.ComputeShader.SetShaderResource(null, 4);
            context.ComputeShader.SetShaderResource(null, 5);
            context.ComputeShader.SetShaderResource(null, 6);
            context.ComputeShader.SetShaderResource(null, 7);
            context.ComputeShader.SetShaderResource(null, 8);
            context.ComputeShader.SetShaderResource(null, 9);
            context.ComputeShader.SetShaderResource(null, 10);
            context.ComputeShader.SetShaderResource(null, 11);
            context.ComputeShader.SetShaderResource(null, 12);
            context.ComputeShader.SetShaderResource(null, 13);
            context.ComputeShader.SetShaderResource(null, 14);
            context.ComputeShader.SetShaderResource(null, 15);
        }

        public static void SetConstantBuffer(DeviceContext context, SlimDX.Direct3D11.Buffer buffer, int slot = 0)
        {
            context.VertexShader.SetConstantBuffer(buffer, slot);
            context.GeometryShader.SetConstantBuffer(buffer, slot);
            context.HullShader.SetConstantBuffer(buffer, slot);
            context.DomainShader.SetConstantBuffer(buffer, slot);
            context.PixelShader.SetConstantBuffer(buffer, slot);
            context.ComputeShader.SetConstantBuffer(buffer, slot);
        }

        public static void ClearSRVs(DeviceContext context)
        {
            for (int i = 0; i < 16; ++i )
            {
                context.PixelShader.SetShaderResource(null, 0);
                context.VertexShader.SetShaderResource(null, 0);
                context.GeometryShader.SetShaderResource(null, 0);
                context.ComputeShader.SetShaderResource(null, 0);
            }
        }

        public static void Initialize(Device device)
        {
            {
                var blendStateDescription = new BlendStateDescription();
                blendStateDescription.RenderTargets[0].BlendEnable = false;
                blendStateDescription.RenderTargets[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                blendStateDescription.RenderTargets[1].BlendEnable = false;
                blendStateDescription.RenderTargets[1].RenderTargetWriteMask = ColorWriteMaskFlags.None;
                blendStateDescription.RenderTargets[2].BlendEnable = false;
                blendStateDescription.RenderTargets[2].RenderTargetWriteMask = ColorWriteMaskFlags.None;
                blendStateDescription.RenderTargets[3].BlendEnable = false;
                blendStateDescription.RenderTargets[3].RenderTargetWriteMask = ColorWriteMaskFlags.None;

                m_BlendStates[(int)BlendType.None] = BlendState.FromDescription(device, blendStateDescription);

                blendStateDescription.RenderTargets[0].BlendEnable = true;
                blendStateDescription.RenderTargets[0].BlendOperation = BlendOperation.Add;
                blendStateDescription.RenderTargets[0].BlendOperationAlpha = BlendOperation.Add;
                blendStateDescription.RenderTargets[0].DestinationBlend = BlendOption.One;
                blendStateDescription.RenderTargets[0].DestinationBlendAlpha = BlendOption.One;
                blendStateDescription.RenderTargets[0].SourceBlend = BlendOption.One;
                blendStateDescription.RenderTargets[0].SourceBlendAlpha = BlendOption.One;
                blendStateDescription.RenderTargets[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

                m_BlendStates[(int)BlendType.Additive] = BlendState.FromDescription(device, blendStateDescription);
            }

            {
                var depthStencilStateDescription = new DepthStencilStateDescription();
                depthStencilStateDescription.DepthComparison = Comparison.Always;
                depthStencilStateDescription.DepthWriteMask = DepthWriteMask.Zero;
                depthStencilStateDescription.IsDepthEnabled = false;
                depthStencilStateDescription.IsStencilEnabled = false;

                m_DepthStencilStates[(int)DepthConfigurationType.NoDepth] = DepthStencilState.FromDescription(device, depthStencilStateDescription);

                depthStencilStateDescription.DepthComparison = Comparison.LessEqual;
                depthStencilStateDescription.DepthWriteMask = DepthWriteMask.All;
                depthStencilStateDescription.IsDepthEnabled = true;

                m_DepthStencilStates[(int)DepthConfigurationType.DepthWriteCompare] = DepthStencilState.FromDescription(device, depthStencilStateDescription);

                depthStencilStateDescription.DepthWriteMask = DepthWriteMask.Zero;

                m_DepthStencilStates[(int)DepthConfigurationType.DepthCompare] = DepthStencilState.FromDescription(device, depthStencilStateDescription);
            }

        }

        public static void SetBlendState(DeviceContext context, BlendType type)
        {
            context.OutputMerger.BlendState = m_BlendStates[(int)type];
        }

        public static void SetDepthStencilState(DeviceContext context, DepthConfigurationType type)
        {
            context.OutputMerger.DepthStencilState = m_DepthStencilStates[(int)type];
        }
    }
}

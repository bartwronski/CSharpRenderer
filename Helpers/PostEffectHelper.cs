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
    static class PostEffectHelper
    {
        public static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp;
            temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        static PostEffectHelper()
        {

        }

        public static void Initialize(Device device, int resX, int resY)
        {
            int numQuads = resX * resY;
            int numIndices = numQuads * 6;
            var indices = new DataStream(sizeof(System.Int32) * numIndices, true, true);

            for (int i = 0; i < numQuads; i++)
            {
                indices.Write(i*4 + 0);
                indices.Write(i*4 + 1);
                indices.Write(i*4 + 2);

                indices.Write(i*4 + 1);
                indices.Write(i*4 + 3);
                indices.Write(i*4 + 2);
            }
            indices.Position = 0;

            // create the vertex layout and buffer
            m_FullResTriangleGridIndexBuffer = new Buffer(device, indices, sizeof(System.Int32) * numIndices, ResourceUsage.Default, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
        
        }

        static Buffer m_FullResTriangleGridIndexBuffer;
        
        public static void RenderFullscreenTriangle(DeviceContext context, string pixelShader)
        {
            context.InputAssembler.InputLayout = null;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
            context.InputAssembler.SetIndexBuffer(null, Format.Unknown, 0);
            context.VertexShader.Set(ShaderManager.GetVertexShader("VertexFullScreenTriangle"));
            context.PixelShader.Set(ShaderManager.GetPixelShader(pixelShader));
            context.Draw(3, 0);
        }

        public static void RenderFullscreenGrid(DeviceContext context, int quadCount)
        {
            context.InputAssembler.InputLayout = null;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
            context.InputAssembler.SetIndexBuffer(m_FullResTriangleGridIndexBuffer, Format.R32_UInt, 0);
            context.DrawIndexed(6 * quadCount, 0, 0);
        }

        public static void Copy(DeviceContext context, RenderTargetSet target, RenderTargetSet source)
        {
            using (new GpuProfilePoint(context, "Copy"))
            {
                source.BindSRV(context, 0);
                target.BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, "Copy");
                RenderTargetSet.BindNull(context);
                ContextHelper.ClearSRVs(context);
            }
        }

        public static void LinearizeDepth(DeviceContext context, RenderTargetSet target, RenderTargetSet source)
        {
            using (new GpuProfilePoint(context, "LinearizeDepth"))
            {
                source.BindDepthAsSRV(context, 0);
                target.BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, "LinearizeDepth");
                RenderTargetSet.BindNull(context);
                ContextHelper.ClearSRVs(context);
            }
        }
    
    }
}

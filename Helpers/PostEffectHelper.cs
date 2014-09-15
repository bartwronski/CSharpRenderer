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
        public static Buffer m_FullResTriangleGridIndexBuffer;
        public static Buffer m_ParticleHelperIndexBuffer;
        public static GPUBufferObject m_RandomNumbersBuffer;

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
            int numParticlesMax = 1024 * 1024; // million particles max
            int numIndicesParticles = numParticlesMax * 6;
            int numRandomNumbers = 1024 * 1024; // million

            var indices = new DataStream(sizeof(System.Int32) * numIndices, true, true);
            var indicesParticles = new DataStream(sizeof(System.Int32) * numIndicesParticles, true, true);

            var randomNumbers = new DataStream(sizeof(System.Single) * numRandomNumbers, true, true);

            for (int i = 0; i < numQuads; i++)
            {
                indices.Write(i * 4 + 0);
                indices.Write(i * 4 + 1);
                indices.Write(i * 4 + 2);

                indices.Write(i * 4 + 1);
                indices.Write(i * 4 + 3);
                indices.Write(i * 4 + 2);
            }
            indices.Position = 0;

            for (int i = 0; i < numParticlesMax; i++)
            {
                indicesParticles.Write(i * 4 + 0);
                indicesParticles.Write(i * 4 + 1);
                indicesParticles.Write(i * 4 + 2);

                indicesParticles.Write(i * 4 + 1);
                indicesParticles.Write(i * 4 + 3);
                indicesParticles.Write(i * 4 + 2);
            }
            indicesParticles.Position = 0;

            System.Random rand = new System.Random();

            for (int i = 0; i < numRandomNumbers; i++)
            {
                randomNumbers.Write((float)rand.NextDouble());
            }
            randomNumbers.Position = 0;

            // create the vertex layout and buffer
            m_FullResTriangleGridIndexBuffer = new Buffer(device, indices, sizeof(System.Int32) * numIndices, ResourceUsage.Default, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            m_ParticleHelperIndexBuffer = new Buffer(device, indicesParticles, sizeof(System.Int32) * numIndicesParticles, ResourceUsage.Default, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            m_RandomNumbersBuffer = GPUBufferObject.CreateBuffer(device, numRandomNumbers, 4, randomNumbers, true, false);

        }

        public static void RenderFullscreenTriangle(DeviceContext context, string pixelShader, bool maxZ = false)
        {
            context.InputAssembler.InputLayout = null;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
            context.InputAssembler.SetIndexBuffer(null, Format.Unknown, 0);
            context.VertexShader.Set(maxZ ? ShaderManager.GetVertexShader("VertexFullScreenTriangleMaxZ") : ShaderManager.GetVertexShader("VertexFullScreenTriangle"));
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

        public static void CopyAlpha(DeviceContext context, RenderTargetSet target, RenderTargetSet source)
        {
            using (new GpuProfilePoint(context, "Copy"))
            {
                source.BindSRV(context, 0);
                target.BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, "CopyAlpha");
                RenderTargetSet.BindNull(context);
                ContextHelper.ClearSRVs(context);
            }
        }

        public static void CopyFrac(DeviceContext context, RenderTargetSet target, RenderTargetSet source)
        {
            using (new GpuProfilePoint(context, "Copy"))
            {
                source.BindSRV(context, 0);
                target.BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, "CopyFrac");
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

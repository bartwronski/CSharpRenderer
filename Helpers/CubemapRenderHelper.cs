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
    static class CubemapRenderHelper
    {
        static CustomConstantBufferInstance m_CurrentViewportBuffer;
        static CustomConstantBufferInstance m_CubemapDownsampleBuffer;

        static CubemapRenderHelper()
        {
        }

        public static void Initialize( Device device)
        {
            m_CurrentViewportBuffer = ShaderManager.CreateConstantBufferInstance("CurrentViewport", device);
            m_CubemapDownsampleBuffer = ShaderManager.CreateConstantBufferInstance("CubemapDownsample", device);
        }

        public static void GenerateCubemapMips(DeviceContext context, string shader, TextureObject targetTexture, TextureObject sourceTexture)
        {
            for (int m = 0; m < targetTexture.m_Mips; ++m)
            {
                for (int i = 0; i < 6; ++i)
                {
                    dynamic cdb = m_CubemapDownsampleBuffer;
                    cdb.g_Mip = (float)m;
                    cdb.g_Face = i;
                    m_CubemapDownsampleBuffer.CompileAndBind(context);

                    RenderTargetSet.BindTextureAsRenderTarget(context, targetTexture.m_ArrayRenderTargetViews[m * 6 + i], null, targetTexture.m_Width >> m, targetTexture.m_Height >> m);
                    context.PixelShader.SetShaderResource(PostEffectHelper.m_RandomNumbersBuffer.m_ShaderResourceView, 39);
                    context.PixelShader.SetShaderResource(sourceTexture.m_ShaderResourceView, 0);
                    PostEffectHelper.RenderFullscreenTriangle(context, shader);
                    RenderTargetSet.BindNull(context);
                }
            }
        }

        public static void RenderCubemap(DeviceContext context, TextureObject targetTexture, TextureObject targetDepth, SimpleSceneWrapper sceneWrapper, Vector3 position, float range, bool depthOnly = false, Color4 clearColor = new Color4())
        {
            //using (new GpuProfilePoint(context, "CubemapRendering"))
            {
                // some hardcoded near plane
                Matrix projectionMatrix = Matrix.PerspectiveFovLH((float)Math.PI / 2.0f, 1.0f, 0.05f, range);

                ContextHelper.SetDepthStencilState(context, ContextHelper.DepthConfigurationType.DepthCompare);

                // set the shaders
                context.VertexShader.Set(ShaderManager.GetVertexShader("VertexScene"));
                context.PixelShader.Set(depthOnly ? null : ShaderManager.GetPixelShader("PixelSceneSimple"));

                dynamic cvpb = m_CurrentViewportBuffer;

                for (int i = 0; i < 6; ++i)
                {
                    Vector3 lookAt = new Vector3();
                    Vector3 upVec = new Vector3();

                    switch (i)
                    {
                        case 0:
                            lookAt = new Vector3(1.0f, 0.0f, 0.0f);
                            upVec = new Vector3(0.0f, 1.0f, 0.0f);
                            break;
                        case 1:
                            lookAt = new Vector3(-1.0f, 0.0f, 0.0f);
                            upVec = new Vector3(0.0f, 1.0f, 0.0f);
                            break;
                        case 2:
                            lookAt = new Vector3(0.0f, 1.0f, 0.0f);
                            upVec = new Vector3(0.0f, 0.0f, -1.0f);
                            break;
                        case 3:
                            lookAt = new Vector3(0.0f, -1.0f, 0.0f);
                            upVec = new Vector3(0.0f, 0.0f, 1.0f);
                            break;
                        case 4:
                            lookAt = new Vector3(0.0f, 0.0f, 1.0f);
                            upVec = new Vector3(0.0f, 1.0f, 0.0f);
                            break;
                        case 5:
                            lookAt = new Vector3(0.0f, 0.0f, -1.0f);
                            upVec = new Vector3(0.0f, 1.0f, 0.0f);
                            break;
                    }

                    Matrix viewMatrix = Matrix.LookAtLH(position, position + lookAt, upVec);

                    cvpb.g_ProjMatrix = projectionMatrix;
                    cvpb.g_ViewMatrix = viewMatrix;
                    cvpb.g_ViewProjMatrix = viewMatrix * projectionMatrix;

                    {
                        context.ClearRenderTargetView(targetTexture.m_ArrayRenderTargetViews[i], clearColor);
                        context.ClearDepthStencilView(targetDepth.m_ArrayDepthStencilViews[i], DepthStencilClearFlags.Depth, 1.0f, 0);

                        RenderTargetSet.BindTextureAsRenderTarget(context, targetTexture.m_ArrayRenderTargetViews[i], targetDepth.m_ArrayDepthStencilViews[i], targetTexture.m_Width, targetTexture.m_Height);

                        m_CurrentViewportBuffer.CompileAndBind(context);

                        // render triangles
                        sceneWrapper.Render(context);

                        RenderTargetSet.BindNull(context);
                    }
                }
                ContextHelper.SetDepthStencilState(context, ContextHelper.DepthConfigurationType.NoDepth);
            }
        }
    }
}

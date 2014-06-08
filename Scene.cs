using System.Windows.Forms;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using SlimDX.Windows;
using Device = SlimDX.Direct3D11.Device;
using Resource = SlimDX.Direct3D11.Resource;
using System.IO;
using Math = System.Math;
using String = System.String;

namespace CSharpRenderer
{
    class Scene
    {
        Camera              m_ViewportCamera; 
        Camera              m_ShadowCamera;

        RenderTargetSet.RenderTargetDescriptor m_FullResDescriptor;
        RenderTargetSet.RenderTargetDescriptor m_FullResAndDepthDescriptor;
        RenderTargetSet.RenderTargetDescriptor m_ResolvedColorDescriptor;
        RenderTargetSet.RenderTargetDescriptor m_LinearDepthDescriptor;
        RenderTargetSet.RenderTargetDescriptor m_SSAODescriptor;

        SimpleSceneWrapper  m_SimpleSceneWrapper;

        ScatterDOFPass                  m_ScatterDOFPass;
        ResolveHDRPass                  m_ResolveHDRPass;
        FxaaPass                        m_FxaaPass;
        ResolveMotionVectorsPass        m_ResolveMotionVectorsPass;
        ResolveTemporalMotionBasedPass  m_ResolveTemporalPass;
        SSAOEffectPass                  m_SSAOPass;
        GlobalIlluminationRenderer      m_GIRenderer;
        LuminanceCalculations           m_LuminanceCalculations;
        ShadowEVSMGenerator             m_ShadowEVSMGenerator;

        RenderTargetSet                 m_ResolvedShadow;

        int m_ResolutionX;
        int m_ResolutionY;

        CustomConstantBufferInstance m_ForwardPassBuffer;
        CustomConstantBufferInstance m_CurrentViewportBuffer;
        CustomConstantBufferInstance m_ViewportConstantBuffer;
        CustomConstantBufferInstance m_PostEffectsConstantBuffer;
        
        bool m_ShadowsInitialized;

        public Scene()
        {
            m_ViewportCamera = new Camera();
            m_ShadowCamera = new Camera(true);
            m_SimpleSceneWrapper = new SimpleSceneWrapper();
            m_ShadowsInitialized = false;
        }

        public void Initialize(Device device, Form form, Panel panel, int resolutionX, int resolutionY)
        {
            m_ResolutionX = resolutionX;
            m_ResolutionY = resolutionY;

            m_FullResDescriptor = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = Format.R16G16B16A16_Float,
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = resolutionY,
                m_Width = resolutionX
            };

            m_FullResAndDepthDescriptor = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = Format.R16G16B16A16_Float,
                m_HasDepth = true,
                m_NumSurfaces = 1,
                m_Height = resolutionY,
                m_Width = resolutionX
            };

            m_ResolvedColorDescriptor = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = Format.R8G8B8A8_UNorm,
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = resolutionY,
                m_Width = resolutionX
            };
            m_LinearDepthDescriptor = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = Format.R32_Float,
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = resolutionY,
                m_Width = resolutionX
            };
            m_SSAODescriptor = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = Format.R8_UNorm,
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = resolutionY,
                m_Width = resolutionX
            };

            m_SimpleSceneWrapper.Initialize(device, "sponza");

            TemporalSurfaceManager.InitializeRenderTarget("ResolvedColor", m_ResolvedColorDescriptor );

            TemporalSurfaceManager.InitializeRenderTarget("MotionVectors",
                new RenderTargetSet.RenderTargetDescriptor()
                {
                    m_Format = Format.R16G16_Float,
                    m_HasDepth = false,
                    m_NumSurfaces = 1,
                    m_Height = resolutionY,
                    m_Width = resolutionX
                });


            m_PostEffectsConstantBuffer = ShaderManager.CreateConstantBufferInstance("PostEffects", device);
            m_ForwardPassBuffer = ShaderManager.CreateConstantBufferInstance("ForwardPassBuffer", device);
            m_ViewportConstantBuffer = ShaderManager.CreateConstantBufferInstance("GlobalViewportBuffer", device);
            m_CurrentViewportBuffer = ShaderManager.CreateConstantBufferInstance("CurrentViewport", device);

            Vector3 min, max;
            m_SimpleSceneWrapper.GetSceneBounds(out min, out max);

            m_GIRenderer = new GlobalIlluminationRenderer(device, min, max);

            // Init passes
            m_ScatterDOFPass = new ScatterDOFPass();
            m_ScatterDOFPass.Initialize(device, resolutionX, resolutionY);

            m_ResolveHDRPass = new ResolveHDRPass();

            m_ResolveMotionVectorsPass = new ResolveMotionVectorsPass();

            m_ResolveTemporalPass = new ResolveTemporalMotionBasedPass();

            m_FxaaPass = new FxaaPass();

            m_SSAOPass = new SSAOEffectPass(device, resolutionX, resolutionY);

            m_LuminanceCalculations = new LuminanceCalculations(device, resolutionX, resolutionY);

            m_ShadowEVSMGenerator = new ShadowEVSMGenerator();

            m_ViewportCamera.BindToInput(form, panel);
        }

        public void RenderFrame(DeviceContext context, float timeElapsed, RenderTargetSet targetRT)
        {
            ShaderManager.BindSamplerStates(context);
            RenderTargetSet currentFrameMainBuffer = RenderTargetManager.RequestRenderTargetFromPool(m_FullResAndDepthDescriptor);
            RenderTargetSet linearDepth = RenderTargetManager.RequestRenderTargetFromPool(m_LinearDepthDescriptor);
            RenderTargetSet ssaoRT = RenderTargetManager.RequestRenderTargetFromPool(m_SSAODescriptor);

            if (!m_ShadowsInitialized)
            {
                m_ShadowCamera.m_CameraForward = new Vector3(-0.15f, -1.0f, 0.3f);
                m_ShadowCamera.m_CameraForward.Normalize();
                
                Vector3 min, max;
                m_SimpleSceneWrapper.GetSceneBounds(out min, out max);

                Vector3 sceneTop = (min + max) * 0.5f;
                sceneTop.Y = max.Y;

                m_ShadowCamera.m_OrthoZoomX = (max.X - min.X) * 0.7f; // some overlap
                m_ShadowCamera.m_OrthoZoomY = (max.Y - min.Y) * 0.7f;

                m_ShadowCamera.m_CameraPosition = sceneTop - m_ShadowCamera.m_CameraForward * 50.0f;
                m_ShadowCamera.m_CameraUp = new Vector3(0, 0, 1);
            }

            CalculateAndUpdateConstantBuffer(context);

            if (!m_ShadowsInitialized)
            {
                m_ResolvedShadow = m_ShadowEVSMGenerator.RenderShadows(context, m_SimpleSceneWrapper);
                m_ShadowsInitialized = true;
            }

            if (false)
            {
                m_GIRenderer.PartialGIUpdate(context, m_SimpleSceneWrapper, m_ResolvedShadow);
            }

            m_CurrentViewportBuffer.Bind(context);

            using (new GpuProfilePoint(context, "DepthPrepass"))
            {
                // set the shaders
                context.VertexShader.Set(ShaderManager.GetVertexShader("VertexScene"));
                context.PixelShader.Set(null);

                currentFrameMainBuffer.Clear(context, new Color4(1.0f, 1.0f, 1.0f, 1.5f), true);
                currentFrameMainBuffer.BindAsRenderTarget(context, true, false);
                ContextHelper.SetDepthStencilState(context, ContextHelper.DepthConfigurationType.DepthWriteCompare);

                // render triangles
                m_SimpleSceneWrapper.RenderNoMaterials(context);

                ContextHelper.SetDepthStencilState(context, ContextHelper.DepthConfigurationType.NoDepth);

                RenderTargetSet.BindNull(context);
            }

            RenderTargetSet motionVectorsSurface = TemporalSurfaceManager.GetRenderTargetCurrent("MotionVectors");
            RenderTargetSet motionVectorsSurfacePrevious = TemporalSurfaceManager.GetRenderTargetHistory("MotionVectors");
            m_ResolveMotionVectorsPass.ExecutePass(context, motionVectorsSurface, currentFrameMainBuffer);

            PostEffectHelper.LinearizeDepth(context, linearDepth, currentFrameMainBuffer);
            m_SSAOPass.ExecutePass(context, ssaoRT, linearDepth, motionVectorsSurface);

            using (new GpuProfilePoint(context, "MainForwardRender"))
            {
                // set the shaders
                context.VertexShader.Set(ShaderManager.GetVertexShader("VertexScene"));
                context.PixelShader.Set(ShaderManager.GetPixelShader("PixelScene"));

                currentFrameMainBuffer.BindAsRenderTarget(context, true);

                m_ResolvedShadow.BindSRV(context, 0);
                ssaoRT.BindSRV(context, 1);
                context.PixelShader.SetShaderResource(m_GIRenderer.m_GIVolumeR.m_ShaderResourceView, 5);
                context.PixelShader.SetShaderResource(m_GIRenderer.m_GIVolumeG.m_ShaderResourceView, 6);
                context.PixelShader.SetShaderResource(m_GIRenderer.m_GIVolumeB.m_ShaderResourceView, 7);

                ContextHelper.SetDepthStencilState(context, ContextHelper.DepthConfigurationType.DepthCompare);

                // render triangles
                m_SimpleSceneWrapper.Render(context);

                ContextHelper.SetDepthStencilState(context, ContextHelper.DepthConfigurationType.NoDepth);

                RenderTargetSet.BindNull(context);
            }
            
            using (new GpuProfilePoint(context, "PostEffects"))
            {
                RenderTargetSet postEffectSurfacePong = RenderTargetManager.RequestRenderTargetFromPool(m_FullResDescriptor);

                RenderTargetSet source, dest;
                source = currentFrameMainBuffer;
                dest = postEffectSurfacePong;

                dynamic ppcb = m_PostEffectsConstantBuffer;
                if (ppcb.dofCoCScale > 0.0f)
                {
                    m_ScatterDOFPass.ExecutePass(context, dest, currentFrameMainBuffer, currentFrameMainBuffer);

                    PostEffectHelper.Swap(ref source, ref dest);
                }

                RenderTargetSet luminanceTexture = m_LuminanceCalculations.ExecutePass(context, source);

                RenderTargetSet resolvedCurrent = TemporalSurfaceManager.GetRenderTargetCurrent("ResolvedColor");
                RenderTargetSet resolvedHistory = TemporalSurfaceManager.GetRenderTargetHistory("ResolvedColor");

                m_ResolveHDRPass.ExecutePass(context, resolvedCurrent, source, luminanceTexture);

                RenderTargetSet resolvedTemporal = RenderTargetManager.RequestRenderTargetFromPool(m_ResolvedColorDescriptor);
                m_ResolveTemporalPass.ExecutePass(context, resolvedTemporal, resolvedCurrent, resolvedHistory, motionVectorsSurface, motionVectorsSurfacePrevious);
                m_FxaaPass.ExecutePass(context, targetRT, resolvedTemporal);

                RenderTargetManager.ReleaseRenderTargetToPool(resolvedTemporal);
                RenderTargetManager.ReleaseRenderTargetToPool(postEffectSurfacePong);
            }

            RenderTargetManager.ReleaseRenderTargetToPool(ssaoRT);
            RenderTargetManager.ReleaseRenderTargetToPool(linearDepth);
            RenderTargetManager.ReleaseRenderTargetToPool(currentFrameMainBuffer);

        }

        private void CalculateAndUpdateConstantBuffer(DeviceContext context)
        {
            Matrix previousFrameViewProjMatrix = m_ViewportCamera.m_ViewProjectionMatrix;
            m_ViewportCamera.TickCamera(0.1f);

            dynamic mcb = m_ForwardPassBuffer;
            dynamic vcb = m_ViewportConstantBuffer;
            dynamic ppcb = m_PostEffectsConstantBuffer;
            dynamic cvpb = m_CurrentViewportBuffer;

            m_ViewportCamera.CalculateMatrices();
            m_ShadowCamera.CalculateMatrices();

            // Temporal component of matrix
            Matrix temporalJitter = Matrix.Identity;
            if (vcb.temporalAA > 0.5f)
            {
                System.Random rand = new System.Random();
                float translationOffset = (TemporalSurfaceManager.GetCurrentPhase("ResolvedColor") == 0) ? 0.5f : -0.5f;
                float translationOffsetX = translationOffset;
                float translationOffsetY = translationOffset;
                temporalJitter = Matrix.Translation(translationOffsetX / (float)m_ResolutionX, translationOffsetY / (float)m_ResolutionY, 0.0f);
            }
            m_ScatterDOFPass.m_DebugBokeh = ppcb.debugBokeh > 0.5f;

            Vector3 sceneMin, sceneMax;
            m_SimpleSceneWrapper.GetSceneBounds(out sceneMin, out sceneMax);

            vcb.viewProjMatrixPrevFrame = previousFrameViewProjMatrix;
            cvpb.projMatrix = m_ViewportCamera.m_ProjectionMatrix;
            cvpb.viewMatrix = m_ViewportCamera.m_WorldToView;
            cvpb.invViewProjMatrix = m_ViewportCamera.m_ViewProjectionMatrix;
            cvpb.invViewProjMatrix.Invert();
            cvpb.viewProjMatrix = m_ViewportCamera.m_ViewProjectionMatrix * temporalJitter;
            vcb.worldEyePos = new Vector4(m_ViewportCamera.m_CameraPosition, 1.0f);
            vcb.worldBoundsMin = new Vector4(sceneMin, 0.0f);
            vcb.worldBoundsMax = new Vector4(sceneMax, 0.0f);
            Vector3 invRange = new Vector3(1.0f / (sceneMax.X - sceneMin.X), 1.0f / (sceneMax.Y - sceneMin.Y), 1.0f / (sceneMax.Z - sceneMin.Z));
            vcb.worldBoundsInvRange = new Vector4(invRange, 0.0f);
            vcb.zNear = m_ViewportCamera.m_NearZ;
            vcb.zFar = m_ViewportCamera.m_FarZ;
            vcb.screenSize = new Vector4((float)m_ResolutionX, (float)m_ResolutionY, 1.0f / (float)m_ResolutionX, 1.0f / (float)m_ResolutionY);
            vcb.screenSizeHalfRes = new Vector4((float)m_ResolutionX / 2.0f, (float)m_ResolutionY / 2.0f, 2.0f / (float)m_ResolutionX, 2.0f / (float)m_ResolutionY);
            vcb.reprojectInfo = new Vector4(
                -2.0f / ((float)m_ResolutionX*m_ViewportCamera.m_ProjectionMatrix.M11),                  
                -2.0f / ((float)m_ResolutionY*m_ViewportCamera.m_ProjectionMatrix.M22),
                (1.0f - m_ViewportCamera.m_ProjectionMatrix.M13) / m_ViewportCamera.m_ProjectionMatrix.M11,
                (1.0f + m_ViewportCamera.m_ProjectionMatrix.M23) / m_ViewportCamera.m_ProjectionMatrix.M22);
            vcb.reprojectInfoFromInt = vcb.reprojectInfo + new Vector4(0.0f, 0.0f, vcb.reprojectInfo.X * 0.5f, vcb.reprojectInfo.Y * 0.5f);
            mcb.shadowViewProjMatrix = m_ShadowCamera.m_ViewProjectionMatrix;
            mcb.shadowInvViewProjMatrix = m_ShadowCamera.m_ViewProjectionMatrix;
            mcb.shadowInvViewProjMatrix.Invert();
            mcb.lightDir = new Vector4(-m_ShadowCamera.m_CameraForward, 1.0f);

            m_ViewportConstantBuffer.CompileAndBind(context);
            m_ForwardPassBuffer.CompileAndBind(context);
            m_CurrentViewportBuffer.CompileAndBind(context);
            m_PostEffectsConstantBuffer.CompileAndBind(context);
        }
    }
}

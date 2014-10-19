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
using System;

namespace CSharpRenderer
{
    class Scene
    {
        Camera              m_ViewportCamera; 
        Camera              m_ShadowCamera;

        RenderTargetSet.RenderTargetDescriptor m_FullResDescriptor;
        RenderTargetSet.RenderTargetDescriptor m_HalfResDescriptor;
        RenderTargetSet.RenderTargetDescriptor m_FullResAndDepthDescriptor;
        RenderTargetSet.RenderTargetDescriptor m_ResolvedColorDescriptor;
        RenderTargetSet.RenderTargetDescriptor m_SSAODescriptor;

        SimpleSceneWrapper  m_SimpleSceneWrapper;

        ScatterDOFPass                  m_ScatterDOFPass;
        ResolveHDRPass                  m_ResolveHDRPass;
        FxaaPass                        m_FxaaPass;
        ResolveMotionVectorsPass        m_ResolveMotionVectorsPass;
        ResolveTemporalMotionBasedPass  m_ResolveTemporalPass;
        SSAOEffectPass                  m_SSAOPass;
        SSReflectionsEffectPass         m_SSReflectionsPass;
        GlobalIlluminationRenderer      m_GIRenderer;
        LuminanceCalculations           m_LuminanceCalculations;
        ShadowEVSMGenerator             m_ShadowEVSMGenerator;
        DepthOperationsPass             m_DepthOperationsPass;
        VolumetricFog                   m_VolumetricFog;

        RenderTargetSet                 m_ResolvedShadow;

        int m_ResolutionX;
        int m_ResolutionY;
        
        const int POISSON_SAMPLE_NUM = 8;
        float[] POISSON_SAMPLES;

        CustomConstantBufferInstance m_ForwardPassBuffer;
        CustomConstantBufferInstance m_CurrentViewportBuffer;
        CustomConstantBufferInstance m_ViewportConstantBuffer;
        CustomConstantBufferInstance m_PostEffectsConstantBuffer;

        TextureObject                m_CubeObject;
        TextureObject                m_CubeObjectFiltered;
        TextureObject                m_CubeDepthObject;
        
        bool m_ShadowsInitialized;

        DateTime m_StartTime;

        public Scene()
        {
            m_ViewportCamera = new Camera();
            m_ViewportCamera.m_CameraPosition.Y += 1.0f;
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

            m_SSAODescriptor = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = Format.R8_UNorm,
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = resolutionY / 2,
                m_Width = resolutionX / 2
            };

            m_HalfResDescriptor = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = Format.R16G16B16A16_Float,
                m_HasDepth = false,
                m_NumSurfaces = 1,
                m_Height = resolutionY / 2,
                m_Width = resolutionX / 2
            };

            m_SimpleSceneWrapper.Initialize(device, "sponza");

            TemporalSurfaceManager.InitializeRenderTarget("SceneMainRender", m_FullResAndDepthDescriptor);
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

            m_DepthOperationsPass = new DepthOperationsPass(resolutionX, resolutionY);

            m_SSAOPass = new SSAOEffectPass(device, resolutionX / 2, resolutionY / 2);

            m_SSReflectionsPass = new SSReflectionsEffectPass(device, resolutionX / 2, resolutionY / 2);

            m_LuminanceCalculations = new LuminanceCalculations(device, resolutionX, resolutionY);

            m_ShadowEVSMGenerator = new ShadowEVSMGenerator();

            m_VolumetricFog = new VolumetricFog();
            m_VolumetricFog.Initialize(device);

            m_StartTime = DateTime.Now;

            m_ViewportCamera.BindToInput(form, panel);

            POISSON_SAMPLES = new float[POISSON_SAMPLE_NUM * 2]
                {
                    0.655897408497f, 0.95575996511f, 
                    0.944576716895f, 0.455478901428f, 
                    0.203683172197f, 0.0177149729234f, 
                    0.383628747896f, 0.379284571357f, 
                    0.945473563065f, 0.779431977074f, 
                    0.570420562284f, 0.576156483093f, 
                    0.674354533289f, 0.286346887653f, 
                    0.347776132801f, 0.734938485879f, 
                };

            m_CubeObject = TextureObject.CreateCubeTexture(device, 256, 256, 1, Format.R16G16B16A16_Float, false, true);
            m_CubeObjectFiltered = TextureObject.CreateCubeTexture(device, 256, 256, 7, Format.R16G16B16A16_Float, false, true);
            m_CubeDepthObject = TextureObject.CreateCubeTexture(device, 256, 256, 1, Format.R32_Typeless, true, true);
        }

        public void RenderFrame(DeviceContext context, double timeElapsed, RenderTargetSet targetRT)
        {
            ShaderManager.BindSamplerStates(context);
            PerlinNoiseRenderHelper.BindTextures(context);
            RenderTargetSet currentFrameMainBuffer = TemporalSurfaceManager.GetRenderTargetCurrent("SceneMainRender");
            RenderTargetSet ssaoRT = RenderTargetManager.RequestRenderTargetFromPool(m_SSAODescriptor);
            bool useTemporal = !DebugManager.m_DisabledTemporal;

            if (!m_ShadowsInitialized)
            {
                InitializeShadowmapAndCubemaps(context, timeElapsed);
            }

            CalculateAndUpdateConstantBuffer(context, timeElapsed);

            if (false)
            {
                m_GIRenderer.PartialGIUpdate(context, m_SimpleSceneWrapper, m_ResolvedShadow);
            }

            m_CurrentViewportBuffer.Bind(context);

            using (new GpuProfilePoint(context, "DepthPrepass"))
            {
                // set the shaders
                context.VertexShader.Set(ShaderManager.GetVertexShader("VertexScene"));
                context.PixelShader.Set(ShaderManager.GetPixelShader("DepthNormalPrepass"));

                currentFrameMainBuffer.Clear(context, new Color4(1.0f, 1.0f, 1.0f, 1.5f), true);
                currentFrameMainBuffer.BindAsRenderTarget(context, true, true);
                ContextHelper.SetDepthStencilState(context, ContextHelper.DepthConfigurationType.DepthWriteCompare);

                // render triangles
                m_SimpleSceneWrapper.Render(context);

                ContextHelper.SetDepthStencilState(context, ContextHelper.DepthConfigurationType.NoDepth);

                RenderTargetSet.BindNull(context);

                DebugManager.RegisterDebug(context, "NormalsPrepass", currentFrameMainBuffer);
            }

            RenderTargetSet motionVectorsSurface = TemporalSurfaceManager.GetRenderTargetCurrent("MotionVectors");
            RenderTargetSet motionVectorsSurfacePrevious = TemporalSurfaceManager.GetRenderTargetHistory("MotionVectors");
            m_ResolveMotionVectorsPass.ExecutePass(context, motionVectorsSurface, currentFrameMainBuffer);
            DebugManager.RegisterDebug(context, "MotionVectors", motionVectorsSurface);
            
            m_DepthOperationsPass.ExecutePass(context, currentFrameMainBuffer);
            RenderTargetSet linearDepth = m_DepthOperationsPass.m_LinearDepth;

            m_SSAOPass.ExecutePass(context, ssaoRT, m_DepthOperationsPass.m_HalfLinearDepth, motionVectorsSurface, m_DepthOperationsPass.m_HalfNormals, m_DepthOperationsPass);
            m_SSReflectionsPass.ExecutePass(context, m_DepthOperationsPass.m_HalfLinearDepth, motionVectorsSurface, m_DepthOperationsPass.m_HalfNormals, TemporalSurfaceManager.GetRenderTargetHistory("SceneMainRender"), m_ViewportCamera, m_DepthOperationsPass);
            m_VolumetricFog.RenderVolumetricFog(context, m_ResolvedShadow.m_RenderTargets[0], m_GIRenderer);

            using (new GpuProfilePoint(context, "MainForwardRender"))
            {
                // set the shaders
                context.VertexShader.Set(ShaderManager.GetVertexShader("VertexScene"));
                context.PixelShader.Set(ShaderManager.GetPixelShader("PixelScene"));

                currentFrameMainBuffer.BindAsRenderTarget(context, true);

                m_ResolvedShadow.BindSRV(context, 0);
                ssaoRT.BindSRV(context, 1);
                TemporalSurfaceManager.GetRenderTargetCurrent("SSReflections").BindSRV(context, 2);
                m_DepthOperationsPass.m_BilateralUpsampleOffsets.BindSRV(context, 10);
                
                context.PixelShader.SetShaderResource(m_GIRenderer.m_GIVolumeR.m_ShaderResourceView, 5);
                context.PixelShader.SetShaderResource(m_GIRenderer.m_GIVolumeG.m_ShaderResourceView, 6);
                context.PixelShader.SetShaderResource(m_GIRenderer.m_GIVolumeB.m_ShaderResourceView, 7);

                context.PixelShader.SetShaderResource(m_VolumetricFog.m_ScatteringTexture.m_ShaderResourceView, 8);

                context.PixelShader.SetShaderResource(m_CubeObjectFiltered.m_ShaderResourceView, 11);

                ContextHelper.SetDepthStencilState(context, ContextHelper.DepthConfigurationType.DepthCompare);

                LightingLUTHelper.BindTextures(context);

                // render triangles
                m_SimpleSceneWrapper.Render(context);

                // render sky
                PostEffectHelper.RenderFullscreenTriangle(context, "Sky", true);

                ContextHelper.SetDepthStencilState(context, ContextHelper.DepthConfigurationType.NoDepth);

                RenderTargetSet.BindNull(context);
            }
            RenderTargetManager.ReleaseRenderTargetToPool(ssaoRT);
            
            using (new GpuProfilePoint(context, "PostEffects"))
            {
                RenderTargetSet postEffectSurfacePong = RenderTargetManager.RequestRenderTargetFromPool(m_FullResDescriptor);

                RenderTargetSet source, dest;
                source = currentFrameMainBuffer;
                dest = postEffectSurfacePong;

                dynamic ppcb = m_PostEffectsConstantBuffer;
                if (ppcb.g_DofCoCScale > 0.0f)
                {
                    m_ScatterDOFPass.ExecutePass(context, dest, currentFrameMainBuffer, currentFrameMainBuffer);

                    PostEffectHelper.Swap(ref source, ref dest);
                }

                RenderTargetSet luminanceTexture = m_LuminanceCalculations.ExecutePass(context, source);

                RenderTargetSet resolvedCurrent = TemporalSurfaceManager.GetRenderTargetCurrent("ResolvedColor");
                RenderTargetSet resolvedHistory = TemporalSurfaceManager.GetRenderTargetHistory("ResolvedColor");

                m_ResolveHDRPass.ExecutePass(context, resolvedCurrent, source, luminanceTexture);
                DebugManager.RegisterDebug(context, "ResolvedNoTemporal", resolvedCurrent);

                RenderTargetSet resolvedTemporal = RenderTargetManager.RequestRenderTargetFromPool(m_ResolvedColorDescriptor);
                RenderTargetSet finalResolvedTarget = resolvedCurrent;
                if (useTemporal)
                {
                    m_ResolveTemporalPass.ExecutePass(context, resolvedTemporal, resolvedCurrent, resolvedHistory, motionVectorsSurface, motionVectorsSurfacePrevious, true);
                    finalResolvedTarget = resolvedTemporal;
                }

                DebugManager.RegisterDebug(context, "ResolvedNoFXAA", finalResolvedTarget);
                m_FxaaPass.ExecutePass(context, targetRT, finalResolvedTarget);

                RenderTargetManager.ReleaseRenderTargetToPool(resolvedTemporal);
                RenderTargetManager.ReleaseRenderTargetToPool(postEffectSurfacePong);
            }

        }

        private void InitializeShadowmapAndCubemaps(DeviceContext context, double timeElapsed)
        {

            m_ShadowCamera.m_CameraForward = new Vector3(-0.15f, -1.0f, 0.15f);
            m_ShadowCamera.m_CameraForward.Normalize();

            Vector3 min, max;
            m_SimpleSceneWrapper.GetSceneBounds(out min, out max);

            Vector3 sceneTop = (min + max) * 0.5f;
            sceneTop.Y = max.Y;

            m_ShadowCamera.m_OrthoZoomX = (max.X - min.X) * 0.7f; // some overlap
            m_ShadowCamera.m_OrthoZoomY = (max.Z - min.Z) * 0.7f;

            m_ShadowCamera.m_CameraPosition = sceneTop - m_ShadowCamera.m_CameraForward * 50.0f;
            m_ShadowCamera.m_CameraUp = new Vector3(0, 0, 1);

            CalculateAndUpdateConstantBuffer(context, timeElapsed);

            m_ResolvedShadow = m_ShadowEVSMGenerator.RenderShadows(context, m_SimpleSceneWrapper);

            context.PixelShader.SetShaderResource(m_GIRenderer.m_GIVolumeR.m_ShaderResourceView, 5);
            context.PixelShader.SetShaderResource(m_GIRenderer.m_GIVolumeG.m_ShaderResourceView, 6);
            context.PixelShader.SetShaderResource(m_GIRenderer.m_GIVolumeB.m_ShaderResourceView, 7);
            m_ResolvedShadow.BindSRV(context, 0);

            CubemapRenderHelper.RenderCubemap(context, m_CubeObject, m_CubeDepthObject, m_SimpleSceneWrapper, m_ViewportCamera.m_CameraPosition, 256.0f, false, new Color4(1.0f, 1.0f, 1.0f, 1.5f));
            CubemapRenderHelper.GenerateCubemapMips(context, "PrefilterEnvLighting", m_CubeObjectFiltered, m_CubeObject);

            m_ShadowsInitialized = true;
        }

        private void CalculateAndUpdateConstantBuffer(DeviceContext context, double tick)
        {
            Matrix previousFrameViewProjMatrix = m_ViewportCamera.m_ViewProjectionMatrix;
            m_ViewportCamera.TickCamera(tick);

            dynamic mcb = m_ForwardPassBuffer;
            dynamic vcb = m_ViewportConstantBuffer;
            dynamic ppcb = m_PostEffectsConstantBuffer;
            dynamic cvpb = m_CurrentViewportBuffer;

            m_ViewportCamera.CalculateMatrices();
            m_ShadowCamera.CalculateMatrices();

            // Temporal component of matrix
            Matrix temporalJitter = Matrix.Identity;

            if (!DebugManager.m_DisabledTemporal)
            {
                temporalJitter = Matrix.Translation((POISSON_SAMPLES[(Program.m_FrameNumber) % POISSON_SAMPLE_NUM * 2 + 0] * 2.0f - 1.0f) / (float)m_ResolutionX, (POISSON_SAMPLES[(Program.m_FrameNumber) % POISSON_SAMPLE_NUM * 2 + 1] * 2.0f - 1.0f) / (float)m_ResolutionY, 0.0f);
            }
            m_ScatterDOFPass.m_DebugBokeh = ppcb.g_DebugBokeh > 0.5f;

            Vector3 sceneMin, sceneMax;
            m_SimpleSceneWrapper.GetSceneBounds(out sceneMin, out sceneMax);

            vcb.g_ViewProjMatrixPrevFrame = previousFrameViewProjMatrix;
            cvpb.g_ProjMatrix = m_ViewportCamera.m_ProjectionMatrix;
            cvpb.g_ViewMatrix = m_ViewportCamera.m_WorldToView;
            cvpb.g_InvViewProjMatrix = m_ViewportCamera.m_ViewProjectionMatrix;
            cvpb.g_InvViewProjMatrix.Invert();
            vcb.g_ReprojectProjToPrevFrame = cvpb.g_InvViewProjMatrix * previousFrameViewProjMatrix;
            cvpb.g_ViewProjMatrix = m_ViewportCamera.m_ViewProjectionMatrix * temporalJitter;

            Matrix viewToWorldMatrix = m_ViewportCamera.m_WorldToView;
            viewToWorldMatrix.Invert();

            float projWidth = m_ViewportCamera.m_ProjectionMatrix.M11;
            float projHeight = m_ViewportCamera.m_ProjectionMatrix.M22;

            vcb.g_EyeXAxis = Vector4.Transform(new Vector4(1.0f / projWidth, 0, 0, 0), viewToWorldMatrix);
            vcb.g_EyeYAxis = Vector4.Transform(new Vector4(0, 1.0f / projHeight, 0, 0), viewToWorldMatrix);
            vcb.g_EyeZAxis = Vector4.Transform(new Vector4(0, 0, 1, 0), viewToWorldMatrix);

            vcb.g_WorldEyePos = new Vector4(m_ViewportCamera.m_CameraPosition, 1.0f);
            vcb.g_WorldBoundsMin = new Vector4(sceneMin, 0.0f);
            vcb.g_WorldBoundsMax = new Vector4(sceneMax, 0.0f);
            Vector3 invRange = new Vector3(1.0f / (sceneMax.X - sceneMin.X), 1.0f / (sceneMax.Y - sceneMin.Y), 1.0f / (sceneMax.Z - sceneMin.Z));
            vcb.g_WorldBoundsInvRange = new Vector4(invRange, 0.0f);
            vcb.g_zNear = m_ViewportCamera.m_NearZ;
            vcb.g_zFar = m_ViewportCamera.m_FarZ;
            vcb.g_FrameJitter = (TemporalSurfaceManager.GetCurrentPhase("ResolvedColor") == 0) ? 1.0f : 0.0f;
            vcb.g_ScreenSize = new Vector4((float)m_ResolutionX, (float)m_ResolutionY, 1.0f / (float)m_ResolutionX, 1.0f / (float)m_ResolutionY);
            vcb.g_ScreenSizeHalfRes = new Vector4((float)m_ResolutionX / 2.0f, (float)m_ResolutionY / 2.0f, 2.0f / (float)m_ResolutionX, 2.0f / (float)m_ResolutionY);
            vcb.g_ReprojectInfo = new Vector4(
                -2.0f / ((float)m_ResolutionX*m_ViewportCamera.m_ProjectionMatrix.M11),                  
                -2.0f / ((float)m_ResolutionY*m_ViewportCamera.m_ProjectionMatrix.M22),
                (1.0f - m_ViewportCamera.m_ProjectionMatrix.M13) / m_ViewportCamera.m_ProjectionMatrix.M11,
                (1.0f + m_ViewportCamera.m_ProjectionMatrix.M23) / m_ViewportCamera.m_ProjectionMatrix.M22);
            vcb.g_ReprojectInfoFromInt = vcb.g_ReprojectInfo + new Vector4(0.0f, 0.0f, vcb.g_ReprojectInfo.X * 0.5f, vcb.g_ReprojectInfo.Y * 0.5f);
            vcb.g_ReprojectInfoHalfRes = new Vector4(
                -2.0f / ((float)(m_ResolutionX/2) * m_ViewportCamera.m_ProjectionMatrix.M11),
                -2.0f / ((float)(m_ResolutionY/2) * m_ViewportCamera.m_ProjectionMatrix.M22),
                (1.0f - m_ViewportCamera.m_ProjectionMatrix.M13) / m_ViewportCamera.m_ProjectionMatrix.M11,
                (1.0f + m_ViewportCamera.m_ProjectionMatrix.M23) / m_ViewportCamera.m_ProjectionMatrix.M22);
            vcb.g_ReprojectInfoHalfResFromInt = vcb.g_ReprojectInfoHalfRes + new Vector4(0.0f, 0.0f, vcb.g_ReprojectInfoHalfRes.X * 0.5f, vcb.g_ReprojectInfoHalfRes.Y * 0.5f);

            mcb.g_ShadowViewProjMatrix = m_ShadowCamera.m_ViewProjectionMatrix;
            mcb.g_ShadowInvViewProjMatrix = m_ShadowCamera.m_ViewProjectionMatrix;
            mcb.g_ShadowInvViewProjMatrix.Invert();
            mcb.g_LightDir = new Vector4(-m_ShadowCamera.m_CameraForward, 1.0f);
            mcb.g_LightColor = new Vector4(1.0f, 0.95f, 0.9f, 0.0f);
            
            float time = (float)Program.m_Time;
            mcb.g_LocalPointLightPosition = new Vector4((float)Math.Sin(0.05f * time) * 10.0f, 2.0f, (float)Math.Cos(time * 0.15f) * 5.8f, 0.0f);
            mcb.g_LocalPointLightColor = new Vector4(1, 0, 0, 0);

            m_ViewportConstantBuffer.CompileAndBind(context);
            m_ForwardPassBuffer.CompileAndBind(context);
            m_CurrentViewportBuffer.CompileAndBind(context);
            m_PostEffectsConstantBuffer.CompileAndBind(context);
        }
    }
}

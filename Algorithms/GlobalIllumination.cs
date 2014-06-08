using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using Device = SlimDX.Direct3D11.Device;
using Resource = SlimDX.Direct3D11.Resource;
using SlimDX;

namespace CSharpRenderer
{
    class GlobalIlluminationRenderer
    {
        TextureObject                   m_CubeObject;
        TextureObject                   m_CubeObjectDepth;

        public TextureObject            m_GIVolumeR;
        public TextureObject            m_GIVolumeG;
        public TextureObject            m_GIVolumeB;

        RenderTargetSet                 m_InitialSHSet;
        CustomConstantBufferInstance    m_GIConstantBuffer;
        RenderTargetSet                 m_Downsampled16x16SHSetR;
        RenderTargetSet                 m_Downsampled16x16SHSetG;
        RenderTargetSet                 m_Downsampled16x16SHSetB;
        RenderTargetSet                 m_Downsampled4x4SHSetR;
        RenderTargetSet                 m_Downsampled4x4SHSetG;
        RenderTargetSet                 m_Downsampled4x4SHSetB;

        public const int VolumeSizeX = 64;
        public const int VolumeSizeY = 32;
        public const int VolumeSizeZ = 64;

        Vector3 m_SceneBoundsMin;
        Vector3 m_SceneBoundsMax;

        int m_CurrentCellX, m_CurrentCellY, m_CurrentCellZ;

        public GlobalIlluminationRenderer(Device device, Vector3 sceneBoundsMin, Vector3 sceneBoundsMax)
        {
            m_CubeObject      = TextureObject.CreateCubeTexture(device, 128, 128, 1, Format.R16G16B16A16_Float, false, true);
            m_CubeObjectDepth = TextureObject.CreateCubeTexture(device, 128, 128, 1, Format.R32_Typeless, true, true);

            if (System.IO.File.Exists("textures\\givolumer.dds"))
            {
                m_GIVolumeR = TextureObject.CreateTexture3DFromFile(device, VolumeSizeX, VolumeSizeY, VolumeSizeZ, Format.R16G16B16A16_Float, "textures\\givolumer.dds");
                m_GIVolumeG = TextureObject.CreateTexture3DFromFile(device, VolumeSizeX, VolumeSizeY, VolumeSizeZ, Format.R16G16B16A16_Float, "textures\\givolumeg.dds");
                m_GIVolumeB = TextureObject.CreateTexture3DFromFile(device, VolumeSizeX, VolumeSizeY, VolumeSizeZ, Format.R16G16B16A16_Float, "textures\\givolumeb.dds");
            }
            else
            {
                m_GIVolumeR = TextureObject.CreateTexture3D(device, VolumeSizeX, VolumeSizeY, VolumeSizeZ, Format.R16G16B16A16_Float);
                m_GIVolumeG = TextureObject.CreateTexture3D(device, VolumeSizeX, VolumeSizeY, VolumeSizeZ, Format.R16G16B16A16_Float);
                m_GIVolumeB = TextureObject.CreateTexture3D(device, VolumeSizeX, VolumeSizeY, VolumeSizeZ, Format.R16G16B16A16_Float);
            }
            

            m_InitialSHSet = RenderTargetSet.CreateRenderTargetSet(device, 128, 128, Format.R16G16B16A16_Float, 3, false);

            m_Downsampled16x16SHSetR = RenderTargetSet.CreateRenderTargetSet(device, 32, 32, Format.R16G16B16A16_Float, 1, false);
            m_Downsampled16x16SHSetG = RenderTargetSet.CreateRenderTargetSet(device, 32, 32, Format.R16G16B16A16_Float, 1, false);
            m_Downsampled16x16SHSetB = RenderTargetSet.CreateRenderTargetSet(device, 32, 32, Format.R16G16B16A16_Float, 1, false);

            m_Downsampled4x4SHSetR = RenderTargetSet.CreateRenderTargetSet(device, 8, 8, Format.R16G16B16A16_Float, 1, false);
            m_Downsampled4x4SHSetG = RenderTargetSet.CreateRenderTargetSet(device, 8, 8, Format.R16G16B16A16_Float, 1, false);
            m_Downsampled4x4SHSetB = RenderTargetSet.CreateRenderTargetSet(device, 8, 8, Format.R16G16B16A16_Float, 1, false);

            m_GIConstantBuffer = ShaderManager.CreateConstantBufferInstance("GIConstantBuffer", device);

            m_SceneBoundsMin = sceneBoundsMin;
            m_SceneBoundsMax = sceneBoundsMax;
        }

        void DownsamplePass(DeviceContext context, RenderTargetSet inputSet, RenderTargetSet targetRSet, RenderTargetSet targetGSet, RenderTargetSet targetBSet)
        {
            inputSet.BindSRV(context, 0, 0);
            targetRSet.BindAsRenderTarget(context);
            PostEffectHelper.RenderFullscreenTriangle(context, "Downsample4x4");
            RenderTargetSet.BindNull(context);

            inputSet.BindSRV(context, 0, 1);
            targetGSet.BindAsRenderTarget(context);
            PostEffectHelper.RenderFullscreenTriangle(context, "Downsample4x4");
            RenderTargetSet.BindNull(context);

            inputSet.BindSRV(context, 0, 2);
            targetBSet.BindAsRenderTarget(context);
            PostEffectHelper.RenderFullscreenTriangle(context, "Downsample4x4");
            RenderTargetSet.BindNull(context);
        }

        public void PartialGIUpdate(DeviceContext context, SimpleSceneWrapper sceneWrapper, RenderTargetSet resolvedShadow)
        {
            Vector3 min = m_SceneBoundsMin;
            Vector3 max = m_SceneBoundsMax;
            Vector3 bounds = m_SceneBoundsMax - m_SceneBoundsMin;

            for (int i = 0; i < 128; ++i)
            {
                Vector3 capturePosition = 
                    new Vector3((float)m_CurrentCellX / ((float)GlobalIlluminationRenderer.VolumeSizeX) * bounds.X + min.X, 
                                (float)m_CurrentCellY / ((float)GlobalIlluminationRenderer.VolumeSizeY) * bounds.Y + min.Y, 
                                (float)m_CurrentCellZ / ((float)GlobalIlluminationRenderer.VolumeSizeZ) * bounds.Z + min.Z);

                if (++m_CurrentCellX >= GlobalIlluminationRenderer.VolumeSizeX)
                {
                    m_CurrentCellX = 0;
                    if (++m_CurrentCellZ >= GlobalIlluminationRenderer.VolumeSizeZ)
                    {
                        m_CurrentCellZ = 0;
                        if (++m_CurrentCellY >= GlobalIlluminationRenderer.VolumeSizeY)
                        {
                            Texture3D.SaveTextureToFile(context, m_GIVolumeR.m_TextureObject3D, ImageFileFormat.Dds, "textures\\givolumer.dds");
                            Texture3D.SaveTextureToFile(context, m_GIVolumeG.m_TextureObject3D, ImageFileFormat.Dds, "textures\\givolumeg.dds");
                            Texture3D.SaveTextureToFile(context, m_GIVolumeB.m_TextureObject3D, ImageFileFormat.Dds, "textures\\givolumeb.dds");
                            m_CurrentCellY = 0;
                        }
                    }
                }

                resolvedShadow.BindSRV(context, 0);
                ExecutePass(context, sceneWrapper, capturePosition);
            }
        }

        public void ExecutePass(DeviceContext context, SimpleSceneWrapper sceneWrapper, Vector3 position)
        {
            //using (new GpuProfilePoint(context, "CubemapRender"))
            {
                context.PixelShader.SetShaderResource(m_GIVolumeR.m_ShaderResourceView, 5);
                context.PixelShader.SetShaderResource(m_GIVolumeG.m_ShaderResourceView, 6);
                context.PixelShader.SetShaderResource(m_GIVolumeB.m_ShaderResourceView, 7);

                CubemapRenderHelper.RenderCubemap(context, m_CubeObject, m_CubeObjectDepth, sceneWrapper, position, 50.0f, false, new Color4(1.0f, 0.75f, 0.75f, 1.0f));
            }

            //using (new GpuProfilePoint(context, "InitSH"))
            {
                context.PixelShader.SetShaderResource(m_CubeObject.m_ShaderResourceView, 0);
                m_InitialSHSet.BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, "GIInitSH");
                RenderTargetSet.BindNull(context);

                DownsamplePass(context, m_InitialSHSet, m_Downsampled16x16SHSetR, m_Downsampled16x16SHSetG, m_Downsampled16x16SHSetB);

                m_Downsampled16x16SHSetR.BindSRV(context, 0);
                m_Downsampled4x4SHSetR.BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, "Downsample4x4");
                RenderTargetSet.BindNull(context);

                m_Downsampled16x16SHSetG.BindSRV(context, 0);
                m_Downsampled4x4SHSetG.BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, "Downsample4x4");
                RenderTargetSet.BindNull(context);

                m_Downsampled16x16SHSetB.BindSRV(context, 0);
                m_Downsampled4x4SHSetB.BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, "Downsample4x4");
                RenderTargetSet.BindNull(context);

                Vector3 sceneMin, sceneMax;
                sceneWrapper.GetSceneBounds(out sceneMin, out sceneMax);
                Vector3 positionInVolume = position - sceneMin;
                positionInVolume.X = positionInVolume.X / (sceneMax.X - sceneMin.X);
                positionInVolume.Y = positionInVolume.Y / (sceneMax.Y - sceneMin.Y);
                positionInVolume.Z = positionInVolume.Z / (sceneMax.Z - sceneMin.Z);
                dynamic scb = m_GIConstantBuffer;
                scb.InjectPosition = new Vector4(new Vector3(positionInVolume.X * (float)(VolumeSizeX - 1), positionInVolume.Y * (float)(VolumeSizeY - 1), positionInVolume.Z * (float)(VolumeSizeZ - 1)), 0);
                m_GIConstantBuffer.CompileAndBind(context);

                m_Downsampled4x4SHSetR.BindSRV(context, 2);
                m_Downsampled4x4SHSetG.BindSRV(context, 3);
                m_Downsampled4x4SHSetB.BindSRV(context, 4);
                context.ComputeShader.SetUnorderedAccessView(m_GIVolumeR.m_UnorderedAccessView, 0);
                context.ComputeShader.SetUnorderedAccessView(m_GIVolumeG.m_UnorderedAccessView, 1);
                context.ComputeShader.SetUnorderedAccessView(m_GIVolumeB.m_UnorderedAccessView, 2);
                ShaderManager.ExecuteComputeForSize(context, 1, 1, 1, "InjectSHIntoVolume");

                ContextHelper.ClearCSContext(context);
            }
        }
    }
}

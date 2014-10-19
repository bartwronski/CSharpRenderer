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
    class TextureObject
    {
        // Consts

        // dx stuff
        public Texture2D                m_TextureObject2D;
        public Texture3D                m_TextureObject3D;
        public RenderTargetView         m_RenderTargetView;
        public ShaderResourceView       m_ShaderResourceView;
        public DepthStencilView         m_DepthStencilView;
        public UnorderedAccessView      m_UnorderedAccessView;

        public RenderTargetView[]       m_ArrayRenderTargetViews;
        public ShaderResourceView[]     m_ArrayShaderResourceViews;
        public DepthStencilView[]       m_ArrayDepthStencilViews;
        public UnorderedAccessView[]    m_ArrayUnorderedAccessViews;


        // my stuff
        public int m_Width, m_Height;
        public int m_Mips;
        public int m_Depth;
        public int m_ArraySize;
        public Format m_TexFormat;
        public bool m_IsCube;


        // Functions
        public TextureObject()
        {
            m_Width = m_Height = 1;
            m_Mips = 1;
            m_Depth = 1;
            m_ArraySize = 1;
            m_IsCube = false;
            m_TexFormat = Format.Unknown;
            m_TextureObject2D = null;
            m_TextureObject3D = null;
            m_RenderTargetView = null;
            m_ShaderResourceView = null;
            m_DepthStencilView = null;
            m_UnorderedAccessView = null;

            m_ArrayRenderTargetViews = null;
            m_ArrayShaderResourceViews = null;
            m_ArrayDepthStencilViews = null;
            m_ArrayUnorderedAccessViews = null;

        }

        // Static functions
        static public TextureObject CreateTexture(Device device, int width, int height, int mips, Format format, bool isDepthStencil, bool needsGpuWrite)
        {
            if (isDepthStencil && format != Format.R32_Typeless && format != Format.R24G8_Typeless)
            {
                throw new Exception("Unsupported depth format");
            }

            TextureObject newTexture = new TextureObject();

            // Variables
            newTexture.m_Width = width;
            newTexture.m_Height = height;
            newTexture.m_TexFormat = format;
            newTexture.m_Mips = mips;

            BindFlags bindFlags = BindFlags.ShaderResource;
            if (isDepthStencil) bindFlags |= BindFlags.DepthStencil;
            if (needsGpuWrite && !isDepthStencil) bindFlags |= BindFlags.RenderTarget;
            if (needsGpuWrite && !isDepthStencil) bindFlags |= BindFlags.UnorderedAccess;

            Texture2DDescription textureDescription = new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = bindFlags,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = format,
                Height = height,
                Width = width,
                MipLevels = mips,
                OptionFlags = (mips > 1 && needsGpuWrite) ? ResourceOptionFlags.GenerateMipMaps : ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            };

            newTexture.m_TextureObject2D = new Texture2D(device, textureDescription);

            Format srvFormat = format;

            // Special case for depth
            if (isDepthStencil)
            {
                srvFormat = (format == Format.R32_Typeless) ? Format.R32_Float : Format.R24_UNorm_X8_Typeless;
            }

            ShaderResourceViewDescription srvViewDesc = new ShaderResourceViewDescription
            {
                ArraySize = 0,
                Format = srvFormat,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Flags = 0,
                FirstArraySlice = 0,
                MostDetailedMip = 0,
                MipLevels = mips
            };

            newTexture.m_ShaderResourceView = new ShaderResourceView(device, newTexture.m_TextureObject2D, srvViewDesc);

            if (isDepthStencil)
            {
                DepthStencilViewDescription dsViewDesc = new DepthStencilViewDescription
                {
                    ArraySize = 0,
                    Format = (format == Format.R32_Typeless) ? Format.D32_Float : Format.D24_UNorm_S8_UInt,
                    Dimension = DepthStencilViewDimension.Texture2D,
                    MipSlice = 0,
                    Flags = 0,
                    FirstArraySlice = 0
                };

                newTexture.m_DepthStencilView = new DepthStencilView(device, newTexture.m_TextureObject2D, dsViewDesc);
            }

            // No RTV for depth stencil, sorry
            if (needsGpuWrite && !isDepthStencil)
            {
                newTexture.m_RenderTargetView = new RenderTargetView(device, newTexture.m_TextureObject2D);

                UnorderedAccessViewDescription uavDesc = new UnorderedAccessViewDescription
                {
                    ArraySize = 0,
                    DepthSliceCount = 1,
                    Dimension = UnorderedAccessViewDimension.Texture2D,
                    ElementCount = 1,
                    FirstArraySlice = 0,
                    FirstDepthSlice = 0,
                    FirstElement = 0,
                    Flags = UnorderedAccessViewBufferFlags.None,
                    Format = format,
                    MipSlice = 0
                };
                newTexture.m_UnorderedAccessView = new UnorderedAccessView(device, newTexture.m_TextureObject2D, uavDesc);
            }

            return newTexture;
        }

        // Static functions
        static public TextureObject CreateCubeTexture(Device device, int width, int height, int mips, Format format, bool isDepthStencil, bool needsGpuWrite)
        {
            if (isDepthStencil && format != Format.R32_Typeless && format != Format.R24G8_Typeless)
            {
                throw new Exception("Unsupported depth format");
            }

            TextureObject newTexture = new TextureObject();

            // Variables
            newTexture.m_Width = width;
            newTexture.m_Height = height;
            newTexture.m_TexFormat = format;
            newTexture.m_Mips = mips;
            newTexture.m_IsCube = true;
            newTexture.m_ArraySize = 6;

            BindFlags bindFlags = BindFlags.ShaderResource;
            if (isDepthStencil) bindFlags |= BindFlags.DepthStencil;
            if (needsGpuWrite && !isDepthStencil) bindFlags |= BindFlags.RenderTarget;
            if (needsGpuWrite && !isDepthStencil) bindFlags |= BindFlags.UnorderedAccess;

            Texture2DDescription textureDescription = new Texture2DDescription
            {
                ArraySize = 6,
                BindFlags = bindFlags,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = format,
                Height = height,
                Width = width,
                MipLevels = mips,
                OptionFlags = ResourceOptionFlags.TextureCube | ((mips > 1 && needsGpuWrite) ? ResourceOptionFlags.GenerateMipMaps : ResourceOptionFlags.None),
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            };

            newTexture.m_TextureObject2D = new Texture2D(device, textureDescription);

            Format srvFormat = format;

            // Special case for depth
            if (isDepthStencil)
            {
                srvFormat = (format == Format.R32_Typeless) ? Format.R32_Float : Format.R24_UNorm_X8_Typeless;
            }

            ShaderResourceViewDescription srvViewDesc = new ShaderResourceViewDescription
            {
                Format = srvFormat,
                Dimension = ShaderResourceViewDimension.TextureCube,
                Flags = 0,
                FirstArraySlice = 0,
                MostDetailedMip = 0,
                MipLevels = mips,
                ArraySize = 6,
            };

            newTexture.m_ShaderResourceView = new ShaderResourceView(device, newTexture.m_TextureObject2D, srvViewDesc);

            newTexture.m_ArrayShaderResourceViews = new ShaderResourceView[6]; 
            for (int i = 0; i < 6; ++ i )
            {
                srvViewDesc.ArraySize = 1;
                srvViewDesc.FirstArraySlice = i;
                newTexture.m_ArrayShaderResourceViews[i] = new ShaderResourceView(device, newTexture.m_TextureObject2D, srvViewDesc);
            }

            if (isDepthStencil)
            {
                DepthStencilViewDescription dsViewDesc = new DepthStencilViewDescription
                {
                    ArraySize = 6,
                    Format = (format == Format.R32_Typeless) ? Format.D32_Float : Format.D24_UNorm_S8_UInt,
                    Dimension = DepthStencilViewDimension.Texture2DArray,
                    MipSlice = 0,
                    Flags = 0,
                    FirstArraySlice = 0
                };

                newTexture.m_DepthStencilView = new DepthStencilView(device, newTexture.m_TextureObject2D, dsViewDesc);

                newTexture.m_ArrayDepthStencilViews = new DepthStencilView[6];
                for (int i = 0; i < 6; ++i)
                {
                    dsViewDesc.ArraySize = 1;
                    dsViewDesc.FirstArraySlice = i;
                    newTexture.m_ArrayDepthStencilViews[i] = new DepthStencilView(device, newTexture.m_TextureObject2D, dsViewDesc);
                }
            }

            // No RTV for depth stencil, sorry
            if (needsGpuWrite && !isDepthStencil)
            {
                newTexture.m_RenderTargetView = new RenderTargetView(device, newTexture.m_TextureObject2D);

                RenderTargetViewDescription rtvDesc = new RenderTargetViewDescription
                {
                    ArraySize = 6,
                    Dimension = RenderTargetViewDimension.Texture2DArray,
                    FirstArraySlice = 0,
                    Format = format,
                };

                newTexture.m_ArrayRenderTargetViews = new RenderTargetView[6 * mips];
                
                for (int m = 0; m < mips; ++m)
                {
                    for (int i = 0; i < 6; ++i)
                    {
                        rtvDesc.MipSlice = m;
                        rtvDesc.ArraySize = 1;
                        rtvDesc.FirstArraySlice = i;
                        newTexture.m_ArrayRenderTargetViews[6 * m + i] = new RenderTargetView(device, newTexture.m_TextureObject2D, rtvDesc);
                    }
                }

                UnorderedAccessViewDescription uavDesc = new UnorderedAccessViewDescription
                {
                    ArraySize = 6,
                    Dimension = UnorderedAccessViewDimension.Texture2DArray,
                    FirstArraySlice = 0,
                    Format = format,
                };
                newTexture.m_UnorderedAccessView = new UnorderedAccessView(device, newTexture.m_TextureObject2D, uavDesc);

                newTexture.m_ArrayUnorderedAccessViews = new UnorderedAccessView[6 * mips];
                for (int m = 0; m < mips; ++m)
                {
                    for (int i = 0; i < 6; ++i)
                    {
                        uavDesc.ArraySize = 1;
                        uavDesc.FirstArraySlice = i;
                        uavDesc.MipSlice = m;
                        newTexture.m_ArrayUnorderedAccessViews[6 * m + i] = new UnorderedAccessView(device, newTexture.m_TextureObject2D, uavDesc);
                    }
                }
            }

            return newTexture;
        }

        // Static functions
        static public TextureObject CreateTexture3D(Device device, int width, int height, int depth, Format format)
        {
            TextureObject newTexture = new TextureObject();

            // Variables
            newTexture.m_Width = width;
            newTexture.m_Height = height;
            newTexture.m_Depth = depth;
            newTexture.m_TexFormat = format;
            newTexture.m_Mips = 1;

            BindFlags bindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget | BindFlags.UnorderedAccess;

            Texture3DDescription textureDescription = new Texture3DDescription
            {
                BindFlags = bindFlags,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = format,
                Height = height,
                Width = width,
                Depth = depth,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default
            };

            newTexture.m_TextureObject3D = new Texture3D(device, textureDescription);

            ShaderResourceViewDescription srvViewDesc = new ShaderResourceViewDescription
            {
                ArraySize = 0,
                Format = format,
                Dimension = ShaderResourceViewDimension.Texture3D,
                Flags = 0,
                FirstArraySlice = 0,
                MostDetailedMip = 0,
                MipLevels = 1,
            };

            newTexture.m_ShaderResourceView = new ShaderResourceView(device, newTexture.m_TextureObject3D, srvViewDesc);

            newTexture.m_RenderTargetView = new RenderTargetView(device, newTexture.m_TextureObject3D);
            newTexture.m_UnorderedAccessView = new UnorderedAccessView(device, newTexture.m_TextureObject3D);

            return newTexture;
        }

        // Static functions
        static public TextureObject CreateTexture3DFromFile(Device device, int width, int height, int depth, Format format, string fileName)
        {
            TextureObject newTexture = new TextureObject();

            // Variables
            newTexture.m_Width = width;
            newTexture.m_Height = height;
            newTexture.m_Depth = depth;
            newTexture.m_TexFormat = format;
            newTexture.m_Mips = 1;

            BindFlags bindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget | BindFlags.UnorderedAccess;

            ImageLoadInformation imageLoadInfo = new ImageLoadInformation()
            {
                BindFlags = bindFlags,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = format,
                Height = height,
                Width = width,
                Depth = depth,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default
            };

            newTexture.m_TextureObject3D = Texture3D.FromFile(device, fileName, imageLoadInfo);

            ShaderResourceViewDescription srvViewDesc = new ShaderResourceViewDescription
            {
                ArraySize = 0,
                Format = format,
                Dimension = ShaderResourceViewDimension.Texture3D,
                Flags = 0,
                FirstArraySlice = 0,
                MostDetailedMip = 0,
                MipLevels = 1,
            };

            newTexture.m_ShaderResourceView = new ShaderResourceView(device, newTexture.m_TextureObject3D, srvViewDesc);

            newTexture.m_RenderTargetView = new RenderTargetView(device, newTexture.m_TextureObject3D);
            newTexture.m_UnorderedAccessView = new UnorderedAccessView(device, newTexture.m_TextureObject3D);

            return newTexture;
        }
    }
}

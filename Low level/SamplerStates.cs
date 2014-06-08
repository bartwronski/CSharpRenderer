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
    static class SamplerStates
    {
        public enum SamplerType
        {
            PointWrap,
            PointClamp,
            LinearWrap,
            LinearClamp,
            LinearComparisonClamp,
            AnisotropicClamp,
        }

        static SamplerState[] m_SamplerStates;
        static bool m_Initialized;

        static SamplerStates()
        {
            m_Initialized = false;
            m_SamplerStates = new SamplerState[Enum.GetNames(typeof(SamplerType)).Length];
        }

        public static void Initialize(Device device)
        {
            m_Initialized = true;

            var samplerDescription = new SamplerDescription()
            {
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Filter = Filter.MinMagMipPoint,
                MinimumLod = 0,
                MaximumLod = 255,
            };

            m_SamplerStates[(int)SamplerType.PointWrap] = SamplerState.FromDescription(device, samplerDescription);

            samplerDescription = new SamplerDescription()
            {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                Filter = Filter.MinMagMipPoint,
                MinimumLod = 0,
                MaximumLod = 255,
            };

            m_SamplerStates[(int)SamplerType.PointClamp] = SamplerState.FromDescription(device, samplerDescription);

            samplerDescription = new SamplerDescription()
            {
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Filter = Filter.MinMagMipLinear,
                MinimumLod = 0,
                MaximumLod = 255,
            };

            m_SamplerStates[(int)SamplerType.LinearWrap] = SamplerState.FromDescription(device, samplerDescription);

            samplerDescription = new SamplerDescription()
            {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                Filter = Filter.MinMagMipLinear,
                MinimumLod = 0,
                MaximumLod = 255,
            };

            m_SamplerStates[(int)SamplerType.LinearClamp] = SamplerState.FromDescription(device, samplerDescription);

            samplerDescription = new SamplerDescription()
            {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                Filter = Filter.ComparisonMinMagMipLinear,
                MinimumLod = 0,
                MaximumLod = 255,
                ComparisonFunction = Comparison.LessEqual
            };

            m_SamplerStates[(int)SamplerType.LinearComparisonClamp] = SamplerState.FromDescription(device, samplerDescription);

            samplerDescription = new SamplerDescription()
            {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                Filter = Filter.Anisotropic,
                MinimumLod = 0,
                MaximumLod = 255,
                MaximumAnisotropy = 4,
            };

            m_SamplerStates[(int)SamplerType.AnisotropicClamp] = SamplerState.FromDescription(device, samplerDescription);
        }

        public static SamplerState GetSamplerState(SamplerType type)
        {
            if (!m_Initialized)
            {
                throw new Exception("Uninitialized sampler states!");
            }

            return m_SamplerStates[(int)type];
        }

        public static SamplerState GetSamplerStateForName(string name)
        {
            switch(name)
            {
                case "pointSampler":
                    return m_SamplerStates[(int)SamplerType.PointClamp];
                case "pointWrapSampler":
                    return m_SamplerStates[(int)SamplerType.PointWrap];
                case "linearSampler":
                    return m_SamplerStates[(int)SamplerType.LinearClamp];
                case "linearWrapSampler":
                    return m_SamplerStates[(int)SamplerType.LinearWrap];
                case "linearComparisonSampler":
                    return m_SamplerStates[(int)SamplerType.LinearComparisonClamp];
                case "anisoSampler":
                    return m_SamplerStates[(int)SamplerType.AnisotropicClamp];
                default:
                    throw new Exception("Unrecognized sampler!");
            }
        }
    }
}

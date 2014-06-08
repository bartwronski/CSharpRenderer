using System;
using System.Collections.Generic;
using System.Text;
using SlimDX;
using SlimDX.Direct3D11;
using Device = SlimDX.Direct3D11.Device;
using Resource = SlimDX.Direct3D11.Resource;
using Math = System.Math;
using SlimDX.DXGI;

using Buffer = SlimDX.Direct3D11.Buffer;

namespace CSharpRenderer
{
    class GPUBufferObject
    {
        // Consts

        // dx stuff
        public Buffer m_BufferObject;
        public ShaderResourceView m_ShaderResourceView;
        public UnorderedAccessView m_UnorderedAccessView;

        // my stuff
        public int m_Size;

        // Functions
        public GPUBufferObject()
        {
            m_Size = 1;
            m_BufferObject = null;
            m_ShaderResourceView = null;
            m_UnorderedAccessView = null;
        }

        // Static functions
        static public GPUBufferObject CreateBuffer(Device device, int size)
        {
            GPUBufferObject newBuffer = new GPUBufferObject();

            // Variables
            newBuffer.m_Size = size;

            BindFlags bindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess;

            BufferDescription description = new BufferDescription(size * 4, ResourceUsage.Default, bindFlags, CpuAccessFlags.None, ResourceOptionFlags.StructuredBuffer, 4);

            newBuffer.m_BufferObject = new Buffer(device, description);

            ShaderResourceViewDescription srvViewDesc = new ShaderResourceViewDescription
            {
                ArraySize = 0,
                ElementCount = size,
                ElementWidth = size,
                Format = Format.Unknown,
                Dimension = ShaderResourceViewDimension.Buffer,
                Flags = 0,
                FirstArraySlice = 0,
                MostDetailedMip = 0,
                MipLevels = 0
            };

            newBuffer.m_ShaderResourceView = new ShaderResourceView(device, newBuffer.m_BufferObject, srvViewDesc);


            UnorderedAccessViewDescription uavDesc = new UnorderedAccessViewDescription
            {
                ArraySize = 0,
                Dimension = UnorderedAccessViewDimension.Buffer,
                ElementCount = size,
                Flags = UnorderedAccessViewBufferFlags.None,
                Format = Format.Unknown,
                MipSlice = 0
            };
            newBuffer.m_UnorderedAccessView = new UnorderedAccessView(device, newBuffer.m_BufferObject, uavDesc);

            return newBuffer;
        }
    }
}

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

        public Buffer m_StagingBufferObject;
        public Buffer m_StagingCountBufferObject;

        // my stuff
        public int m_Size;

        // Functions
        public GPUBufferObject()
        {
            m_Size = -1;
            m_BufferObject = null;
            m_ShaderResourceView = null;
            m_UnorderedAccessView = null;
            m_StagingBufferObject = null;
            m_StagingCountBufferObject = null;
        }

        // Static functions
        static public GPUBufferObject CreateBuffer(Device device, int size, int elementSizeInBytes, DataStream stream = null, bool append = false, bool allowStaging = false)
        {
            GPUBufferObject newBuffer = new GPUBufferObject();

            // Variables
            newBuffer.m_Size = size;

            BindFlags bindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess;

            BufferDescription description = new BufferDescription(size * elementSizeInBytes, ResourceUsage.Default, bindFlags, CpuAccessFlags.None, ResourceOptionFlags.StructuredBuffer, elementSizeInBytes);

            newBuffer.m_BufferObject = stream != null ? new Buffer(device, stream, description) : new Buffer(device, description);

            ShaderResourceViewDescription srvViewDesc = new ShaderResourceViewDescription
            {
                FirstElement = 0,
                ElementCount = size,
                Format = Format.Unknown,
                Dimension = ShaderResourceViewDimension.ExtendedBuffer,
                Flags = 0,
            };

            newBuffer.m_ShaderResourceView = new ShaderResourceView(device, newBuffer.m_BufferObject, srvViewDesc);


            UnorderedAccessViewDescription uavDesc = new UnorderedAccessViewDescription
            {
                ArraySize = 0,
                Dimension = UnorderedAccessViewDimension.Buffer,
                ElementCount = size,
                Flags = append ? UnorderedAccessViewBufferFlags.AllowAppend : UnorderedAccessViewBufferFlags.None,
                Format = Format.Unknown,
                MipSlice = 0
            };
            newBuffer.m_UnorderedAccessView = new UnorderedAccessView(device, newBuffer.m_BufferObject, uavDesc);

            if (allowStaging)
            {
                description = new BufferDescription(size * elementSizeInBytes, ResourceUsage.Staging, BindFlags.None, CpuAccessFlags.Read, ResourceOptionFlags.StructuredBuffer, elementSizeInBytes);
                newBuffer.m_StagingBufferObject = new Buffer(device, description);

                description = new BufferDescription(16, ResourceUsage.Staging, BindFlags.None, CpuAccessFlags.Read, ResourceOptionFlags.None, 4);
                newBuffer.m_StagingCountBufferObject = new Buffer(device, description);
            }

            return newBuffer;
        }

        public int GetAppendStructureCount(DeviceContext context)
        {
            context.CopyStructureCount(m_UnorderedAccessView, m_StagingCountBufferObject, 0);
            DataBox box = context.MapSubresource(m_StagingCountBufferObject, MapMode.Read, SlimDX.Direct3D11.MapFlags.None);
            int result = box.Data.Read<int>();
            context.UnmapSubresource(m_StagingCountBufferObject, 0);

            return result;
        }

        public DataBox LockBuffer(DeviceContext context)
        {
            context.CopyResource(m_BufferObject, m_StagingBufferObject);
            return context.MapSubresource(m_StagingBufferObject, MapMode.Read, SlimDX.Direct3D11.MapFlags.None);
        }

        public void UnlockBuffer(DeviceContext context)
        {
            context.UnmapSubresource(m_StagingBufferObject, 0);
        }
    }
}

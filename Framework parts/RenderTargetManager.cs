using SlimDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpRenderer
{
    static class RenderTargetManager
    {
        class RenderTargetContainer
        {
            public RenderTargetSet.RenderTargetDescriptor m_Descriptor;
            public RenderTargetSet m_RT;
            public bool m_Used;

            public RenderTargetContainer()
            {
                m_RT = null;
                m_Used = false;
            }
        };

        static List<RenderTargetContainer> m_RenderTargets;
        static Device m_Device;

        static RenderTargetManager()
        {
            m_RenderTargets = new List<RenderTargetContainer>();
        }

        public static void Initialize(Device device)
        {
            m_Device = device;
        }

        public static RenderTargetSet RequestRenderTargetFromPool( RenderTargetSet.RenderTargetDescriptor descriptor )
        {
            var result = m_RenderTargets.Where(rt => (rt.m_Used == false) && (rt.m_Descriptor == descriptor) );
            if (result.Count() > 0)
            {
                var firstElement = result.First();
                firstElement.m_Used = true;
                return firstElement.m_RT;
            }
            else
            {
                RenderTargetSet rt = RenderTargetSet.CreateRenderTargetSet(m_Device, descriptor);
                RenderTargetContainer containter = new RenderTargetContainer()
                {
                    m_Descriptor = descriptor,
                    m_RT = rt,
                    m_Used = true,
                };
                m_RenderTargets.Add(containter);

                return rt;
            }
        }

        public static void ReleaseRenderTargetToPool(RenderTargetSet renderTargetSet)
        {
            var result = m_RenderTargets.Where(rt => (rt.m_Used == true) && (rt.m_RT == renderTargetSet));
            if(result.Count() != 1)
            {
                throw new Exception("Wrong release of render target set in render target manager");
            }

            result.First().m_Used = false;
        }

    }
}

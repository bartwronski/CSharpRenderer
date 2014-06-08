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
using System.Collections.Generic;
using System;

namespace CSharpRenderer
{
    static class TemporalSurfaceManager
    {
        class TemporalRenderTargetSetDescriptor
        {
            public TemporalRenderTargetSetDescriptor(List<RenderTargetSet> sets)
            {
                m_RenderTargets = new RenderTargetSet[sets.Count];

                int count = 0;
                foreach( RenderTargetSet set in sets)
                {
                    m_RenderTargets[count++] = set;
                }

                m_Phase = 0;
            }

            public RenderTargetSet[] m_RenderTargets;
            public int m_Phase;
        };

        static Dictionary<string, TemporalRenderTargetSetDescriptor> m_TemporalRenderTargets;

        static TemporalSurfaceManager()
        {
            m_TemporalRenderTargets = new Dictionary<string, TemporalRenderTargetSetDescriptor>();
        }

        public static void UpdateTemporalSurfaces()
        {
            foreach(TemporalRenderTargetSetDescriptor s in m_TemporalRenderTargets.Values)
            {
                if(++s.m_Phase >= s.m_RenderTargets.Length)
                {
                    s.m_Phase = 0;
                }
            }
        }

        public static void InitializeRenderTarget( string name, RenderTargetSet.RenderTargetDescriptor descriptor, int countHistory = 1)
        {
            if (m_TemporalRenderTargets.ContainsKey(name))
            {
                throw new Exception("Temporal surface with given name already exists");
            }

            List<RenderTargetSet> sets = new List<RenderTargetSet>();

            while (countHistory-- >= 0)
            {
                sets.Add(RenderTargetManager.RequestRenderTargetFromPool(descriptor));
            }
            TemporalRenderTargetSetDescriptor set = new TemporalRenderTargetSetDescriptor(sets);
            m_TemporalRenderTargets[name] = set;
        }

        public static RenderTargetSet GetRenderTargetCurrent(string name)
        {
            TemporalRenderTargetSetDescriptor set = m_TemporalRenderTargets[name];
            return set.m_RenderTargets[set.m_Phase];
        }

        public static RenderTargetSet GetRenderTargetHistory(string name, int framesBefore = 1)
        {
            TemporalRenderTargetSetDescriptor set = m_TemporalRenderTargets[name];
            int phaseHistory = set.m_Phase - framesBefore;

            if (phaseHistory < 0)
            {
                phaseHistory = set.m_RenderTargets.Length + phaseHistory;
            }

            return set.m_RenderTargets[phaseHistory];
        }

        public static int GetCurrentPhase(string name)
        {
            TemporalRenderTargetSetDescriptor set = m_TemporalRenderTargets[name];
            return set.m_Phase;
        }


    };
}
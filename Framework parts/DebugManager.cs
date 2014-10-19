using SlimDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpRenderer
{
    static class DebugManager
    {
        public static List<String> m_AvailableModes;
        public static Dictionary<String, bool> m_AvailableFeatures;
        static RenderTargetSet m_DebugRenderTarget;
        static RenderTargetSet m_SnapshotRenderTarget;
        public static String m_CurrentDebugSurface;
        public static String m_CurrentDebugMode;
        public static bool m_IsUIRebuildRequired;
        public static bool m_GPUDebugOn;
        public static bool m_FirstCallThisFrame;
        public static GPUBufferObject m_DebugAppendBuffer;
        public static bool m_DisabledTemporal;
        public static bool m_TakeSnapshot;
        public static bool m_ShowSnapshot;
        public static bool m_ShowSnapshotDifference;

        static DebugManager()
        {
            m_AvailableFeatures = new Dictionary<string, bool>();
            m_AvailableModes = new List<String>();
            m_AvailableModes.Add("None");
            m_CurrentDebugSurface = "None";
            m_IsUIRebuildRequired = true;
            m_CurrentDebugMode = "RGB";
            m_GPUDebugOn = false;
            m_FirstCallThisFrame = true;
        }

        public static void Initialize(Device device, int resolutionX, int resolutionY)
        {
            RenderTargetSet.RenderTargetDescriptor rtDesc = new RenderTargetSet.RenderTargetDescriptor()
            {
                m_Format = SlimDX.DXGI.Format.R8G8B8A8_UNorm,
                m_HasDepth = false,
                m_Width = resolutionX,
                m_Height = resolutionY,
                m_NumSurfaces = 1,
            };

            m_DebugRenderTarget = RenderTargetManager.RequestRenderTargetFromPool(rtDesc);
            m_SnapshotRenderTarget = RenderTargetManager.RequestRenderTargetFromPool(rtDesc);
            m_DebugAppendBuffer = GPUBufferObject.CreateBuffer(device, 1024, 8 * sizeof(uint), null, true, true);
        }

        public static void SetGPUDebugMode(bool gpuDebugOn)
        {
            m_GPUDebugOn = gpuDebugOn;
        }

        public static void RegisterDebug(DeviceContext context, string name, RenderTargetSet debuggedRT, int rtNum = 0)
        {
            if (m_AvailableModes.Contains(name))
            {
                if (m_CurrentDebugSurface == name)
                {
                    if (m_CurrentDebugMode == "A")
                    {
                        PostEffectHelper.CopyAlpha(context, m_DebugRenderTarget, debuggedRT, rtNum);
                    }
                    else if (m_CurrentDebugMode == "FRAC")
                    {
                        PostEffectHelper.CopyFrac(context, m_DebugRenderTarget, debuggedRT, rtNum);
                    }
                    else if (m_CurrentDebugMode == "RGBGamma")
                    {
                        PostEffectHelper.CopyGamma(context, m_DebugRenderTarget, debuggedRT, rtNum);
                    }
                    else
                    {
                        PostEffectHelper.Copy(context, m_DebugRenderTarget, debuggedRT, rtNum);
                    }
                }
            }
            else
            {
                m_IsUIRebuildRequired = true;
                m_AvailableModes.Add(name);
            }
        }

        public static bool IsFeatureOn(string name)
        {
            bool retVal;
            if(m_AvailableFeatures.TryGetValue(name, out retVal))
            {
                return retVal;
            }
            else
            {
                m_AvailableFeatures.Add(name, true);
                m_IsUIRebuildRequired = true;
            }

            return true;
        }

        public static bool IsDebugging(string name)
        {
            if (m_AvailableModes.Contains(name))
            {
                if (m_CurrentDebugSurface == name)
                {
                    return true;
                }
            }
            else
            {
                m_IsUIRebuildRequired = true;
                m_AvailableModes.Add(name);
            }

            return false;
        }

        public static string GetDebugString(DeviceContext context)
        {
            string outString = "";
            if (m_GPUDebugOn)
            {
                int count = m_DebugAppendBuffer.GetAppendStructureCount(context);
                SlimDX.DataBox db = m_DebugAppendBuffer.LockBuffer(context);
                for (uint i = 0; i < count; ++i)
                {
                    uint x = (uint)db.Data.Read<float>();
                    uint y = (uint)db.Data.Read<float>();
                    float z = db.Data.Read<float>();
                    uint compCount = (uint)db.Data.Read<float>();

                    float xV = db.Data.Read<float>();
                    float yV = db.Data.Read<float>();
                    float zV = db.Data.Read<float>();
                    float wV = db.Data.Read<float>();

                    if (Math.Floor((double)z) == (double)z)
                    {
                        outString += String.Format("pos {0,-4} {1,-4} {2,-4} || ", x, y, (uint)z);
                    }
                    else
                    {
                        outString += String.Format("pos {0,-4} {1,-4} {2,-8:F4} || ", x, y, z);
                    }
                    
                    switch(compCount)
                    {
                        case 1:
                            outString += String.Format("{0,-7:F4}", xV);
                            break;
                        case 2:
                            outString += String.Format("{0,-7:F4} {1,-7:F4}", xV, yV);
                            break;
                        case 3:
                            outString += String.Format("{0,-7:F4} {1,-7:F4} {2,-7:F4}", xV, yV, zV);
                            break;
                        case 4:
                            outString += String.Format("{0,-7:F4} {1,-7:F4} {2,-7:F4} {3:F4}", xV, yV, zV, wV);
                            break;
                    }
                    outString += System.Environment.NewLine;
                }
                m_DebugAppendBuffer.UnlockBuffer(context);
            }

            m_FirstCallThisFrame = true;

            return outString;
        }

        public static void PresentDebug(DeviceContext context, RenderTargetSet target)
        {
            if (m_CurrentDebugSurface != "None")
            {
                PostEffectHelper.Copy(context, target, m_DebugRenderTarget);
            }
            if (m_TakeSnapshot)
            {
                PostEffectHelper.Copy(context, m_SnapshotRenderTarget, target);
                m_TakeSnapshot = false;
            }
            if (m_ShowSnapshot)
            {
                if (m_ShowSnapshotDifference)
                {
                    PostEffectHelper.Copy(context, m_DebugRenderTarget, target);
                    PostEffectHelper.Difference(context, target, m_SnapshotRenderTarget, m_DebugRenderTarget);
                }
                else
                {
                    PostEffectHelper.Copy(context, target, m_SnapshotRenderTarget);
                }
            }
        }

    }
}

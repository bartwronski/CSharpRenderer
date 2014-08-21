using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.Direct3D11;

namespace CSharpRenderer
{
    class ResolveMotionVectorsPass
    {
        public void ExecutePass(DeviceContext context, RenderTargetSet target, RenderTargetSet source)
        {
            using (new GpuProfilePoint(context, "ResolveMotionVectors"))
            {
                source.BindDepthAsSRV(context, 0);
                target.BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, "ResolveMotionVectors");
                RenderTargetSet.BindNull(context);
                ContextHelper.ClearSRVs(context);
            }
        }
    }

    class ResolveTemporalMotionBasedPass
    {
        public void ExecutePass(
            DeviceContext context, 
            RenderTargetSet target, 
            RenderTargetSet sourceCurrent, 
            RenderTargetSet sourceHistory, 
            RenderTargetSet motionVectors, 
            RenderTargetSet motionVectorsPrevious, 
            bool accumulateHistory = false)
        {
            using (new GpuProfilePoint(context, "ResolveTemporalMotionBased"))
            {
                sourceCurrent.BindSRV(context, 0);
                sourceHistory.BindSRV(context, 1);
                motionVectors.BindSRV(context, 2);
                motionVectorsPrevious.BindSRV(context, 3);

                target.BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, accumulateHistory ? "ResolveTemporalHistoryAccumulation" : "ResolveTemporalMotionBased");
                RenderTargetSet.BindNull(context);
                ContextHelper.ClearSRVs(context);

                if (accumulateHistory)
                {
                    PostEffectHelper.Copy(context, sourceCurrent, target);
                }
            }
        }
    }
}

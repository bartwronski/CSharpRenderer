using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.Direct3D11;

namespace CSharpRenderer
{
    class ResolveHDRPass
    {
        public void ExecutePass(DeviceContext context, RenderTargetSet target, RenderTargetSet source, RenderTargetSet luminanceTexture)
        {
            using (new GpuProfilePoint(context, "HDR Resolve"))
            {
                source.BindSRV(context, 0);
                luminanceTexture.BindSRV(context, 1);
                target.BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, "ResolveHDR");
                RenderTargetSet.BindNull(context);
                ContextHelper.ClearSRVs(context);
            }
        }
    }
}

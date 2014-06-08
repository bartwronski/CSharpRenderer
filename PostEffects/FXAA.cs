using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.Direct3D11;

namespace CSharpRenderer
{
    class FxaaPass
    {
        public void ExecutePass(DeviceContext context, RenderTargetSet target, RenderTargetSet source)
        {
            using (new GpuProfilePoint(context, "FXAA"))
            {
                source.BindSRV(context, 0);
                target.BindAsRenderTarget(context);
                PostEffectHelper.RenderFullscreenTriangle(context, "FXAA");
                RenderTargetSet.BindNull(context);
                ContextHelper.ClearSRVs(context);
            }
        }
    }
}

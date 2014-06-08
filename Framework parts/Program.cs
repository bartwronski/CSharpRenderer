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
using System;
using System.Collections.Generic;

namespace CSharpRenderer
{
    static class Program
    {
        const int ResolutionX = 1280;
        const int ResolutionY = 720;

        static void Main()
        {
            Device device;
            SwapChain swapChain;

            //var form = new RenderForm("CSharpRenderer");
            var form = new CSharpRendererMainForm();
            var panel = form.GetRenderingPanel();
            form.ClientSize = new System.Drawing.Size(ResolutionX, ResolutionY);

            var description = new SwapChainDescription()
            {
                BufferCount = 2,
                Usage = Usage.RenderTargetOutput,
                OutputHandle = panel.Handle,
                IsWindowed = true,
                ModeDescription = new ModeDescription(ResolutionX, ResolutionY, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                SampleDescription = new SampleDescription(1, 0),
                Flags = SwapChainFlags.AllowModeSwitch,
                SwapEffect = SwapEffect.Discard
            };

            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, description, out device, out swapChain);
            var swapChainResource = Resource.FromSwapChain<Texture2D>(swapChain, 0);

            SamplerStates.Initialize(device);
            ShaderManager.Initialize(device);
            GPUProfiler.Initialize(device);
            ContextHelper.Initialize(device);
            RenderTargetManager.Initialize(device);
            PostEffectHelper.Initialize(device, ResolutionX, ResolutionY);
            CubemapRenderHelper.Initialize(device);

            Dictionary<CustomConstantBufferDefinition.ConstantBufferPropertyField, Tuple<TrackBar, TextBox>> propertyControlBindings = 
                new Dictionary<CustomConstantBufferDefinition.ConstantBufferPropertyField, Tuple<TrackBar, TextBox>>();

            {
                TableLayoutPanel tableLayout = form.GetTableLayoutPanel();

                var contantBuffers = ShaderManager.GetConstantBufferDefinitions();

                int tableParamCounter = 1;
                foreach (var cb in contantBuffers)
                {
                    var paramProperties = cb.GetParamProperties();

                    if (paramProperties.Count > 0)
                    {
                        Label groupLabel = new Label();
                        groupLabel.Text = cb.m_Name;
                        groupLabel.BorderStyle = BorderStyle.FixedSingle;
                        groupLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
                        tableLayout.Controls.Add(groupLabel, 0, tableParamCounter);
                        tableParamCounter++;

                        foreach (var param in paramProperties)
                        {
                            Label lb = new Label();
                            lb.Text = param.name;
                            lb.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
                            TextBox text = new TextBox();
                            TrackBar tb = new TrackBar();
                            tb.Size = new System.Drawing.Size(400, 10);
                            tb.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
                            tb.Minimum = 0;
                            tb.Maximum = 1024;
                            tb.Value = (int)(((Single)param.paramValue - param.paramRangeMin) / (param.paramRangeMax - param.paramRangeMin) * 1024);
                            text.Text = ((Single)param.paramValue).ToString();
                            text.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
                            tableLayout.Controls.Add(lb, 0, tableParamCounter);
                            tableLayout.Controls.Add(tb, 1, tableParamCounter);
                            tableLayout.Controls.Add(text, 2, tableParamCounter);
                            propertyControlBindings.Add(param, new Tuple<TrackBar, TextBox>(tb, text));
                            tableParamCounter++;
                        }

                    }
                }
            }

            Scene scene = new Scene();
            scene.Initialize(device, form, panel, ResolutionX, ResolutionY);

            var resolvedRenderTarget = RenderTargetSet.CreateRenderTargetSet(device, ResolutionX, ResolutionY, Format.R8G8B8A8_UNorm, 1, false);
            RenderTargetSet.BindNull(device.ImmediateContext);

            // setting a viewport is required if you want to actually see anything
            var context = device.ImmediateContext;

            // prevent DXGI handling of alt+enter, which doesn't work properly with Winforms
            using (var factory = swapChain.GetParent<Factory>())
                factory.SetWindowAssociation(panel.Handle, WindowAssociationFlags.IgnoreAltEnter);

            int counter = 0;

            Dictionary<string, double> profilers;
            profilers = new Dictionary<String, double>();

            MessagePump.Run(form, () =>
            {
                TemporalSurfaceManager.UpdateTemporalSurfaces();
                ShaderManager.UpdateShaderManager(device);

                GPUProfiler.BeginFrameProfiling(context);
                scene.RenderFrame(context, 0.0f, resolvedRenderTarget);
                context.CopyResource(resolvedRenderTarget.m_RenderTargets[0].m_TextureObject2D, swapChainResource);
                GPUProfiler.EndFrameProfiling(context);
                swapChain.Present(0, PresentFlags.None);
                context.PixelShader.SetShaderResource(null, 0);

                if (GPUProfiler.m_CurrentFrameProfilerTree != null)
                {
                    Action<GPUProfiler.ProfilerTreeMember, String> processLevel = null;

                    processLevel = (GPUProfiler.ProfilerTreeMember treeMember, String level) =>
                    {
                        string finalName = level + treeMember.m_Name;

                        if(profilers.ContainsKey(finalName))
                        {
                            profilers[finalName] += treeMember.m_Time;
                        }
                        else 
                        {
                            profilers.Add(finalName, treeMember.m_Time);
                        }

                        foreach (var v in treeMember.m_ChildMembers)
                        {
                            processLevel(v, level + "_");
                        }
                    };

                    processLevel(GPUProfiler.m_CurrentFrameProfilerTree, "");
                }

                if (++counter == 10)
                {
                    foreach (var propertyBinding in propertyControlBindings)
                    {
                        var property = propertyBinding.Key;
                        var trackBar = propertyBinding.Value.Item1;
                        var textBox = propertyBinding.Value.Item2;

                        float rawVal = (float)trackBar.Value / 1024.0f;
                        if (property.isGamma)
                        {
                            rawVal = (float)Math.Pow((double)rawVal, 2.2);
                        }
                        float val = rawVal * (property.paramRangeMax - property.paramRangeMin) + property.paramRangeMin;
                        property.paramValue = val;
                        textBox.Text = val.ToString("F");
                    }

                    DataGridView dataGridView = form.GetDataGridView();
                    dataGridView.Rows.Clear();

                    foreach(var profilerEntry in profilers)
                    {
                        DataGridViewRow row = new DataGridViewRow();
                        row.CreateCells(dataGridView);
                        row.Cells[0].Value = profilerEntry.Key;
                        row.Cells[1].Value = String.Format("{0}", Math.Round(profilerEntry.Value * 100.0 / 10.0) / 100.0);
                        dataGridView.Rows.Add(row);
                    }

                    profilers.Clear();

                    counter = 0;
                }
                System.Threading.Thread.Sleep(15);
            });
        }
    }
}

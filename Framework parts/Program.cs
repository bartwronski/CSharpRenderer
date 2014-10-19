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
        public static int m_FrameNumber;
        public static double m_Time;
        public static int m_ClickedX;
        public static int m_ClickedY;
        public static bool m_TakeSnapshot;

        static Dictionary<CustomConstantBufferDefinition.ConstantBufferPropertyField, Tuple<TrackBar, TextBox>> m_PropertyControlBindings;

        static void Main()
        {
            m_FrameNumber = 0;
            m_Time = 0.0f;

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
            PerlinNoiseRenderHelper.Initialize(device, device.ImmediateContext);
            LightingLUTHelper.Initialize(device, device.ImmediateContext);
            DebugManager.Initialize(device, ResolutionX, ResolutionY);

            InitializeControls(form);

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

            DateTime now = DateTime.Now;

            CustomConstantBufferInstance globalFrameConstantBuffer = ShaderManager.CreateConstantBufferInstance("GlobalFrameBuffer", device);

            MessagePump.Run(form, () =>
            {
                TemporalSurfaceManager.UpdateTemporalSurfaces();
                if (ShaderManager.UpdateShaderManager(device))
                {
                    InitializeControls(form);
                }

                GPUProfiler.BeginFrameProfiling(context);

                double timeDelta = (DateTime.Now - now).TotalMilliseconds;
                
                if (!form.GetFreezeTime())
                {
                    m_Time += timeDelta / 1000.0;
                    m_FrameNumber++;
                }
                
                UpdateGlobalConstantBuffer(context, globalFrameConstantBuffer, form);

                scene.RenderFrame(context, timeDelta, resolvedRenderTarget);
                now = DateTime.Now;
                DebugManager.PresentDebug(context, resolvedRenderTarget);
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

                CheckAndUpdateDebugUI(context, form);

                if (++counter == 10)
                {
                    UpdateControls();
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
                else
                {
                    // avoid 2ms frame times...
                    System.Threading.Thread.Sleep(10);
                }
            });
        }

        private static void UpdateGlobalConstantBuffer(DeviceContext context, CustomConstantBufferInstance globalFrameConstantBuffer, CSharpRendererMainForm form)
        {
            dynamic gfcb = globalFrameConstantBuffer;
            Random rand = new Random();

            gfcb.g_Time = (float)m_Time;
            if (!form.GetFreezeTime())
                gfcb.g_FrameRandoms = new Vector4((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());

            gfcb.g_FrameNumber = m_FrameNumber;
            gfcb.g_GPUDebugOn = DebugManager.m_GPUDebugOn ? 1.0f : 0.0f;
            gfcb.g_GPUDebugOverridePositionEnable = form.GetGPUDebuggingOverridePos() ? 1.0f : 0.0f;
            gfcb.g_GPUDebugOverridePositionXYZ = new Vector4((float)form.GetGPUDebuggingOverridePosX(), (float)form.GetGPUDebuggingOverridePosY(), (float)form.GetGPUDebuggingOverridePosZ(), 0.0f);
            globalFrameConstantBuffer.CompileAndBind(context);
        }

        private static void CheckAndUpdateDebugUI(DeviceContext context, CSharpRendererMainForm form)
        {
            ComboBox cb = form.GetDebugSurface();
            ComboBox cbM = form.GetDebugMode();
            CheckedListBox features = form.GetFeatureCLB();
            if (DebugManager.m_IsUIRebuildRequired)
            {
                cb.Items.Clear();
                cb.Items.AddRange(DebugManager.m_AvailableModes.ToArray());

                features.Items.Clear();

                foreach (var pair in DebugManager.m_AvailableFeatures)
                {
                    features.Items.Add(pair.Key, pair.Value);
                }

                DebugManager.m_IsUIRebuildRequired = false;
                cbM.SelectedIndex = 0;
                cb.SelectedIndex = 0;
            }

            if (cb.SelectedIndex >= 0 && form.GetDebugModeOn())
            {
                DebugManager.m_CurrentDebugSurface = (string)cb.Items[cb.SelectedIndex];
                DebugManager.m_CurrentDebugMode = cbM.SelectedIndex > 0 ? (string)cbM.Items[cbM.SelectedIndex] : "RGB";
            }
            else
            {
                DebugManager.m_CurrentDebugSurface = "None";
            }

            if (m_TakeSnapshot)
            {
                DebugManager.m_TakeSnapshot = true;
                m_TakeSnapshot = false;
            }

            int counter = 0;
            string[] featuresArrayCopy = new string[DebugManager.m_AvailableFeatures.Keys.Count];
            DebugManager.m_AvailableFeatures.Keys.CopyTo(featuresArrayCopy, 0);
            foreach (var key in featuresArrayCopy)
            {
                bool uiValue = features.GetItemChecked(counter++);
                DebugManager.m_AvailableFeatures[key] = uiValue;
            }

            DebugManager.m_ShowSnapshot = form.GetShowSnapshot();
            DebugManager.m_ShowSnapshotDifference = form.GetShowSnapshotDifference();

            if (DebugManager.m_GPUDebugOn)
            {
                form.GetGPUDebuggingTB().Text = DebugManager.GetDebugString(context);
            }
            
            DebugManager.SetGPUDebugMode(form.GetGPUDebugModeOn());
            DebugManager.m_DisabledTemporal = form.GetDisabledTemporal();
        }

        private static void UpdateControls()
        {
            foreach (var propertyBinding in m_PropertyControlBindings)
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
        }

        private static void InitializeControls(CSharpRendererMainForm form)
        {
            Dictionary<String, int> oldValues = new Dictionary<String, int>();
            // record old values
            if (m_PropertyControlBindings != null)
            {
                foreach (var propertyBinding in m_PropertyControlBindings)
                {
                    oldValues.Add(propertyBinding.Key.name, propertyBinding.Value.Item1.Value);
                }
            }

            m_PropertyControlBindings = new Dictionary<CustomConstantBufferDefinition.ConstantBufferPropertyField, Tuple<TrackBar, TextBox>>();
            
            TableLayoutPanel tableLayout = form.GetTableLayoutPanel();
            tableLayout.Controls.Clear();
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

                        string key = param.name;
                        int value = (int)(((Single)param.paramValue - param.paramRangeMin) / (param.paramRangeMax - param.paramRangeMin) * 1024);
                        int potentialOldVal;

                        if (oldValues.TryGetValue(key, out potentialOldVal))
                            value = potentialOldVal;

                        tb.Value = value;
                        text.Text = key;
                        text.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
                        tableLayout.Controls.Add(lb, 0, tableParamCounter);
                        tableLayout.Controls.Add(tb, 1, tableParamCounter);
                        tableLayout.Controls.Add(text, 2, tableParamCounter);
                        m_PropertyControlBindings.Add(param, new Tuple<TrackBar, TextBox>(tb, text));
                        tableParamCounter++;
                    }

                }
            }

        }
    }
}

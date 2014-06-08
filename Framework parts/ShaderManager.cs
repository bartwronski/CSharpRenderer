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
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Security.Permissions;

namespace CSharpRenderer
{
    static class ShaderManager
    {
        enum ShaderType
        {
            ComputeShader,
            PixelShader,
            VertexShader,
            GeometryShader,
        }

        class ShaderWrapper
        {
            public ShaderType m_ShaderType;
            public string m_ShaderName;
            public string m_ShaderEntry;
            public string m_FilePath;
            public HashSet<string> m_UsedIncludes;
            public PixelShader m_PixelShader;
            public VertexShader m_VertexShader;
            public GeometryShader m_GeometryShader;
            public ComputeShader m_ComputeShader;
            public ShaderSignature m_VertexInputSignature;
            public HashSet<string> m_Defines;
            public int m_ThreadsX;
            public int m_ThreadsY;
            public int m_ThreadsZ;
        }

        class IncludeFX : Include
        {
            static string includeDirectory = "shaders\\";

            public void Close(Stream stream)
            {
                stream.Close();
                stream.Dispose();
            }

            public void Open(IncludeType type, string fileName, Stream parentStream, out Stream stream)
            {
                stream = new FileStream(includeDirectory + fileName, FileMode.Open);
                m_CurrentlyProcessedShader.m_UsedIncludes.Add(Directory.GetCurrentDirectory() + "\\shaders\\" + fileName);
            }
        }

        static IncludeFX m_Include;
        static bool m_Initialized;
        static Dictionary<string, ShaderWrapper> m_Shaders;
        static Dictionary<string, CustomConstantBufferDefinition> m_ConstantBuffers;
        static List<FileSystemWatcher> m_Watchers;
        static Dictionary<int, SamplerState> m_SamplerStates;
        static object m_Lock;
        static HashSet<string> m_FilesToReload;

        // hack for ugly IncludeFX :( 
        static ShaderWrapper m_CurrentlyProcessedShader;

        class RegexWrapper
        {
            public Regex shaderRegex;
            public Regex cbufferRegex;
            public Regex samplerRegex;
            public Regex numThreadsRegex;
            public Regex registerRegex;
            public Regex globalBufferRegex;

            public RegexWrapper()
            {
                shaderRegex = new Regex(@"//[ \t]+(Compute|Pixel|Vertex|Geometry)Shader:[ \t]+(\w+)?,[ \t]+entry:[ \t]+(\w+),?[ \t]*(defines:([ \t]*\w+)*)?", RegexOptions.IgnoreCase);
                cbufferRegex = new Regex(@"[ \t]*cbuffer[ \t]+(\w+)[ \t\n\r]*{?", RegexOptions.IgnoreCase);
                samplerRegex = new Regex(@"[ \t]*(SamplerState|SamplerComparisonState)[ \t]+(\w+)[ \t]+:[ \t]+register\(s(\w+)\)", RegexOptions.IgnoreCase);
                numThreadsRegex = new Regex(@"\[numthreads\((\w+),[ \t]*(\w+),[ \t]*(\w+)\)\]", RegexOptions.IgnoreCase);
                registerRegex = new Regex(@"register\(b([\d]+)\)", RegexOptions.IgnoreCase);
                globalBufferRegex = new Regex(@"//[ \t]*Global", RegexOptions.IgnoreCase);
            }
        };
        static ShaderManager()
        {
            m_Initialized = false;
            m_Shaders = new Dictionary<string, ShaderWrapper>();
            m_ConstantBuffers = new Dictionary<string, CustomConstantBufferDefinition>();
            m_Watchers = new List<FileSystemWatcher>();
            m_CurrentlyProcessedShader = null;
            m_SamplerStates = new Dictionary<int, SamplerState>();
            m_Lock = new Object();
            m_FilesToReload = new HashSet<string>();
            regexWrapper = new RegexWrapper();
        }

        static RegexWrapper regexWrapper;

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        static public void Initialize(Device device)
        {
            if (m_Initialized)
            {
                throw new Exception("Already initialized!");
            }

            string[] filters = new[] { "*.hlsl", "*.fx" };

            foreach(string f in filters)
            {
                FileSystemWatcher w = new FileSystemWatcher();
                w.Filter = f;
                w.Changed += new FileSystemEventHandler(OnFileChanged);
                w.Renamed += new RenamedEventHandler(OnFileChanged);
                w.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName;
                w.Path = Directory.GetCurrentDirectory() + "\\shaders";
                w.EnableRaisingEvents = true;
                m_Watchers.Add(w);
            }
            
            m_Initialized = true;
            m_Include = new IncludeFX();

            CultureInfo ci = new CultureInfo("en-US", false);
            string[] filePaths = filters.SelectMany(f => Directory.GetFiles(Directory.GetCurrentDirectory() + "\\shaders", f)).ToArray();


            foreach (string path in filePaths)
            {
                ParseFile(device, path);
            }
        }

        private static void ParseFile(Device device, string path)
        {
            List<ShaderWrapper> localShaders = new List<ShaderWrapper>();
            List<Tuple<string, int, int, int>> computeRegisters = new List<Tuple<String, int, int, int>>();

            using (StreamReader sr = new StreamReader(path))
            {
                while (!sr.EndOfStream)
                {
                    String line = sr.ReadLine();
                    Match matchShaderRegex = regexWrapper.shaderRegex.Match(line);
                    Match matchCbufferRegex = regexWrapper.cbufferRegex.Match(line);
                    Match matchSamplerRegex = regexWrapper.samplerRegex.Match(line);
                    Match matchNumThreadsRegex = regexWrapper.numThreadsRegex.Match(line);

                    if (matchCbufferRegex.Success)
                    {
                        Match globalBufferMatch = regexWrapper.globalBufferRegex.Match(line);
                        Match registerMatch = regexWrapper.registerRegex.Match(line);
                        if (!registerMatch.Success)
                        {
                            throw new Exception("Unable to find register for constant buffer");
                        }
                        int cbufferRegister = Int32.Parse(registerMatch.Groups[1].Value);

                        // We have a new cbuffer
                        string cbufferName = matchCbufferRegex.Groups[1].Value;

                        string cbufferText = "";
                        while (!sr.EndOfStream)
                        {
                            line = sr.ReadLine();
                            if (line.Contains('{'))
                                continue;

                            if (line.Contains('}'))
                            {
                                if (m_ConstantBuffers.ContainsKey(cbufferName))
                                {
                                    m_ConstantBuffers[cbufferName].ParseConstantBuffer(cbufferText, cbufferRegister, globalBufferMatch.Success);
                                }
                                else
                                {
                                    CustomConstantBufferDefinition myNewConstantBuffer =
                                        new CustomConstantBufferDefinition(
                                            cbufferName,
                                            cbufferText,
                                            cbufferRegister,
                                            globalBufferMatch.Success,
                                            path);

                                    m_ConstantBuffers.Add(cbufferName, myNewConstantBuffer);

                                }
                                break;
                            }

                            cbufferText += line.Trim() + "\n";
                        }

                        continue;
                    }

                    if (matchShaderRegex.Success)
                    {
                        // We have a new shader
                        string shaderType = matchShaderRegex.Groups[1].Value;
                        string shaderName = matchShaderRegex.Groups[2].Value;
                        string shaderEntry = matchShaderRegex.Groups[3].Value;
                        string shaderDefines = matchShaderRegex.Groups[4].Value;
                        ShaderType type = ShaderType.PixelShader;

                        switch (shaderType.ToLower())
                        {
                            case "pixel": type = ShaderType.PixelShader;
                                break;
                            case "vertex": type = ShaderType.VertexShader;
                                break;
                            case "compute": type = ShaderType.ComputeShader;
                                break;
                            case "geometry": type = ShaderType.GeometryShader;
                                break;
                        }

                        HashSet<string> defines = new HashSet<String>();

                        if (shaderDefines.Length > 0)
                        {
                            var tokens = shaderDefines.Split(new String[] { " ", "\t" }, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 1; i < tokens.Length; ++i)
                            {
                                defines.Add(tokens[i]);
                            }
                        }

                        ShaderWrapper newShader = new ShaderWrapper()
                        {
                            m_FilePath = path,
                            m_ShaderName = shaderName,
                            m_ShaderType = type,
                            m_ShaderEntry = shaderEntry,
                            m_UsedIncludes = new HashSet<String>(),
                            m_Defines = defines
                        };

                        localShaders.Add(newShader);
                    }

                    if (matchNumThreadsRegex.Success)
                    {
                        int threadsX = Int32.Parse(matchNumThreadsRegex.Groups[1].Value);
                        int threadsY = Int32.Parse(matchNumThreadsRegex.Groups[2].Value);
                        int threadsZ = Int32.Parse(matchNumThreadsRegex.Groups[3].Value);

                        string nextLine = sr.ReadLine();
                        var tokens = nextLine.Split(new String[] { " ", "(" }, StringSplitOptions.RemoveEmptyEntries);

                        computeRegisters.Add(new Tuple<String, int, int, int>(tokens[1], threadsX, threadsY, threadsZ));
                    }

                    if (matchSamplerRegex.Success)
                    {
                        string samplerType = matchSamplerRegex.Groups[1].Value;
                        string samplerName = matchSamplerRegex.Groups[2].Value;
                        string samplerRegister = matchSamplerRegex.Groups[3].Value;

                        m_SamplerStates[Int32.Parse(samplerRegister)] = SamplerStates.GetSamplerStateForName(samplerName);
                    }

                }
            }

            foreach (var shader in localShaders)
            {
                CompileShader(shader, device);
            }

            foreach (var registers in computeRegisters)
            {
                var shaderFit = localShaders.Where
                (shader => shader.m_ShaderEntry == registers.Item1);

                foreach(var fittingShader in shaderFit)
                {
                    fittingShader.m_ThreadsX = registers.Item2;
                    fittingShader.m_ThreadsY = registers.Item3;
                    fittingShader.m_ThreadsZ = registers.Item4;
                }
            }
        }

        public static CustomConstantBufferInstance CreateConstantBufferInstance(String name, Device device)
        {
            if (m_ConstantBuffers.ContainsKey(name))
            {
                return new CustomConstantBufferInstance(m_ConstantBuffers[name], device);
            }
            throw new Exception("Unrecognized constant buffer" + name);
        }

        public static void BindSamplerStates(DeviceContext context)
        {
            foreach(var s in m_SamplerStates)
            {
                context.PixelShader.SetSampler(s.Value, s.Key);
                context.VertexShader.SetSampler(s.Value, s.Key);
                context.HullShader.SetSampler(s.Value, s.Key);
                context.DomainShader.SetSampler(s.Value, s.Key);
                context.GeometryShader.SetSampler(s.Value, s.Key);
                context.ComputeShader.SetSampler(s.Value, s.Key);
            }
        }

        public static void ExecuteComputeForResource(DeviceContext context, TextureObject textureResource, string shader)
        {
            ShaderWrapper wrapper = m_Shaders[shader];
            context.ComputeShader.Set(wrapper.m_ComputeShader);
            context.Dispatch(
                (textureResource.m_Width + wrapper.m_ThreadsX - 1) / wrapper.m_ThreadsX, 
                (textureResource.m_Height + wrapper.m_ThreadsY - 1) / wrapper.m_ThreadsY,
                (textureResource.m_Depth + wrapper.m_ThreadsZ - 1) / wrapper.m_ThreadsZ );
        }

        public static void ExecuteComputeForSize(DeviceContext context, int x, int y, int z, string shader)
        {
            ShaderWrapper wrapper = m_Shaders[shader];
            context.ComputeShader.Set(wrapper.m_ComputeShader);
            context.Dispatch(
                (x + wrapper.m_ThreadsX - 1) / wrapper.m_ThreadsX,
                (y + wrapper.m_ThreadsY - 1) / wrapper.m_ThreadsY,
                (z + wrapper.m_ThreadsZ - 1) / wrapper.m_ThreadsZ);
        }
        
        public static List<CustomConstantBufferDefinition> GetConstantBufferDefinitions()
        {
            return m_ConstantBuffers.Values.ToList();
        }

        private static void CompileShader(ShaderWrapper shader, Device device)
        {
            // hack to add includes to list to allow easy reloading after include has changed...
            m_CurrentlyProcessedShader = shader;
            bool done = false;
            while (!done)
            {
                try
                {
                    var defines = new ShaderMacro[shader.m_Defines.Count];
                    int counter = 0;
                    foreach (var define in shader.m_Defines)
                    {
                        defines[counter++] = new ShaderMacro(define, "1");
                    }

                    switch (shader.m_ShaderType)
                    {
                        case ShaderType.PixelShader:
                            using (var bytecode = ShaderBytecode.CompileFromFile(shader.m_FilePath, shader.m_ShaderEntry, "ps_5_0", ShaderFlags.WarningsAreErrors, EffectFlags.None, defines, m_Include))
                                shader.m_PixelShader = new PixelShader(device, bytecode);
                            break;
                        case ShaderType.ComputeShader:
                            using (var bytecode = ShaderBytecode.CompileFromFile(shader.m_FilePath, shader.m_ShaderEntry, "cs_5_0", ShaderFlags.WarningsAreErrors, EffectFlags.None, defines, m_Include))
                                shader.m_ComputeShader = new ComputeShader(device, bytecode);
                            break;
                        case ShaderType.GeometryShader:
                            using (var bytecode = ShaderBytecode.CompileFromFile(shader.m_FilePath, shader.m_ShaderEntry, "gs_5_0", ShaderFlags.WarningsAreErrors, EffectFlags.None, defines, m_Include))
                                shader.m_GeometryShader = new GeometryShader(device, bytecode);
                            break;
                        case ShaderType.VertexShader:
                            using (var bytecode = ShaderBytecode.CompileFromFile(shader.m_FilePath, shader.m_ShaderEntry, "vs_5_0", ShaderFlags.WarningsAreErrors, EffectFlags.None, defines, m_Include))
                            {
                                shader.m_VertexInputSignature = ShaderSignature.GetInputSignature(bytecode);
                                shader.m_VertexShader = new VertexShader(device, bytecode);
                            }
                            break;
                    }

                    done = true;
                }
                catch(Exception e)
                {
                    System.Windows.Forms.MessageBox.Show(e.Message, "Shader compilation error" );
                }
            }
            m_CurrentlyProcessedShader = null;

            if (m_Shaders.ContainsKey(shader.m_ShaderName))
            {
                m_Shaders[shader.m_ShaderName] = shader;
            }
            else
            {
                m_Shaders.Add(shader.m_ShaderName, shader);
            }
        }

        private static void OnFileChanged(object source, FileSystemEventArgs e)
        {
            bool contains = m_ConstantBuffers.Values.Where
                (cb => cb.m_FilePath == e.FullPath)
                .Count() > 0 
                ||
                m_Shaders.Values.Where
                (shader => shader.m_FilePath == e.FullPath)
                .Count() > 0;
            var shadersUsingInclude = m_Shaders.Values.Where
                (shader => shader.m_UsedIncludes.Contains(e.FullPath));


            lock(m_Lock)
            {
                if(contains)
                {
                    m_FilesToReload.Add(e.FullPath);
                }
                m_FilesToReload.UnionWith(shadersUsingInclude.Select( shader => shader.m_FilePath) );
            }
        } 

        public static void UpdateShaderManager(Device device)
        {
            HashSet<string> filesToReload = new HashSet<string>();

            lock (m_Lock)
            {
                filesToReload.UnionWith(m_FilesToReload);
                m_FilesToReload.Clear();
            }

            foreach (var fileToReload in filesToReload)
            {
                ParseFile(device, fileToReload);
            }

            lock(CustomConstantBufferInstance.m_Lock)
            {
                foreach (var bufferInstance in CustomConstantBufferInstance.s_AllInstances)
                {
                    bufferInstance.CreateBufferInstance(bufferInstance.m_Definition, device, false);
                }
            }
        }

        public static VertexShader GetVertexShader(string name)
        {
            return m_Shaders[name].m_VertexShader;
        }

        public static GeometryShader GetGeometryShader(string name)
        {
            return m_Shaders[name].m_GeometryShader;
        }

        public static ShaderSignature GetVertexShaderSignature(string name)
        {
            return m_Shaders[name].m_VertexInputSignature;
        }

        public static PixelShader GetPixelShader(string name)
        {
            return m_Shaders[name].m_PixelShader;
        }

        public static ComputeShader GetComputeShader(string name)
        {
            return m_Shaders[name].m_ComputeShader;
        }
    }
}

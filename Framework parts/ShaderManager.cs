using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using Device = SlimDX.Direct3D11.Device;
using System.IO;
using String = System.String;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

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

        class ShaderFile
        {
            public string m_FileName;
            public string m_FilePath;
            public List<String> m_DirectlyIncludedFiles;
            public HashSet<String> m_FlattenedFiles;
        }

        class ShaderWrapper
        {
            public ShaderType m_ShaderType;
            public string m_ShaderName;
            public string m_ShaderEntry;
            public ShaderFile m_ShaderFile;
            public DeviceChild m_ShaderObject;
            public ShaderBytecode m_ShaderBytecode;
            public ShaderSignature m_VertexInputSignature;
            public HashSet<string> m_Defines;
            public Task m_ShaderCompilationTask;
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
                stream = new FileStream(includeDirectory + fileName, FileMode.Open, FileAccess.Read);
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
        static Dictionary<string, float> m_GlobalDefineValues;
        static Dictionary<string, ShaderFile> m_AllShaderFiles;
        
        class RegexWrapper
        {
            public Regex shaderRegex;
            public Regex cbufferRegex;
            public Regex samplerRegex;
            public Regex numThreadsRegex;
            public Regex registerRegex;
            public Regex globalBufferRegex;
            public Regex globalDefineRegex;
            public Regex includeRegex;

            public RegexWrapper()
            {
                shaderRegex = new Regex(@"//[ \t]+(Compute|Pixel|Vertex|Geometry)Shader:[ \t]+(\w+)?,[ \t]+entry:[ \t]+(\w+),?[ \t]*(defines:([ \t]*\w+)*)?", RegexOptions.IgnoreCase);
                cbufferRegex = new Regex(@"[ \t]*cbuffer[ \t]+(\w+)[ \t\n\r]*{?", RegexOptions.IgnoreCase);
                samplerRegex = new Regex(@"[ \t]*(SamplerState|SamplerComparisonState)[ \t]+(\w+)[ \t]+:[ \t]+register\(s(\w+)\)", RegexOptions.IgnoreCase);
                numThreadsRegex = new Regex(@"\[numthreads\((\w+),[ \t]*(\w+),[ \t]*(\w+)\)\]", RegexOptions.IgnoreCase);
                registerRegex = new Regex(@"register\(b([\d]+)\)", RegexOptions.IgnoreCase);
                globalBufferRegex = new Regex(@"//[ \t]*Global", RegexOptions.IgnoreCase);
                globalDefineRegex = new Regex(@"#define[ \t]*(\w+)[ \t]*([\d.f]+)[ \t]*//[ \t]*GlobalDefine", RegexOptions.IgnoreCase);
                includeRegex = new Regex(@"#include[ \t]*""([\w.]+)""", RegexOptions.IgnoreCase);
            }
        };
        static ShaderManager()
        {
            m_Initialized = false;
            m_Shaders = new Dictionary<string, ShaderWrapper>();
            m_ConstantBuffers = new Dictionary<string, CustomConstantBufferDefinition>();
            m_Watchers = new List<FileSystemWatcher>();
            m_SamplerStates = new Dictionary<int, SamplerState>();
            m_Lock = new Object();
            m_FilesToReload = new HashSet<string>();
            m_RegexWrapper = new RegexWrapper();
            m_GlobalDefineValues = new Dictionary<string, float>();
            m_AllShaderFiles = new Dictionary<String, ShaderFile>();
        }

        static RegexWrapper m_RegexWrapper;

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        static public void Initialize(Device device)
        {
            if (m_Initialized)
            {
                throw new Exception("Already initialized!");
            }

            string[] filters = new[] { "*.hlsl", "*.fx" };

            foreach (string f in filters)
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
                string fileName = GetFileNameFromPath(path);
                ShaderFile sf = new ShaderFile();
                sf.m_DirectlyIncludedFiles = new List<String>();
                sf.m_FileName = fileName;
                sf.m_FilePath = path;
                sf.m_FlattenedFiles = new HashSet<String>();
                
                m_AllShaderFiles.Add(fileName, sf);
            }

            foreach (string path in filePaths)
            {
                ParseFile(path);
            }
            FlattenIncludes();
            FinalizeCompilationOnDevice(device);
        }

        private static String GetFileNameFromPath(string path)
        {
            string[] splitPath = path.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            string fileName = splitPath[splitPath.Length - 1];
            return fileName;
        }

        private static void FlattenIncludes()
        {
            bool includesChanged = true;
            
            while(includesChanged)
            {
                includesChanged = false;
                foreach(var sf in m_AllShaderFiles.Values)
                {
                    int includeCount = sf.m_FlattenedFiles.Count;
                    var newIncludes = new HashSet<String>();

                    foreach (var include in sf.m_FlattenedFiles)
                    {
                        newIncludes.UnionWith(m_AllShaderFiles[include].m_FlattenedFiles);
                    }
                    sf.m_FlattenedFiles.UnionWith(newIncludes);

                    if (sf.m_FlattenedFiles.Count != includeCount)
                    {
                        includesChanged = true;
                    }
                }
            }
        }

        private static void FinalizeCompilationOnDevice(Device device)
        {
            foreach (var shader in m_Shaders.Values)
            {
                if (shader.m_ShaderCompilationTask != null)
                {
                    shader.m_ShaderCompilationTask.Wait();
                    shader.m_ShaderCompilationTask.Dispose();
                    shader.m_ShaderCompilationTask = null;

                    FinalizeShader(shader, device);
                }
            }
        }

        private static void ParseFile(string path)
        {
            List<ShaderWrapper> localShaders = new List<ShaderWrapper>();
            List<Tuple<string, int, int, int>> computeRegisters = new List<Tuple<String, int, int, int>>();
            ShaderFile sf = m_AllShaderFiles[GetFileNameFromPath(path)];
            sf.m_DirectlyIncludedFiles.Clear();
            sf.m_FlattenedFiles.Clear();

            using (StreamReader sr = new StreamReader(path))
            {
                while (!sr.EndOfStream)
                {
                    String line = sr.ReadLine();
                    Match matchShaderRegex = m_RegexWrapper.shaderRegex.Match(line);
                    Match matchCbufferRegex = m_RegexWrapper.cbufferRegex.Match(line);
                    Match matchSamplerRegex = m_RegexWrapper.samplerRegex.Match(line);
                    Match matchNumThreadsRegex = m_RegexWrapper.numThreadsRegex.Match(line);
                    Match matchGlobalDefineRegex = m_RegexWrapper.globalDefineRegex.Match(line);
                    Match matchIncludeRegex = m_RegexWrapper.includeRegex.Match(line);

                    if (matchIncludeRegex.Success)
                    {
                        string includeName = matchIncludeRegex.Groups[1].Value;
                        sf.m_DirectlyIncludedFiles.Add(includeName);
                    }

                    if (matchGlobalDefineRegex.Success)
                    {
                        string defineName = matchGlobalDefineRegex.Groups[1].Value;
                        float value = Single.Parse(matchGlobalDefineRegex.Groups[2].Value, CultureInfo.InvariantCulture);

                        if (m_GlobalDefineValues.ContainsKey(defineName))
                        {
                            m_GlobalDefineValues[defineName] = value;
                        }
                        else
                        {
                            m_GlobalDefineValues.Add(defineName, value);
                        }
                    }

                    if (matchCbufferRegex.Success)
                    {
                        Match globalBufferMatch = m_RegexWrapper.globalBufferRegex.Match(line);
                        Match registerMatch = m_RegexWrapper.registerRegex.Match(line);
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
                            m_ShaderFile = sf,
                            m_ShaderName = shaderName,
                            m_ShaderType = type,
                            m_ShaderEntry = shaderEntry,
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
                if (m_Shaders.ContainsKey(shader.m_ShaderName))
                {
                    m_Shaders[shader.m_ShaderName] = shader;
                }
                else
                {
                    m_Shaders.Add(shader.m_ShaderName, shader);
                }

                // CompileShader(shader);
                if (shader.m_ShaderCompilationTask != null)
                    throw new Exception("Already compiling");

                shader.m_ShaderCompilationTask = Task.Factory.StartNew(() => CompileShader(shader));
            }

            sf.m_FlattenedFiles.Add(sf.m_FileName);
            sf.m_FlattenedFiles.UnionWith(sf.m_DirectlyIncludedFiles);

            foreach (var registers in computeRegisters)
            {
                var shaderFit = localShaders.Where
                (shader => shader.m_ShaderEntry == registers.Item1);

                foreach (var fittingShader in shaderFit)
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
            foreach (var s in m_SamplerStates)
            {
                context.PixelShader.SetSampler(s.Value, s.Key);
                context.VertexShader.SetSampler(s.Value, s.Key);
                context.HullShader.SetSampler(s.Value, s.Key);
                context.DomainShader.SetSampler(s.Value, s.Key);
                context.GeometryShader.SetSampler(s.Value, s.Key);
                context.ComputeShader.SetSampler(s.Value, s.Key);
            }
        }

        private static void BindDebugUAV(DeviceContext context)
        {
            if (DebugManager.m_GPUDebugOn)
            {
                context.ComputeShader.SetUnorderedAccessView(DebugManager.m_DebugAppendBuffer.m_UnorderedAccessView, 7, DebugManager.m_FirstCallThisFrame ? 0 : -1);
                DebugManager.m_FirstCallThisFrame = false;
            }
        }

        public static void ExecuteComputeForResource(DeviceContext context, TextureObject textureResource, string shader)
        {
            BindDebugUAV(context);

            ShaderWrapper wrapper = m_Shaders[shader];
            context.ComputeShader.Set((ComputeShader)wrapper.m_ShaderObject);
            context.Dispatch(
                (textureResource.m_Width + wrapper.m_ThreadsX - 1) / wrapper.m_ThreadsX,
                (textureResource.m_Height + wrapper.m_ThreadsY - 1) / wrapper.m_ThreadsY,
                (textureResource.m_Depth + wrapper.m_ThreadsZ - 1) / wrapper.m_ThreadsZ);
        }

        public static void ExecuteComputeForSize(DeviceContext context, int x, int y, int z, string shader)
        {
            BindDebugUAV(context);

            ShaderWrapper wrapper = m_Shaders[shader];
            context.ComputeShader.Set((ComputeShader)wrapper.m_ShaderObject);
            context.Dispatch(
                (x + wrapper.m_ThreadsX - 1) / wrapper.m_ThreadsX,
                (y + wrapper.m_ThreadsY - 1) / wrapper.m_ThreadsY,
                (z + wrapper.m_ThreadsZ - 1) / wrapper.m_ThreadsZ);
        }

        public static float GetShaderDefine(string name)
        {
            return m_GlobalDefineValues[name];
        }

        public static uint GetUIntShaderDefine(string name)
        {
            float value = m_GlobalDefineValues[name];

            // if someone tried to use it for huge ints or negative values, then we need to find other, less hacky way
            System.Diagnostics.Debug.Assert((float)((uint)value) == value);

            return (uint)value;
        }


        public static List<CustomConstantBufferDefinition> GetConstantBufferDefinitions()
        {
            return m_ConstantBuffers.Values.ToList();
        }

        private static void FinalizeShader(ShaderWrapper shader, Device device)
        {
            switch (shader.m_ShaderType)
            {
                case ShaderType.PixelShader:
                    shader.m_ShaderObject = new PixelShader(device, shader.m_ShaderBytecode);
                    break;
                case ShaderType.ComputeShader:
                    shader.m_ShaderObject = new ComputeShader(device, shader.m_ShaderBytecode);
                    break;
                case ShaderType.GeometryShader:
                    shader.m_ShaderObject = new GeometryShader(device, shader.m_ShaderBytecode);
                    break;
                case ShaderType.VertexShader:
                    shader.m_VertexInputSignature = ShaderSignature.GetInputSignature(shader.m_ShaderBytecode);
                    shader.m_ShaderObject = new VertexShader(device, shader.m_ShaderBytecode);
                    break;
            }
            shader.m_ShaderBytecode.Dispose();
            shader.m_ShaderBytecode = null;
        }

        private static void CompileShader(ShaderWrapper shader)
        {
            bool done = false;
            bool acquiredLock = false;
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

                    string profile = "";

                    switch (shader.m_ShaderType)
                    {
                        case ShaderType.PixelShader:
                            profile = "ps_5_0";
                            break;
                        case ShaderType.ComputeShader:
                            profile = "cs_5_0";
                            break;
                        case ShaderType.GeometryShader:
                            profile = "gs_5_0";
                            break;
                        case ShaderType.VertexShader:
                            profile = "vs_5_0";
                            break;
                    }
                    shader.m_ShaderBytecode = ShaderBytecode.CompileFromFile(shader.m_ShaderFile.m_FilePath, shader.m_ShaderEntry, profile, ShaderFlags.WarningsAreErrors, EffectFlags.None, defines, m_Include);

                    done = true;
                }
                catch (Exception e)
                {
                    // if error - we need to enter synchronized state
                    if (!acquiredLock)
                    {
                        System.Threading.Monitor.TryEnter(m_Lock, ref acquiredLock);
                    }

                    // if we are first to aquire lock - show message box, allowing user to fix shader
                    if (acquiredLock)
                    {
                        System.Windows.Forms.MessageBox.Show(e.Message, "Shader compilation error");
                    }
                    else
                    {
                        // otherwise just enter without showing mb, will retry compilation after first shader is fixed
                        System.Threading.Monitor.Enter(m_Lock, ref acquiredLock);
                    }
                }
            }

            if (acquiredLock)
            {
                System.Threading.Monitor.Exit(m_Lock);
            }
        }

        private static void OnFileChanged(object source, FileSystemEventArgs e)
        {
            var shadersUsingInclude = m_Shaders.Values.Where
                (shader => shader.m_ShaderFile.m_FlattenedFiles.Contains(e.Name));

            lock (m_Lock)
            {
                m_FilesToReload.UnionWith(shadersUsingInclude.Select(shader => shader.m_ShaderFile.m_FilePath));
            }
        }

        public static bool UpdateShaderManager(Device device)
        {
            HashSet<string> filesToReload = new HashSet<string>();

            lock (m_Lock)
            {
                filesToReload.UnionWith(m_FilesToReload);
                m_FilesToReload.Clear();
            }

            if (filesToReload.Count > 0)
            {
                foreach (var fileToReload in filesToReload)
                {
                    ParseFile(fileToReload);
                }

                FlattenIncludes();
                FinalizeCompilationOnDevice(device);

                lock (CustomConstantBufferInstance.m_Lock)
                {
                    foreach (var bufferInstance in CustomConstantBufferInstance.s_AllInstances)
                    {
                        bufferInstance.CreateBufferInstance(bufferInstance.m_Definition, device, false);
                    }
                }

                return true;
            }

            return false;
        }

        public static VertexShader GetVertexShader(string name)
        {
            return (VertexShader)m_Shaders[name].m_ShaderObject;
        }

        public static GeometryShader GetGeometryShader(string name)
        {
            return (GeometryShader)m_Shaders[name].m_ShaderObject;
        }

        public static ShaderSignature GetVertexShaderSignature(string name)
        {
            return m_Shaders[name].m_VertexInputSignature;
        }

        public static PixelShader GetPixelShader(string name)
        {
            return (PixelShader)m_Shaders[name].m_ShaderObject;
        }

        public static ComputeShader GetComputeShader(string name)
        {
            return (ComputeShader)m_Shaders[name].m_ShaderObject;
        }
    }
}

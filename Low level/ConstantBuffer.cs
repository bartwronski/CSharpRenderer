using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using SlimDX;
using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.InteropServices;
using SlimDX.Direct3D11;
using NLua;

namespace CSharpRenderer
{
    class CustomConstantBufferDefinition
    {
        public enum ConstantType
        {
            ConstantType_Float,
            ConstantType_Int,
            ConstantType_Float4,
            ConstantType_Matrix44,
        }

        public static string[] ConstantTypeStrings = {"float", "int", "float4", "float4x4"};

        public static object GetDefaultObjectForType(ConstantType type)
        {
            switch(type)
            {
                case ConstantType.ConstantType_Float:
                    return new Single();
                case ConstantType.ConstantType_Int:
                    return new Int32();
                case ConstantType.ConstantType_Float4:
                    return new Vector4();
                case ConstantType.ConstantType_Matrix44:
                    return new Matrix();
                default:
                    throw new Exception("GetDefaultObjectForType: unknown type!");
            }
        }

        public static ConstantType FindType(String typeDescriptor)
        {
            switch(typeDescriptor)
            {
                case "float":
                    return ConstantType.ConstantType_Float;
                case "int":
                    return ConstantType.ConstantType_Int;
                case "float4":
                    return ConstantType.ConstantType_Float4;
                case "float4x4":
                    return ConstantType.ConstantType_Matrix44;
                default:
                    throw new Exception("FindType: Unrecognized type");
            }
        }

        public static int GetByteSizeOfType(ConstantType type)
        {
            switch (type)
            {
                case ConstantType.ConstantType_Float:
                    return 4;
                case ConstantType.ConstantType_Int:
                    return 4;
                case ConstantType.ConstantType_Float4:
                    return 4*4;
                case ConstantType.ConstantType_Matrix44:
                    return 4*4*4;
                default:
                    throw new Exception("GetByteSizeOfType: unknown type!");
            }
        }


        public class ConstantBufferPropertyField
        {
            public ConstantType type;
            public int byteOffset;
            public bool isParam;
            public object paramValue;
            public float paramRangeMin;
            public float paramRangeMax;
            public bool isGamma;
            public bool isScripted;
            public String name;

            public ConstantBufferPropertyField(String name, ConstantType type, int offset)
            {
                this.type = type;
                this.name = name;
                byteOffset = offset;
                isParam = false;
                isScripted = false;
                isGamma = false;
                paramValue = null;
                paramRangeMin = -Single.MaxValue;
                paramRangeMax = Single.MaxValue;
            }
        }

        Dictionary<String,ConstantBufferPropertyField> m_MemberConstants;
        public String m_Name;
        public int m_Size;
        public int m_Register;
        public bool m_IsGlobal;
        public string m_Script;
        public string m_FilePath;

        public object GetDefaultObjectForField(String name)
        {
            if (m_MemberConstants.ContainsKey(name))
            {
                return m_MemberConstants[name].paramValue;
            }
            throw new Exception("Unknown field " + name);
        }

        public ConstantBufferPropertyField GetPropertyForName(String name)
        {
            return m_MemberConstants[name];
        }

        public Dictionary<String, ConstantBufferPropertyField> GetProperties()
        {
            return m_MemberConstants;
        }

        public List<ConstantBufferPropertyField> GetParamProperties()
        {
            return m_MemberConstants.Values.Where(val => val.isParam == true).ToList();
        }

        public List<ConstantBufferPropertyField> GetScriptedProperties()
        {
            return m_MemberConstants.Values.Where(val => val.isScripted == true).ToList();
        }

        public CustomConstantBufferDefinition(
            String name, 
            String constantBufferString, 
            int register, 
            bool isGlobal, 
            string path )
        {
            m_Name = name;
            m_FilePath = path;
            m_MemberConstants = new Dictionary<String, ConstantBufferPropertyField>();

            ParseConstantBuffer(constantBufferString, register, isGlobal);
        }

        public void ParseConstantBuffer(String constantBufferString, int register, bool isGlobal)
        {
            m_IsGlobal = isGlobal;
            m_MemberConstants.Clear();
            m_Register = register;

            char[] separator = { '\n' };
            string[] lines = constantBufferString.Split(separator);
            CultureInfo ci = new CultureInfo("en-US", false);
            string types = "";

            foreach (string type in ConstantTypeStrings)
            {
                types += types.Length == 0 ? type : "|" + type;
            }

            Regex variableRegex = new Regex(@"[ \t]*(" + types + @")[ \t]+(\w+);", RegexOptions.IgnoreCase);

            Regex paramRegex = new Regex(@"//[ \t]*Param(,[ \t]*\w+:?[ \t]*[\d\.\-]*)+", RegexOptions.IgnoreCase);
            Regex scriptedRegex = new Regex(@"//[ \t]*Scripted", RegexOptions.IgnoreCase);
            Regex paramOptionsRegex = new Regex(@",[ \t]*(\w+):?([ \t]*[\d\.\-]*)", RegexOptions.IgnoreCase);
            // Example
            /* // Param, Default: 1.0, Range:0.0-3.0, Linear */

            int offset = 0;

            for (int it = 0; it < lines.Length; ++it)
            {
                string line = lines[it];

                Match m = variableRegex.Match(line);

                if (m.Success)
                {
                    string variableType = m.Groups[1].Value;
                    string variableName = m.Groups[2].Value;

                    ConstantType type = FindType(variableType);

                    int byteSize = GetByteSizeOfType(type);

                    // padding to next float4
                    int padding = Math.Min(byteSize, 16);
                    if (offset % padding != 0)
                    {
                        offset += padding - offset % padding;
                    }

                    ConstantBufferPropertyField addedProperty = new ConstantBufferPropertyField(variableName, type, offset);

                    Match paramMatch = paramRegex.Match(line);
                    if (paramMatch.Success)
                    {
                        addedProperty.isParam = true;

                        MatchCollection paramOptionsMatch = paramOptionsRegex.Matches(line);
                        foreach (Match optionsMatch in paramOptionsMatch)
                        {
                            string optionName = optionsMatch.Groups[1].Value.ToLower();
                            string optionValue = optionsMatch.Groups[2].Value;

                            switch (optionName)
                            {
                                case "default":
                                    {
                                        if (addedProperty.type != ConstantType.ConstantType_Float)
                                        {
                                            throw new Exception("Unsupported type yet");
                                        }
                                        float value = Single.Parse(optionValue, ci.NumberFormat);
                                        addedProperty.paramValue = value;
                                    }
                                    break;
                                case "gamma":
                                    {
                                        if (addedProperty.type != ConstantType.ConstantType_Float)
                                        {
                                            throw new Exception("Unsupported type yet");
                                        }
                                        addedProperty.isGamma = true;
                                    }
                                    break;
                                case "range":
                                    {
                                        if (addedProperty.type != ConstantType.ConstantType_Float)
                                        {
                                            throw new Exception("Unsupported type yet");
                                        }
                                        char[] rangeSeparator = { '-' };
                                        string[] splitStrings = optionValue.Split(rangeSeparator);

                                        float minValue = Single.Parse(splitStrings[0], ci.NumberFormat);
                                        float maxValue = Single.Parse(splitStrings[1], ci.NumberFormat);
                                        addedProperty.paramRangeMin = minValue;
                                        addedProperty.paramRangeMax = maxValue;
                                    }
                                    break;

                            }
                        }
                        if (addedProperty.isGamma)
                        {
                            float paramValueToTransform = ((float)addedProperty.paramValue - addedProperty.paramRangeMin) / (addedProperty.paramRangeMax - addedProperty.paramRangeMin);
                            paramValueToTransform = (float)Math.Pow((double)paramValueToTransform, 1.0 / 2.2);
                            addedProperty.paramValue = paramValueToTransform * (addedProperty.paramRangeMax - addedProperty.paramRangeMin) + addedProperty.paramRangeMin;
                        }
                    }
                    Match scriptedMatch = scriptedRegex.Match(line);
                    if (scriptedMatch.Success)
                    {
                        addedProperty.isScripted = true;
                    }

                    m_MemberConstants.Add(variableName, addedProperty);
                    offset += byteSize;
                }

                if (line.Contains("BEGINSCRIPT"))
                {
                    string script = "";
                    bool finish = false;
                    do
                    {
                        ++it;
                        line = lines[it];
                        finish = line.Contains("ENDSCRIPT");
                        if (!finish)
                        {
                            script += line + "\n";
                        }
                    } while (!finish);
                    m_Script = script;
                }
            }

            // Final padding for whole buffer, after packing everything
            if (offset % 16 != 0)
            {
                offset += 16 - offset % 16;
            }

            m_Size = offset;
        }       
    }

    class CustomConstantBufferInstance : DynamicObject
    {
        public byte[]                          m_DataBuffer;
        public int                             m_Size;
        public MemoryStream                    m_DataStream;
        public BinaryWriter                    m_Writer;
        public String                          m_DefinitionName;
        public CustomConstantBufferDefinition  m_Definition;
        public Dictionary<String, object>      m_Values;
        public SlimDX.Direct3D11.Buffer        m_ConstantBuffer;

        public static object            m_Lock;
        public static HashSet<CustomConstantBufferInstance> s_AllGlobalInstances;
        public static HashSet<CustomConstantBufferInstance> s_AllInstances;

        static CustomConstantBufferInstance()
        {
            m_Lock = new Object();
            s_AllGlobalInstances = new HashSet<CustomConstantBufferInstance>();
            s_AllInstances = new HashSet<CustomConstantBufferInstance>();
        }

        public CustomConstantBufferInstance(CustomConstantBufferDefinition baseDefinition, Device device)
        {
            CreateBufferInstance(baseDefinition, device, true);

            lock(m_Lock)
            {
                if (baseDefinition.m_IsGlobal)
                {
                    // todo never destroyed
                    s_AllGlobalInstances.Add(this);
                }
                s_AllInstances.Add(this);
            }
        }

        public void CreateBufferInstance(CustomConstantBufferDefinition baseDefinition, Device device, bool newInstance)
        {
            if(!newInstance)
            {
                m_Writer.Dispose();
                m_Writer = null;

                m_DataStream.Dispose();
                m_DataStream = null;

                m_ConstantBuffer.Dispose();
                m_ConstantBuffer = null;
            }
            else
            {
                m_Values = new Dictionary<String, object>();
            }

            m_Definition = baseDefinition;
            m_DefinitionName = baseDefinition.m_Name;
            m_DataBuffer = new byte[baseDefinition.m_Size];
            m_Size = baseDefinition.m_Size;
            m_DataStream = new MemoryStream(m_DataBuffer, 0, baseDefinition.m_Size, true);
            m_Writer = new BinaryWriter(m_DataStream);
            m_ConstantBuffer = new SlimDX.Direct3D11.Buffer(
                device,
                baseDefinition.m_Size,
                ResourceUsage.Dynamic,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.Write,
                ResourceOptionFlags.None,
                0);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if(m_Values.ContainsKey(binder.Name))
            {
                result = m_Values[binder.Name];
            }
            else
            {
                result = m_Definition.GetDefaultObjectForField(binder.Name);
            }
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            object defaultObject = m_Definition.GetDefaultObjectForField(binder.Name);
            if (defaultObject == null || defaultObject.GetType() == value.GetType())
            {
                m_Values[binder.Name] = value;
                return true;
            }

            return false;
        }

        public byte[] GetByteBuffer()
        {
            return m_DataBuffer;
        }

        public int GetByteBufferSize()
        {
            return m_Size;
        }

        private byte[] StructToByteArray(object o)
        {
            try
            {
                // This function copies the structure data into a byte[] 

                //Set the buffer to the correct size 
                byte[] buffer = new byte[Marshal.SizeOf(o)];

                //Allocate the buffer to memory and pin it so that GC cannot use the 
                //space (Disable GC) 
                GCHandle h = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                // copy the struct into int byte[] mem alloc 
                Marshal.StructureToPtr(o, h.AddrOfPinnedObject(), false);

                h.Free(); //Allow GC to do its job 

                return buffer; // return the byte[]. After all that's why we are here 
                // right. 
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Bind(DeviceContext context)
        {
            ContextHelper.SetConstantBuffer(context, m_ConstantBuffer, m_Definition.m_Register);
        }

        public void CompileAndBind(DeviceContext context)
        {
            foreach(var v in m_Values)
            {
                var property = m_Definition.GetPropertyForName(v.Key);
                m_Writer.Seek(property.byteOffset, SeekOrigin.Begin);
                CustomConstantBufferDefinition.ConstantType type = property.type;
                Object value = v.Value;

                BufferWriteType(type, value);
            }

            var paramProperties = m_Definition.GetParamProperties();
            foreach (var paramPropertyP in paramProperties)
            {
                m_Writer.Seek(paramPropertyP.byteOffset, SeekOrigin.Begin);
                CustomConstantBufferDefinition.ConstantType type = paramPropertyP.type;
                Object value = paramPropertyP.paramValue;

                BufferWriteType(type, value);
            }

            var scriptedProperties = m_Definition.GetScriptedProperties();
            if (scriptedProperties.Count > 0)
            {
                using( Lua luaContext = new Lua() )
                {
                    foreach (var globalBuffer in s_AllGlobalInstances)
                    {
                        foreach (var val in globalBuffer.m_Values)
                        {
                            string name = val.Key;
                            object value = val.Value;
                            FillScriptLuaContext(luaContext, name, value);
                        }

                        var bufferParamProperties = globalBuffer.m_Definition.GetParamProperties();
                        foreach (var param in bufferParamProperties)
                        {
                            string name = param.name;
                            object value = param.paramValue;
                            FillScriptLuaContext(luaContext, name, value);
                        }
                    }
                    if (!s_AllGlobalInstances.Contains(this))
                    {
                        foreach (var val in m_Values)
                        {
                            string name = val.Key;
                            object value = val.Value;
                            FillScriptLuaContext(luaContext, name, value);
                        }

                        var bufferParamProperties = m_Definition.GetParamProperties();
                        foreach (var param in bufferParamProperties)
                        {
                            string name = param.name;
                            object value = param.paramValue;
                            FillScriptLuaContext(luaContext, name, value);
                        }
                    }

                    // Execute!
                    luaContext.DoString(m_Definition.m_Script);

                    foreach (var scriptedProperty in scriptedProperties)
                    {
                        m_Writer.Seek(scriptedProperty.byteOffset, SeekOrigin.Begin);
                        CustomConstantBufferDefinition.ConstantType type = scriptedProperty.type;
                        float value = (float)(double)luaContext[scriptedProperty.name];

                        BufferWriteType(type, value);
                    }
                }
            }

            m_Writer.Flush();

            ContextHelper.SetConstantBuffer(context, null, m_Definition.m_Register);

            DataBox box = context.MapSubresource(m_ConstantBuffer, MapMode.WriteDiscard, SlimDX.Direct3D11.MapFlags.None);
            box.Data.Write(m_DataBuffer, 0, m_Size);
            context.UnmapSubresource(m_ConstantBuffer, 0);

            ContextHelper.SetConstantBuffer(context, m_ConstantBuffer, m_Definition.m_Register);

        }

        private void BufferWriteType(CustomConstantBufferDefinition.ConstantType type, Object value)
        {
            switch (type)
            {
                case CustomConstantBufferDefinition.ConstantType.ConstantType_Float:
                    m_Writer.Write((Single)value);
                    break;
                case CustomConstantBufferDefinition.ConstantType.ConstantType_Int:
                    m_Writer.Write((Int32)value);
                    break;
                default:
                    m_Writer.Write(StructToByteArray(value));
                    break;
            }
        }

        private static void FillScriptLuaContext(Lua luaContext, string name, object value)
        {
            if (value.GetType() == typeof(float))
            {
                luaContext[name] = (float)value;
            }
            else if (value.GetType() == typeof(Vector4))
            {
                luaContext[name + "_x"] = ((Vector4)value).X;
                luaContext[name + "_y"] = ((Vector4)value).Y;
                luaContext[name + "_z"] = ((Vector4)value).Z;
                luaContext[name + "_w"] = ((Vector4)value).W;
            }
        }
    }
}

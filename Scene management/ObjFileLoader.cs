using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;
using SlimDX;
using System.Runtime.InteropServices;

namespace CSharpRenderer
{
    public static class StructTools
    {
        /// <summary>
        /// converts byte[] to struct
        /// </summary>
        public static T RawDeserialize<T>(byte[] rawData, int position)
        {
            int rawsize = Marshal.SizeOf(typeof(T));
            if (rawsize > rawData.Length - position)
                throw new ArgumentException("Not enough data to fill struct. Array length from position: " + (rawData.Length - position) + ", Struct length: " + rawsize);
            IntPtr buffer = Marshal.AllocHGlobal(rawsize);
            Marshal.Copy(rawData, position, buffer, rawsize);
            T retobj = (T)Marshal.PtrToStructure(buffer, typeof(T));
            Marshal.FreeHGlobal(buffer);
            return retobj;
        }

        /// <summary>
        /// converts a struct to byte[]
        /// </summary>
        public static byte[] RawSerialize(object anything)
        {
            int rawSize = Marshal.SizeOf(anything);
            IntPtr buffer = Marshal.AllocHGlobal(rawSize);
            Marshal.StructureToPtr(anything, buffer, false);
            byte[] rawDatas = new byte[rawSize];
            Marshal.Copy(buffer, rawDatas, 0, rawSize);
            Marshal.FreeHGlobal(buffer);
            return rawDatas;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 32, Pack = 1)]
    struct MyVertex
    {
        [FieldOffset(0)]
        public float x;
        [FieldOffset(4)]
        public float y;
        [FieldOffset(8)]
        public float z;

        [FieldOffset(12)]
        public float nx;
        [FieldOffset(16)]
        public float ny;
        [FieldOffset(20)]
        public float nz;
        [FieldOffset(24)]
        public float u;
        [FieldOffset(28)]
        public float v;
    }

    class ObjFileLoader
    {
        public List<MyVertex> m_Vertices;
        public List<Int32> m_Indices;
        public List<Int32> m_MaterialIndices;
        public List<String> m_Materials;
        public Dictionary<String, String> m_MatTexMappingDiffuse;
        public Dictionary<String, String> m_MatTexMappingNormalMap;
        public Vector3 m_BoundingBoxMin;
        public Vector3 m_BoundingBoxMax;

        public ObjFileLoader()
        {
            m_Vertices = new List<MyVertex>();
            m_Indices = new List<Int32>();
            m_MaterialIndices = new List<Int32>();
            m_Materials = new List<String>();
            m_MatTexMappingDiffuse = new Dictionary<String, String>();
            m_MatTexMappingNormalMap = new Dictionary<String, String>();
            m_BoundingBoxMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            m_BoundingBoxMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        }

        private const int VERSION = 1;

        public bool TryLoadCache(String fileName)
        {
            try
            {
                using (var fs = new FileStream(fileName + ".cache", FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        int ver = br.ReadInt32();
                        if (ver == VERSION)
                        {
                            int numVertices = br.ReadInt32();
                            int sizeOfVertex = Marshal.SizeOf(typeof(MyVertex));
                            byte[] verticesData = br.ReadBytes(sizeOfVertex * (int)numVertices);
                            for (int i = 0; i < numVertices; ++i )
                            {
                                // read vert
                                MyVertex vert = StructTools.RawDeserialize<MyVertex>(verticesData, i * sizeOfVertex);
                                m_Vertices.Add(vert);
                            }

                            uint numIndices = br.ReadUInt32();
                            for (uint i = 0; i < numIndices; ++i)
                            {
                                // read index
                                m_Indices.Add(br.ReadInt32());
                            }

                            uint numMaterialIndices = br.ReadUInt32();
                            for (uint i = 0; i < numMaterialIndices; ++i)
                            {
                                // read material index
                                m_MaterialIndices.Add(br.ReadInt32());
                            }

                            uint numMaterials = br.ReadUInt32();
                            for (uint i = 0; i < numMaterials; ++i)
                            {
                                // read material
                                m_Materials.Add(br.ReadString());
                            }

                            uint numMaterialsTexMappingDiffuse = br.ReadUInt32();
                            for (uint i = 0; i < numMaterialsTexMappingDiffuse; ++i)
                            {
                                // read material
                                string s1 = br.ReadString();
                                string s2 = br.ReadString();
                                m_MatTexMappingDiffuse.Add(s1, s2);
                            }

                            uint numMaterialsTexMappingNormal = br.ReadUInt32();
                            for (uint i = 0; i < numMaterialsTexMappingNormal; ++i)
                            {
                                // read material
                                string s1 = br.ReadString();
                                string s2 = br.ReadString();
                                m_MatTexMappingNormalMap.Add(s1, s2);
                            }

                            int sizeOfVector3 = Marshal.SizeOf(typeof(Vector3));
                            byte[] bbData = br.ReadBytes(sizeOfVector3 * 2);
                            m_BoundingBoxMin = StructTools.RawDeserialize<Vector3>(bbData, 0);
                            m_BoundingBoxMax = StructTools.RawDeserialize<Vector3>(bbData, sizeOfVector3);
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public void TrySaveCache(String fileName)
        {
            try
            {
                using (var fs = new FileStream(fileName + ".cache", FileMode.Create, FileAccess.Write))
                {
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    {
                        bw.Write(VERSION);
                        bw.Write(m_Vertices.Count);
                        foreach (var v in m_Vertices)
                        {
                            byte[] rawVertex = StructTools.RawSerialize(v);
                            bw.Write(rawVertex);
                        }
                        bw.Write(m_Indices.Count);
                        foreach (var i in m_Indices)
                        {
                            bw.Write(i);
                        }
                        bw.Write(m_MaterialIndices.Count);
                        foreach (var i in m_MaterialIndices)
                        {
                            bw.Write(i);
                        }
                        bw.Write(m_Materials.Count);
                        foreach (var i in m_Materials)
                        {
                            bw.Write(i);
                        }
                        bw.Write(m_MatTexMappingDiffuse.Count);
                        foreach (var i in m_MatTexMappingDiffuse)
                        {
                            bw.Write(i.Key);
                            bw.Write(i.Value);
                        }
                        bw.Write(m_MatTexMappingNormalMap.Count);
                        foreach (var i in m_MatTexMappingNormalMap)
                        {
                            bw.Write(i.Key);
                            bw.Write(i.Value);
                        }
                        byte[] rawBBMin = StructTools.RawSerialize(m_BoundingBoxMin);
                        byte[] rawBBMax = StructTools.RawSerialize(m_BoundingBoxMax);
                        bw.Write(rawBBMin);
                        bw.Write(rawBBMax);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public void LoadMaterials(String fileName)
        {
            CultureInfo ci = new CultureInfo("en-US", false);

            if (m_MatTexMappingNormalMap.Count > 0)
            {
                // already loaded through cache
                return;
            }

            try
            {
                using (StreamReader sr = new StreamReader(fileName))
                {
                    bool end = false;

                    String currentMaterial = "";

                    while (!end)
                    {
                        if (sr.EndOfStream)
                        {
                            end = true;
                            continue;
                        }

                        String line = sr.ReadLine().Trim();
                        if (line.StartsWith("#"))
                        {
                            continue;
                        }

                        if (line.Length < 1)
                        {
                            continue;
                        }

                        if (line.StartsWith("newmtl "))
                        {
                            var tokens = line.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                            currentMaterial = tokens[1];
                        }

                        if (line.StartsWith("map_Kd "))
                        {
                            var tokens = line.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                            String texture = tokens[1];
                            m_MatTexMappingDiffuse.Add(currentMaterial, texture);
                        }

                        if (line.StartsWith("map_bump "))
                        {
                            var tokens = line.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                            String texture = tokens[1];
                            m_MatTexMappingNormalMap.Add(currentMaterial, texture);
                        }

                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
        }

        public void Load(String fileName)
        {
            List<MyVertex> objVertices = new List<MyVertex>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> texCoords = new List<Vector2>();

            CultureInfo ci = new CultureInfo("en-US", false);

            if (TryLoadCache(fileName))
            {
                return;
            }

            try
            {
                using (StreamReader sr = new StreamReader(fileName))
                {
                    bool end = false;

                    while (!end)
                    {
                        if (sr.EndOfStream)
                        {
                            end = true;
                            continue;
                        }

                        String line = sr.ReadLine().Trim();
                        if (line.StartsWith("#"))
                        {
                            continue;
                        }

                        if (line.Length < 1)
                        {
                            continue;
                        }

                        if (line.StartsWith("usemtl "))
                        {
                            var tokens = line.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                            if (m_Materials.Count == 0 || m_Materials[m_Materials.Count - 1] != tokens[1])
                            {
                                if (m_Indices.Count > 0)
                                {
                                    m_MaterialIndices.Add(m_Indices.Count);
                                }

                                m_Materials.Add(tokens[1]);
                            }
                        }

                        if (line.StartsWith("v "))
                        {
                            var tokens = line.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                            MyVertex v = new MyVertex();
                            v.x = Convert.ToSingle(tokens[1], ci) / 100.0f;
                            v.y = Convert.ToSingle(tokens[2], ci) / 100.0f;
                            v.z = Convert.ToSingle(tokens[3], ci) / 100.0f;
                            objVertices.Add(v);

                            m_BoundingBoxMax = Vector3.Maximize(m_BoundingBoxMax, new Vector3(v.x, v.y, v.z));
                            m_BoundingBoxMin = Vector3.Minimize(m_BoundingBoxMin, new Vector3(v.x, v.y, v.z));
                        }

                        if (line.StartsWith("vn "))
                        {
                            var tokens = line.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                            Vector3 v = new Vector3();
                            v.X = Convert.ToSingle(tokens[1], ci);
                            v.Y = Convert.ToSingle(tokens[2], ci);
                            v.Z = Convert.ToSingle(tokens[3], ci);
                            normals.Add(v);
                        }

                        if (line.StartsWith("vt "))
                        {
                            var tokens = line.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                            Vector2 v = new Vector2();
                            v.X = Convert.ToSingle(tokens[1], ci);
                            v.Y = Convert.ToSingle(tokens[2], ci);
                            texCoords.Add(v);
                        }

                        if (line.StartsWith("f "))
                        {
                            var tokens = line.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                            if (tokens.Length > 4)
                            {
                                Int32[] tmpArray = new Int32[tokens.Length - 1];
                                for (int i = 0; i < tokens.Length - 1; ++i)
                                {
                                    var splitTokens = tokens[i + 1].Split(new String[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                                    tmpArray[i] = Convert.ToInt32(splitTokens[0]);
                                    Int32 normalIndex = Convert.ToInt32(splitTokens[2]);
                                    Int32 uvIndex = Convert.ToInt32(splitTokens[1]);

                                    MyVertex objVertex = objVertices[tmpArray[i] - 1];

                                    MyVertex v = new MyVertex();
                                    v = objVertex;

                                    v.nx += normals[normalIndex - 1].X;
                                    v.ny += normals[normalIndex - 1].Y;
                                    v.nz += normals[normalIndex - 1].Z;

                                    v.u = texCoords[uvIndex - 1].X;
                                    v.v = 1.0f - texCoords[uvIndex - 1].Y;

                                    m_Vertices.Add(v);
                                    tmpArray[i] = m_Vertices.Count;
                                }

                                m_Indices.Add(tmpArray[0] - 1);
                                m_Indices.Add(tmpArray[1] - 1);
                                m_Indices.Add(tmpArray[2] - 1);
                                m_Indices.Add(tmpArray[2] - 1);
                                m_Indices.Add(tmpArray[3] - 1);
                                m_Indices.Add(tmpArray[0] - 1);
                            }
                            else
                            {
                                for (int i = 0; i < 3; ++i)
                                {
                                    var splitTokens = tokens[i + 1].Split(new String[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                                    Int32 index = Convert.ToInt32(splitTokens[0]) - 1;
                                    Int32 normalIndex = Convert.ToInt32(splitTokens[2]);
                                    Int32 uvIndex = Convert.ToInt32(splitTokens[1]);

                                    MyVertex objVertex = objVertices[index];

                                    MyVertex v = new MyVertex();
                                    v = objVertex;

                                    v.nx += normals[normalIndex - 1].X;
                                    v.ny += normals[normalIndex - 1].Y;
                                    v.nz += normals[normalIndex - 1].Z;
                                    v.u = texCoords[uvIndex - 1].X;
                                    v.v = 1.0f - texCoords[uvIndex - 1].Y;

                                    m_Vertices.Add(v);
                                    m_Indices.Add(m_Vertices.Count - 1);
                                }
                            }
                        }
                    }
                    if (m_Indices.Count > 0)
                    {
                        m_MaterialIndices.Add(m_Indices.Count);
                    }

                }

                for (int i = 0; i < m_Vertices.Count; ++i)
                {
                    MyVertex vertexCopy = m_Vertices[i];
                    Vector3 normal = new Vector3(vertexCopy.nx, vertexCopy.ny, vertexCopy.nz);
                    normal.Normalize();
                    vertexCopy.nx = normal[0];
                    vertexCopy.ny = normal[1];
                    vertexCopy.nz = normal[2];

                    m_Vertices[i] = vertexCopy;
                }

                TrySaveCache(fileName);
            }
            catch (IOException e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
        }
    }
}

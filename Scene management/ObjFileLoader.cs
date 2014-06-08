using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;
using SlimDX;

namespace CSharpRenderer
{
    [Serializable]
    struct MyVertex
    {
        public float x;
        public float y;
        public float z;

        public float nx;
        public float ny;
        public float nz;

        public float u;
        public float v;
    }

    [Serializable]
    class ObjFileLoader
    {
        public List<MyVertex> m_Vertices;
        public List<Int32> m_Indices;
        public List<Vector3> m_Normals;
        public List<Vector2> m_TexCoords;
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
            m_Normals = new List<Vector3>();
            m_TexCoords = new List<Vector2>();
            m_Materials = new List<String>();
            m_MatTexMappingDiffuse = new Dictionary<String, String>();
            m_MatTexMappingNormalMap = new Dictionary<String, String>();
            m_BoundingBoxMin = new Vector3(float.MaxValue,float.MaxValue,float.MaxValue);
            m_BoundingBoxMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        }

        public void LoadMaterials(String fileName)
        {
            CultureInfo ci = new CultureInfo("en-US", false);

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
            CultureInfo ci = new CultureInfo("en-US", false);

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

                        if (line.Length < 1 )
                        {
                            continue;
                        }

                        if (line.StartsWith("usemtl "))
                        {
                            var tokens = line.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                            if ( m_Materials.Count == 0 || m_Materials[m_Materials.Count-1] != tokens[1] )
                            {
                                if (m_Indices.Count > 0)
                                {
                                    m_MaterialIndices.Add(m_Indices.Count);
                                }

                                m_Materials.Add(tokens[1]);
                            }
                        }

                        if ( line.StartsWith("v ") )
                        {
                            var tokens = line.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries );
                            MyVertex v = new MyVertex();
                            v.x = Convert.ToSingle(tokens[1], ci) / 100.0f;
                            v.y = Convert.ToSingle(tokens[2], ci) / 100.0f;
                            v.z = Convert.ToSingle(tokens[3], ci) / 100.0f;
                            m_Vertices.Add(v);

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
                            m_Normals.Add(v);
                        }

                        if (line.StartsWith("vt "))
                        {
                            var tokens = line.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                            Vector2 v = new Vector2();
                            v.X = Convert.ToSingle(tokens[1], ci);
                            v.Y = Convert.ToSingle(tokens[2], ci);
                            m_TexCoords.Add(v);
                        }

                        if (line.StartsWith("f "))
                        {
                            var tokens = line.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                            
                            if ( tokens.Length > 4 )
                            {
                                Int32[] tmpArray = new Int32[tokens.Length-1];
                                for (int i = 0; i < tokens.Length - 1; ++i)
                                {
                                    var splitTokens = tokens[i + 1].Split(new String[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                                    tmpArray[i] = Convert.ToInt32(splitTokens[0]);
                                    Int32 normalIndex = Convert.ToInt32(splitTokens[2]);
                                    Int32 uvIndex = Convert.ToInt32(splitTokens[1]);

                                    MyVertex v = m_Vertices[tmpArray[i] - 1];
                                    v.nx += m_Normals[normalIndex - 1].X;
                                    v.ny += m_Normals[normalIndex - 1].Y;
                                    v.nz += m_Normals[normalIndex - 1].Z;

                                    v.u = m_TexCoords[uvIndex - 1].X;
                                    v.v = m_TexCoords[uvIndex - 1].Y;

                                    m_Vertices[tmpArray[i] - 1] = v;
                                }

                                m_Indices.Add(tmpArray[0]-1);
                                m_Indices.Add(tmpArray[1]-1);
                                m_Indices.Add(tmpArray[2]-1);
                                m_Indices.Add(tmpArray[2]-1);
                                m_Indices.Add(tmpArray[3]-1);
                                m_Indices.Add(tmpArray[0]-1);
                            }
                            else
                            {
                                for (int i = 0; i < 3; ++i)
                                {
                                    var splitTokens = tokens[i + 1].Split(new String[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                                    Int32 index = Convert.ToInt32(splitTokens[0]) - 1;

                                    m_Indices.Add(index);

                                    Int32 normalIndex = Convert.ToInt32(splitTokens[2]);
                                    Int32 uvIndex = Convert.ToInt32(splitTokens[1]);

                                    MyVertex v = m_Vertices[index];
                                    v.nx += m_Normals[normalIndex - 1].X;
                                    v.ny += m_Normals[normalIndex - 1].Y;
                                    v.nz += m_Normals[normalIndex - 1].Z;
                                    v.u = m_TexCoords[uvIndex - 1].X;
                                    v.v = m_TexCoords[uvIndex - 1].Y;
                                    m_Vertices[index] = v;
                                }
                            }
                        }
                    }
                    if (m_Indices.Count > 0)
                    {
                        m_MaterialIndices.Add(m_Indices.Count);
                    }

                }

                for(int i = 0; i < m_Vertices.Count; ++i )
                {
                    MyVertex vertexCopy = m_Vertices[i];
                    Vector3 normal = new Vector3(vertexCopy.nx, vertexCopy.ny, vertexCopy.nz);
                    normal.Normalize();
                    vertexCopy.nx = normal[0];
                    vertexCopy.ny = normal[1];
                    vertexCopy.nz = normal[2];

                    m_Vertices[i] = vertexCopy;
                }
                
            
            }
            catch (IOException e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
        }
    }
}

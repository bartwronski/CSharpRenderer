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


namespace CSharpRenderer
{
    class SimpleSceneWrapper
    {
        struct TexWrapper
        {
            public String name;
            public String textureNameDiffuse;
            public String textureNameBump;
            public Texture2D textureObjectDiffuse;
            public Texture2D textureObjectBump;
            public ShaderResourceView textureSrvDiffuse;
            public ShaderResourceView textureSrvBump;
        };

        ObjFileLoader m_ObjectLoader;

        TexWrapper[] m_MaterialsResources;

        InputLayout m_SceneInputLayout;
        Buffer m_VertexBuffer;
        Buffer m_IndexBuffer;


        public SimpleSceneWrapper()
        {
            m_ObjectLoader = new ObjFileLoader();
        }

        public void Initialize(Device device, string sceneName)
        {
            m_ObjectLoader.Load(sceneName + ".obj");
            m_ObjectLoader.LoadMaterials(sceneName + ".mtl");

            m_MaterialsResources = new TexWrapper[m_ObjectLoader.m_MatTexMappingDiffuse.Count];
            int counter = 0;
            foreach (var v in m_ObjectLoader.m_MatTexMappingDiffuse)
            {
                TexWrapper tex = new TexWrapper();
                tex.name = v.Key;
                tex.textureNameDiffuse = v.Value;

                // try to find bump texture or revert to default
                string bumpTexName = "textures\\default_n.dds";
                if (m_ObjectLoader.m_MatTexMappingNormalMap.ContainsKey(v.Key))
                {
                    bumpTexName = m_ObjectLoader.m_MatTexMappingNormalMap[v.Key];
                }
                tex.textureNameBump = bumpTexName;

                try
                {
                    tex.textureObjectDiffuse = Texture2D.FromFile(device, v.Value);
                    tex.textureSrvDiffuse = new ShaderResourceView(device, tex.textureObjectDiffuse);

                    tex.textureObjectBump = Texture2D.FromFile(device, tex.textureNameBump);
                    tex.textureSrvBump = new ShaderResourceView(device, tex.textureObjectBump);

                }
                catch
                {
                    tex.textureObjectDiffuse = null;
                    tex.textureSrvDiffuse = null;
                    tex.textureObjectBump = null;
                    tex.textureSrvBump = null;
                }
                m_MaterialsResources[counter++] = tex;
            }

            int numVertices = m_ObjectLoader.m_Vertices.Count;
            int numIndices = m_ObjectLoader.m_Indices.Count;

            // create test vertex data, making sure to rewind the stream afterward
            var vertices = new DataStream(8 * sizeof(System.Single) * numVertices, true, true);
            foreach (var vertex in m_ObjectLoader.m_Vertices)
            {
                vertices.Write(new Vector3(vertex.x, vertex.y, vertex.z));
                Vector3 normal = new Vector3(vertex.nx, vertex.ny, vertex.nz);
                vertices.Write(normal);
                Vector2 uv = new Vector2(vertex.u, vertex.v);
                vertices.Write(uv);
            }
            vertices.Position = 0;

            var indices = new DataStream(sizeof(System.Int32) * numIndices, true, true);
            foreach (var index in m_ObjectLoader.m_Indices)
            {
                indices.Write(index);
            }
            indices.Position = 0;

            // create the vertex layout and buffer
            var elements = new[] { new InputElement("POSITION", 0, Format.R32G32B32_Float, 0), new InputElement("NORMAL", 0, Format.R32G32B32_Float, 0), new InputElement("TEXCOORD", 0, Format.R32G32_Float, 0) };
            m_SceneInputLayout = new InputLayout(device, ShaderManager.GetVertexShaderSignature("VertexScene"), elements);
            m_VertexBuffer = new Buffer(device, vertices, 8 * sizeof(System.Single) * numVertices, ResourceUsage.Default, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            m_IndexBuffer = new Buffer(device, indices, 4 * numIndices, ResourceUsage.Default, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
        }

        public void RenderNoMaterials(DeviceContext context)
        {
            // configure the Input Assembler portion of the pipeline with the vertex data
            context.InputAssembler.InputLayout = m_SceneInputLayout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(m_VertexBuffer, 8 * sizeof(System.Single), 0));
            context.InputAssembler.SetIndexBuffer(m_IndexBuffer, Format.R32_UInt, 0);

            context.DrawIndexed(m_ObjectLoader.m_MaterialIndices[m_ObjectLoader.m_MaterialIndices.Count-1], 0, 0);
        }

        public void GetSceneBounds(out Vector3 min, out Vector3 max)
        {
            min = m_ObjectLoader.m_BoundingBoxMin;
            max = m_ObjectLoader.m_BoundingBoxMax;
        }

        public void Render(DeviceContext context)
        {
            // configure the Input Assembler portion of the pipeline with the vertex data
            context.InputAssembler.InputLayout = m_SceneInputLayout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(m_VertexBuffer, 8 * sizeof(System.Single), 0));
            context.InputAssembler.SetIndexBuffer(m_IndexBuffer, Format.R32_UInt, 0);

            for (int i = 0; i < m_ObjectLoader.m_MaterialIndices.Count; ++i)
            {
                int prevCount = i == 0 ? 0 : m_ObjectLoader.m_MaterialIndices[i - 1];

                String matName = m_ObjectLoader.m_Materials[i];
                foreach (var t in m_MaterialsResources)
                {
                    if (t.name == matName && t.textureSrvDiffuse != null)
                    {
                        context.PixelShader.SetShaderResource(t.textureSrvDiffuse, 4);
                        context.PixelShader.SetShaderResource(t.textureSrvBump, 9);
                        break;
                    }
                }

                context.DrawIndexed(m_ObjectLoader.m_MaterialIndices[i] - prevCount, prevCount, 0);
            }
        }

    }
}

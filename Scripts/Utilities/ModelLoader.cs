using Silk.NET.OpenGL;
using Assimp;
using System;
using System.Collections.Generic;
using System.IO;

namespace SpaceSim
{
    public class ModelLoader
    {
        private readonly GL _gl;
        private readonly List<uint> _vaos = new List<uint>();
        private readonly List<uint> _vbos = new List<uint>();
        private readonly List<uint> _ebos = new List<uint>();
        private readonly List<int> _indexCounts = new List<int>();

        public ModelLoader(GL gl, string modelPath)
        {
            _gl = gl;
            LoadModel(modelPath);
        }

        private unsafe void LoadModel(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Model file not found: {path}");

            AssimpContext importer = new AssimpContext();
            Scene scene = importer.ImportFile(path,
                PostProcessSteps.Triangulate |
                PostProcessSteps.FlipUVs |
                PostProcessSteps.CalculateTangentSpace);

            if (scene == null || scene.MeshCount == 0)
                throw new Exception($"Failed to load model or no meshes found: {path}");

            foreach (var mesh in scene.Meshes)
            {
                List<float> vertexData = new();
                List<uint> indices = new();

                for (int i = 0; i < mesh.Vertices.Count; i++)
                {
                    // Position
                    vertexData.Add(mesh.Vertices[i].X);
                    vertexData.Add(mesh.Vertices[i].Y);
                    vertexData.Add(mesh.Vertices[i].Z);

                    // Normal
                    vertexData.Add(mesh.Normals[i].X);
                    vertexData.Add(mesh.Normals[i].Y);
                    vertexData.Add(mesh.Normals[i].Z);

                    // Texture coordinates
                    if (mesh.HasTextureCoords(0))
                    {
                        vertexData.Add(mesh.TextureCoordinateChannels[0][i].X);
                        vertexData.Add(mesh.TextureCoordinateChannels[0][i].Y);
                    }
                    else
                    {
                        vertexData.Add(0);
                        vertexData.Add(0);
                    }
                }

                foreach (var face in mesh.Faces)
                {
                    foreach (var index in face.Indices)
                        indices.Add((uint)index);
                }

                _indexCounts.Add(indices.Count);

                uint vao = _gl.GenVertexArray();
                uint vbo = _gl.GenBuffer();
                uint ebo = _gl.GenBuffer();

                _vaos.Add(vao);
                _vbos.Add(vbo);
                _ebos.Add(ebo);

                _gl.BindVertexArray(vao);

                float[] vertexArray = vertexData.ToArray();
                _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
                fixed (float* v = vertexArray)
                {
                    _gl.BufferData(BufferTargetARB.ArrayBuffer,
                        (nuint)(vertexArray.Length * sizeof(float)),
                        v, BufferUsageARB.StaticDraw);
                }

                uint[] indexArray = indices.ToArray();
                _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
                fixed (uint* i = indexArray)
                {
                    _gl.BufferData(BufferTargetARB.ElementArrayBuffer,
                        (nuint)(indexArray.Length * sizeof(uint)),
                        i, BufferUsageARB.StaticDraw);
                }

                int stride = 8 * sizeof(float); // Each vertex has 8 floats: 3 position, 3 normal, 2 texture coordinates

                // position
                _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)stride, (void*)0);
                _gl.EnableVertexAttribArray(0);

                // normal
                _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, (uint)stride, (void*)(3 * sizeof(float)));
                _gl.EnableVertexAttribArray(1);

                // texture
                _gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, (uint)stride, (void*)(6 * sizeof(float)));
                _gl.EnableVertexAttribArray(2);

                _gl.BindVertexArray(0);
            }
        }

        public unsafe void Render()
        {
            for (int i = 0; i < _vaos.Count; i++)
            {
                _gl.BindVertexArray(_vaos[i]);
                _gl.DrawElements(Silk.NET.OpenGL.PrimitiveType.Triangles, (uint)_indexCounts[i], DrawElementsType.UnsignedInt, null);
            }
        }
    }
}
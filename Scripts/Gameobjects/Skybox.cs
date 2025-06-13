using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Silk.NET.Maths;

namespace SpaceSim
{
    public class Skybox
    {
        private readonly GL _gl;
        private readonly uint _vao;
        private readonly uint _vbo;
        private readonly uint _textureId;
        private readonly Shader _shader;

        public Skybox(GL gl, Shader shader, string[] texturePaths)
        {
            _gl = gl;
            _shader = shader;

            float[] skyboxVertices = {
                -1.0f,  1.0f, -1.0f,
                -1.0f, -1.0f, -1.0f,
                 1.0f, -1.0f, -1.0f,
                 1.0f, -1.0f, -1.0f,
                 1.0f,  1.0f, -1.0f,
                -1.0f,  1.0f, -1.0f,

                -1.0f, -1.0f,  1.0f,
                -1.0f, -1.0f, -1.0f,
                -1.0f,  1.0f, -1.0f,
                -1.0f,  1.0f, -1.0f,
                -1.0f,  1.0f,  1.0f,
                -1.0f, -1.0f,  1.0f,

                 1.0f, -1.0f, -1.0f,
                 1.0f, -1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f, -1.0f,
                 1.0f, -1.0f, -1.0f,

                -1.0f, -1.0f,  1.0f,
                -1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f, -1.0f,  1.0f,
                -1.0f, -1.0f,  1.0f,

                -1.0f,  1.0f, -1.0f,
                 1.0f,  1.0f, -1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                -1.0f,  1.0f,  1.0f,
                -1.0f,  1.0f, -1.0f,

                -1.0f, -1.0f, -1.0f,
                -1.0f, -1.0f,  1.0f,
                 1.0f, -1.0f, -1.0f,
                 1.0f, -1.0f, -1.0f,
                -1.0f, -1.0f,  1.0f,
                 1.0f, -1.0f,  1.0f
            };

            _vao = _gl.GenVertexArray();
            _vbo = _gl.GenBuffer();

            _gl.BindVertexArray(_vao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

            unsafe
            {
                fixed (float* v = skyboxVertices)
                {
                    _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(skyboxVertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
                }
            }

            _gl.EnableVertexAttribArray(0);
            unsafe
            {
                _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
            }

            _textureId = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.TextureCubeMap, _textureId);

            for (int i = 0; i < texturePaths.Length; i++)
            {
                using var image = Image.Load<Rgba32>(texturePaths[i]);

                var pixelData = new Rgba32[image.Width * image.Height];
                image.CopyPixelDataTo(pixelData);

                unsafe
                {
                    fixed (void* dataPtr = pixelData)
                    {
                        _gl.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0,
                            InternalFormat.Rgba, (uint)image.Width, (uint)image.Height,
                            0, PixelFormat.Rgba, PixelType.UnsignedByte, dataPtr);
                    }
                }
            }

            _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            _gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)GLEnum.ClampToEdge);
        }

        public unsafe void Draw(Matrix4X4<float> view, Matrix4X4<float> projection)
        {
            _gl.DepthFunc(DepthFunction.Lequal);
            _shader.Use();

            // Remove translation from view matrix
            var viewWithoutTranslation = new Matrix4X4<float>(
                view.M11, view.M12, view.M13, 0,
                view.M21, view.M22, view.M23, 0,
                view.M31, view.M32, view.M33, 0,
                0, 0, 0, 1
            );

            _shader.SetMatrix4("view", viewWithoutTranslation);
            _shader.SetMatrix4("projection", projection);

            _gl.BindVertexArray(_vao);
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.TextureCubeMap, _textureId);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
            _gl.BindVertexArray(0);
            _gl.DepthFunc(DepthFunction.Less);
        }
    }
}
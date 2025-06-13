using Silk.NET.OpenGL;
using System;
using System.IO;
using Silk.NET.Maths;

namespace SpaceSim
{
    public class Shader
    {
        private readonly GL _gl;
        private readonly uint _handle;

        public Shader(GL gl, string vertexPath, string fragmentPath)
        {
            _gl = gl;

            uint vertexShader = LoadShader(ShaderType.VertexShader, vertexPath);
            uint fragmentShader = LoadShader(ShaderType.FragmentShader, fragmentPath);

            _handle = _gl.CreateProgram();
            _gl.AttachShader(_handle, vertexShader);
            _gl.AttachShader(_handle, fragmentShader);
            _gl.LinkProgram(_handle);

            CheckShaderLinking();

            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);
        }

        public Shader(GL gl, uint program)
        {
            _gl = gl;
            _handle = program;
        }

        private uint LoadShader(ShaderType type, string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"Shader file not found: {path}");
                return CreateDefaultShader(type);
            }

            string shaderSource = File.ReadAllText(path);
            uint shader = _gl.CreateShader(type);
            _gl.ShaderSource(shader, shaderSource);
            _gl.CompileShader(shader);

            string infoLog = _gl.GetShaderInfoLog(shader);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                Console.WriteLine($"Shader compilation warning/error ({type}): {infoLog}");
            }

            return shader;
        }

        private uint CreateDefaultShader(ShaderType type)
        {
            string shaderSource = "";

            if (type == ShaderType.VertexShader)
            {
                shaderSource = @"#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoord;
uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
out vec3 Normal;
out vec2 TexCoord;
void main()
{
    Normal = aNormal;
    TexCoord = aTexCoord;
    gl_Position = projection * view * model * vec4(aPosition, 1.0);
}";
            }
            else if (type == ShaderType.FragmentShader)
            {
                shaderSource = @"#version 330 core
out vec4 FragColor;
uniform vec3 objectColor;
void main()
{
    FragColor = vec4(objectColor, 1.0);
}";
            }

            uint shader = _gl.CreateShader(type);
            _gl.ShaderSource(shader, shaderSource);
            _gl.CompileShader(shader);

            _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
            if (status == 0)
            {
                string log = _gl.GetShaderInfoLog(shader);
                Console.WriteLine($"Default shader compilation error ({type}): {log}");
            }
            else
            {
                Console.WriteLine($"Default shader compiled successfully ({type})");
            }

            return shader;
        }

        private void CheckShaderLinking()
        {
            _gl.GetProgram(_handle, GLEnum.LinkStatus, out int status);
            if (status == 0)
            {
                string errorLog = _gl.GetProgramInfoLog(_handle);
                Console.WriteLine($"Shader program linking error: {errorLog}");
            }
        }

        public void Use()
        {
            _gl.UseProgram(_handle);
        }

        public unsafe void SetMatrix4(string name, Matrix4X4<float> matrix)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location != -1)
            {
                _gl.UniformMatrix4(location, 1, false, (float*)&matrix);
            }
        }

        public void SetInt(string name, int value)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location != -1)
            {
                _gl.Uniform1(location, value);
            }
        }

        public void SetBool(string name, bool value)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location != -1)
            {
                _gl.Uniform1(location, value ? 1 : 0);
            }
        }

        public void SetFloat(string name, float value)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location != -1)
            {
                _gl.Uniform1(location, value);
            }
        }

        public void SetVector3(string name, Vector3D<float> value)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location != -1)
            {
                _gl.Uniform3(location, value.X, value.Y, value.Z);
            }
        }

        public void Dispose()
        {
            _gl.DeleteProgram(_handle);
        }
    }
}
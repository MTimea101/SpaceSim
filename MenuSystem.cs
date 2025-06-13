using Silk.NET.OpenGL;
using Silk.NET.Maths;
using Silk.NET.Input;
using System;

namespace SpaceSim
{
    public enum GameState
    {
        MainMenu,
        Playing,
        GameOver
    }

    public class MenuSystem
    {
        private readonly GL _gl;
        private Shader _uiShader;
        private uint _quadVAO;
        private uint _quadVBO;
        private IKeyboard? _keyboard;
        private bool _enterPressed = false;
        private bool _spacePressed = false;

        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public bool StartGameRequested { get; private set; } = false;

        public MenuSystem(GL gl)
        {
            _gl = gl;
            _uiShader = CreateUIShader();
            CreateQuad();
        }

        public void AttachInput(IInputContext input)
        {
            if (input.Keyboards.Count > 0)
                _keyboard = input.Keyboards[0];
        }

        public void Update(float deltaTime)
        {
            if (_keyboard != null)
                HandleInput();
        }

        private void HandleInput()
        {
            if (_keyboard == null) return;

            bool enterPressed = _keyboard.IsKeyPressed(Key.Enter);
            bool spacePressed = _keyboard.IsKeyPressed(Key.Space);

            if ((enterPressed && !_enterPressed) || (spacePressed && !_spacePressed))
            {
                if (CurrentState == GameState.MainMenu || CurrentState == GameState.GameOver)
                {
                    StartGameRequested = true;
                    CurrentState = GameState.Playing;
                }
            }

            _enterPressed = enterPressed;
            _spacePressed = spacePressed;
        }

        public void SetGameState(GameState state)
        {
            CurrentState = state;
            StartGameRequested = false;
        }

        public void DrawMainMenu(int width, int height)
        {
            SetupUI(width, height);

            // Background
            DrawColoredRect(0, 0, width, height, new Vector3D<float>(0.05f, 0.05f, 0.15f), 0.95f);

            // Title panel
            DrawColoredRect(width / 2 - 300, height - 150, 600, 100, new Vector3D<float>(0.0f, 0.3f, 0.6f), 0.8f);

            // Start button
            DrawColoredRect(width / 2 - 150, 150, 300, 80, new Vector3D<float>(0.0f, 0.8f, 0.2f), 0.9f);

            RestoreGL();
        }

        public void DrawInGameUI(int lives, bool firstPersonView, float gameTime, int asteroidsAvoided, int width, int height)
        {
            SetupUI(width, height);

            // Lives display
            DrawColoredRect(20, height - 100, 200, 80, new Vector3D<float>(0.1f, 0.1f, 0.1f), 0.7f);

            for (int i = 0; i < 3; i++)
            {
                var color = i < lives ? new Vector3D<float>(1.0f, 0.2f, 0.2f) : new Vector3D<float>(0.3f, 0.1f, 0.1f);
                DrawColoredRect(30 + i * 50, height - 80, 40, 40, color, 0.9f);
            }

            // Warning border when low health
            if (lives == 1)
            {
                DrawColoredRect(0, 0, width, 20, new Vector3D<float>(1.0f, 0.0f, 0.0f), 0.3f);
                DrawColoredRect(0, height - 20, width, 20, new Vector3D<float>(1.0f, 0.0f, 0.0f), 0.3f);
            }

            RestoreGL();
        }

        public void DrawGameOverScreen(float gameTime, int asteroidsAvoided, int width, int height)
        {
            SetupUI(width, height);

            // Dark overlay
            DrawColoredRect(0, 0, width, height, new Vector3D<float>(0.0f, 0.0f, 0.0f), 0.8f);

            // Game over panel
            DrawColoredRect(width / 2 - 250, height / 2 - 150, 500, 300, new Vector3D<float>(0.6f, 0.0f, 0.0f), 0.9f);

            // Restart button
            DrawColoredRect(width / 2 - 100, height / 2 - 200, 200, 60, new Vector3D<float>(0.0f, 0.6f, 0.0f), 0.9f);

            RestoreGL();
        }

        private void SetupUI(int width, int height)
        {
            _gl.Disable(GLEnum.DepthTest);
            _gl.Enable(GLEnum.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            var projection = Matrix4X4.CreateOrthographic(width, height, -1.0f, 1.0f);
            _uiShader.Use();
            _uiShader.SetMatrix4("projection", projection);
        }

        private void RestoreGL()
        {
            _gl.Disable(GLEnum.Blend);
            _gl.Enable(GLEnum.DepthTest);
        }

        private void DrawColoredRect(float x, float y, float width, float height, Vector3D<float> color, float alpha)
        {
            _uiShader.SetVector3("color", color);
            _uiShader.SetFloat("alpha", alpha);

            var model = Matrix4X4.CreateScale(width, height, 1.0f) * Matrix4X4.CreateTranslation(x, y, 0.0f);
            _uiShader.SetMatrix4("model", model);

            _gl.BindVertexArray(_quadVAO);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }

        private Shader CreateUIShader()
        {
            string vertexSource = @"
#version 330 core
layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aTexCoord;
out vec2 TexCoord;
uniform mat4 projection;
uniform mat4 model;
void main()
{
    gl_Position = projection * model * vec4(aPosition, 0.0, 1.0);
    TexCoord = aTexCoord;
}";

            string fragmentSource = @"
#version 330 core
in vec2 TexCoord;
out vec4 FragColor;
uniform vec3 color;
uniform float alpha;
void main()
{
    FragColor = vec4(color, alpha);
}";

            uint vs = _gl.CreateShader(ShaderType.VertexShader);
            _gl.ShaderSource(vs, vertexSource);
            _gl.CompileShader(vs);

            uint fs = _gl.CreateShader(ShaderType.FragmentShader);
            _gl.ShaderSource(fs, fragmentSource);
            _gl.CompileShader(fs);

            uint program = _gl.CreateProgram();
            _gl.AttachShader(program, vs);
            _gl.AttachShader(program, fs);
            _gl.LinkProgram(program);

            _gl.DeleteShader(vs);
            _gl.DeleteShader(fs);

            return new Shader(_gl, program);
        }

        private unsafe void CreateQuad()
        {
            float[] vertices = {
                0.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 1.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 1.0f, 0.0f
            };

            _quadVAO = _gl.GenVertexArray();
            _quadVBO = _gl.GenBuffer();

            _gl.BindVertexArray(_quadVAO);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _quadVBO);

            fixed (float* v = vertices)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
            }

            _gl.EnableVertexAttribArray(0);
            _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);
            _gl.EnableVertexAttribArray(1);
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
        }
    }
}
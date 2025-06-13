using Silk.NET.OpenGL;
using Silk.NET.Maths;
using Silk.NET.Input;
using System;

namespace SpaceSim
{
    public class Spaceship
    {
        private readonly ModelLoader _model;
        private readonly GL _gl;
        private readonly Shader _shader;
        private IKeyboard? _keyboard;

        public Vector3D<float> Position { get; private set; } = new(0, 0, 0);
        public Vector3D<float> Direction { get; private set; } = new(0, 0, -1);
        public Vector3D<float> Velocity { get; private set; } = Vector3D<float>.Zero;

        private float _yaw = 0f;
        private float _pitch = 0f;
        private float _roll = 0f;

        private readonly float _lateralSpeed = 60f;
        private readonly float _worldBounds = 300f;

        public Spaceship(GL gl, Shader shader)
        {
            _gl = gl;
            _shader = shader;
            try
            {
                _model = new ModelLoader(gl, "Assets/Models/Spaceship/shuttle.obj");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not load spaceship model: {ex.Message}");
                throw new Exception("Could not load spaceship model and no fallback implemented");
            }
            UpdateDirection();
        }

        public void AttachInput(IInputContext input)
        {
            if (input.Keyboards.Count > 0)
            {
                _keyboard = input.Keyboards[0];
            }
        }

        public void Update(float deltaTime)
        {
            if (_keyboard is null) return;

            // Arrow key movement
            if (_keyboard.IsKeyPressed(Key.Left))
            {
                Position = new Vector3D<float>(Position.X - _lateralSpeed * deltaTime, Position.Y, Position.Z);
            }
            if (_keyboard.IsKeyPressed(Key.Right))
            {
                Position = new Vector3D<float>(Position.X + _lateralSpeed * deltaTime, Position.Y, Position.Z);
            }
            if (_keyboard.IsKeyPressed(Key.Up))
            {
                Position = new Vector3D<float>(Position.X, Position.Y + _lateralSpeed * deltaTime, Position.Z);
            }
            if (_keyboard.IsKeyPressed(Key.Down))
            {
                Position = new Vector3D<float>(Position.X, Position.Y - _lateralSpeed * deltaTime, Position.Z);
            }

            // Boundary wrapping
            if (Position.X > _worldBounds)
                Position = new Vector3D<float>(-_worldBounds, Position.Y, Position.Z);
            else if (Position.X < -_worldBounds)
                Position = new Vector3D<float>(_worldBounds, Position.Y, Position.Z);

            if (Position.Y > _worldBounds)
                Position = new Vector3D<float>(Position.X, -_worldBounds, Position.Z);
            else if (Position.Y < -_worldBounds)
                Position = new Vector3D<float>(Position.X, _worldBounds, Position.Z);

            if (Position.Z > 50f)
                Position = new Vector3D<float>(Position.X, Position.Y, 50f);
            else if (Position.Z < -50f)
                Position = new Vector3D<float>(Position.X, Position.Y, -50f);

            UpdateDirection();
        }

        private void UpdateDirection()
        {
            Direction = new Vector3D<float>(
                MathF.Sin(_yaw) * MathF.Cos(_pitch),
                -MathF.Sin(_pitch),
                -MathF.Cos(_yaw) * MathF.Cos(_pitch)
            );
            Direction = Vector3D.Normalize(Direction);
        }

        public void Draw(Matrix4X4<float> view, Matrix4X4<float> projection)
        {
            _shader.Use();

            var baseRotation = Matrix4X4.CreateRotationY(-MathF.PI / 2);
            var baseRotation2 = Matrix4X4.CreateRotationZ(-MathF.PI / 2);
            var rotationMatrix = baseRotation *
                                Matrix4X4.CreateRotationY(_yaw) *
                                Matrix4X4.CreateRotationX(_pitch) *
                                baseRotation2;

            var modelMatrix = Matrix4X4.CreateScale(1.5f) *
                             rotationMatrix *
                             Matrix4X4.CreateTranslation(Position);

            _shader.SetMatrix4("model", modelMatrix);
            _shader.SetMatrix4("view", view);
            _shader.SetMatrix4("projection", projection);
            _shader.SetVector3("objectColor", new Vector3D<float>(0.7f, 0.8f, 0.9f));

            _model.Render();
        }

        public float GetCollisionRadius()
        {
            return 1.5f;
        }
    }
}
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;

namespace SpaceSim
{
    public class Asteroid
    {
        private Vector3D<float> _position;
        private Vector3D<float> _direction;
        private float _speed;
        private ModelLoader _model;
        private Texture? _texture;
        private float _scale;
        private Vector3D<float> _color;

        public float Radius { get; private set; }
        public Vector3D<float> Position => _position;

        private static readonly Vector3D<float>[] AsteroidColors = new[]
        {
            new Vector3D<float>(0.6f, 0.5f, 0.4f),
            new Vector3D<float>(0.5f, 0.4f, 0.3f),
            new Vector3D<float>(0.7f, 0.6f, 0.5f),
            new Vector3D<float>(0.4f, 0.4f, 0.4f),
            new Vector3D<float>(0.8f, 0.7f, 0.6f),
            new Vector3D<float>(0.3f, 0.3f, 0.2f)
        };

        public Asteroid(GL gl, Shader shader, string modelPath, string texturePath,
            Vector3D<float> startPosition, Vector3D<float> direction,
            float speed, float rotationSpeed)
        {
            _model = new ModelLoader(gl, modelPath);

            try
            {
                _texture = LoadTexture(gl, texturePath);
            }
            catch
            {
                Console.WriteLine($"Warning: Could not load texture {texturePath}");
                _texture = null;
            }

            _color = AsteroidColors[Random.Shared.Next(AsteroidColors.Length)];

            _position = startPosition;
            _direction = Vector3D.Normalize(direction);
            _speed = speed;

            _scale = 6.0f + Random.Shared.NextSingle() * 4.0f;
            Radius = _scale * 1.6f;
        }

        private unsafe Texture LoadTexture(GL gl, string path)
        {
            using var image = Image.Load<Rgba32>(path);
            image.Mutate(x => x.Flip(FlipMode.Vertical));

            var pixelData = new Rgba32[image.Width * image.Height];
            image.CopyPixelDataTo(pixelData);

            uint tex = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, tex);

            fixed (void* dataPtr = pixelData)
            {
                gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                    (uint)image.Width, (uint)image.Height, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte, dataPtr);
            }

            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);

            return new Texture(gl, tex);
        }

        public void Update(float deltaTime)
        {
            _position += _direction * _speed * deltaTime;

            if (_position.Z > 150f)
            {
                ResetToFarPosition();
            }
        }

        public void ResetToFarPosition()
        {
            _position = new Vector3D<float>(
                (Random.Shared.NextSingle() - 0.5f) * 800f,
                (Random.Shared.NextSingle() - 0.5f) * 600f,
                -600f - Random.Shared.NextSingle() * 400f
            );

            var targetX = Random.Shared.NextSingle() * 60f - 30f;
            var targetY = Random.Shared.NextSingle() * 60f - 30f;
            var targetZ = 200f;

            var directionVector = new Vector3D<float>(targetX - _position.X, targetY - _position.Y, targetZ - _position.Z);
            _direction = Vector3D.Normalize(directionVector);

            _speed = 80f + Random.Shared.NextSingle() * 120f;

            _scale = 3.0f + Random.Shared.NextSingle() * 2.0f;
            Radius = _scale * 1.5f;

            _color = AsteroidColors[Random.Shared.Next(AsteroidColors.Length)];
        }

        public void Draw(Matrix4X4<float> view, Matrix4X4<float> projection, Shader shader)
        {
            shader.Use();

            var modelMatrix = Matrix4X4.CreateScale(_scale) *
                             Matrix4X4.CreateTranslation(_position);

            shader.SetMatrix4("model", modelMatrix);
            shader.SetMatrix4("view", view);
            shader.SetMatrix4("projection", projection);
            shader.SetVector3("objectColor", _color);

            _model.Render();
        }
    }
}
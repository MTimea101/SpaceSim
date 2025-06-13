using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using System;
using System.Collections.Generic;

namespace SpaceSim
{
    public static class WindowManager
    {
        private static GL _gl = null!;
        private static Spaceship? _spaceship = null;
        private static Shader _shader = null!;
        private static Shader _skyboxShader = null!;
        private static Camera _camera = null!;
        private static Skybox _skybox = null!;
        private static List<Asteroid>? _asteroids = null;
        private static bool _firstPersonView = false;
        private static IInputContext _input = null!;
        private static OverlayUI _overlayUI = null!;

        private static bool _vKeyPressed = false;
        private static bool _escKeyPressed = false;

        private static Matrix4X4<float> _projection;
        private static int _playerLives = 3;
        private static bool _gameOver = false;
        private static float _gameOverTimer = 0f;
        private static float _lastCollisionTime = 0f;
        private static float _collisionCooldown = 2f;

        private static float _gameTime = 0f;
        private static int _asteroidsAvoided = 0;

        private static float _debugTimer = 0f;
        private static bool _gameInitialized = false;

        private static float _consoleOutputTimer = 0f;
        private static float _asteroidSpawnTimer = 0f;

        private static readonly Random _random = new Random();
        private static Vector3D<float> _lastSpaceshipPosition = Vector3D<float>.Zero;
        private static float _frameTime = 0f;
        private static int _frameCount = 0;

        public static IWindow CreateWindow()
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(1280, 720);
            options.Title = "SpaceSim - 3D Space Adventure";
            options.VSync = true;

            var window = Window.Create(options);

            window.Load += () =>
            {
                try
                {
                    _gl = window.CreateOpenGL();
                    _gl.Enable(GLEnum.DepthTest);
                    _gl.Enable(GLEnum.CullFace);
                    _gl.CullFace(TriangleFace.Back);
                    _gl.ClearColor(0f, 0f, 0.1f, 1f);

                    _input = window.CreateInput();

                    _shader = new Shader(_gl, "Shaders/Objects/vertex.glsl", "Shaders/Objects/fragment.glsl");
                    _skyboxShader = new Shader(_gl, "Shaders/Skybox/skybox_vertex.glsl", "Shaders/Skybox/skybox_fragment.glsl");

                    string[] skyboxTextures = {
                        "Assets/Skybox/right.jpg",
                        "Assets/Skybox/left.jpg",
                        "Assets/Skybox/top.jpg",
                        "Assets/Skybox/bottom.jpg",
                        "Assets/Skybox/front.jpg",
                        "Assets/Skybox/back.jpg"
                    };
                    _skybox = new Skybox(_gl, _skyboxShader, skyboxTextures);

                    _camera = new Camera();

                    _projection = Matrix4X4.CreatePerspectiveFieldOfView(
                        MathHelper.DegreesToRadians(60f),
                        window.Size.X / (float)window.Size.Y,
                        0.1f,
                        1000f
                    );

                    _overlayUI = new OverlayUI(_gl);
                    _overlayUI.AttachInput(_input);
                    _overlayUI.SetGameState(GameState.MainMenu);

                    _gameInitialized = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Initialization error: {ex.Message}");
                    Environment.Exit(1);
                }
            };

            window.Update += delta =>
            {
                float dt = (float)delta;
                _frameTime += dt;
                _frameCount++;

                _debugTimer += dt;
                _consoleOutputTimer += dt;

                if (_debugTimer > 5f)
                {
                    if (_consoleOutputTimer > 5f)
                    {
                        float avgFrameTime = _frameTime / _frameCount;
                        float fps = 1.0f / avgFrameTime;
                        _consoleOutputTimer = 0f;
                        _frameTime = 0f;
                        _frameCount = 0;
                    }
                    _debugTimer = 0f;
                }

                _overlayUI?.Update(dt);

                if (_overlayUI?.StartGameRequested == true)
                {
                    if (_overlayUI.CurrentState == GameState.Playing && (_gameOver || !_gameInitialized))
                    {
                        StartGame();
                    }
                }

                if (_overlayUI?.CurrentState == GameState.Playing && !_gameOver && _gameInitialized)
                {
                    _gameTime += dt;

                    HandleInput();
                    _spaceship?.Update(dt);
                    _lastSpaceshipPosition = _spaceship?.Position ?? Vector3D<float>.Zero;

                    UpdateAsteroids(dt);
                    UpdateCamera();

                    if (_gameTime - _lastCollisionTime > _collisionCooldown)
                    {
                        CheckCollisions();
                    }
                }
                else if (_gameOver)
                {
                    _gameOverTimer += dt;
                    if (_gameOverTimer > 5f)
                    {
                        _overlayUI?.SetGameState(GameState.GameOver);
                    }
                }
            };

            window.Render += delta =>
            {
                _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                var currentState = _overlayUI?.CurrentState ?? GameState.MainMenu;

                if (currentState == GameState.MainMenu)
                {
                    if (_camera != null && _skybox != null)
                    {
                        var viewMatrix = _camera.GetViewMatrix();
                        _gl.DepthFunc(DepthFunction.Lequal);
                        _skybox.Draw(viewMatrix, _projection);
                        _gl.DepthFunc(DepthFunction.Less);
                    }

                    _overlayUI?.DrawMainMenu(window.Size.X, window.Size.Y);
                }
                else if (currentState == GameState.Playing && !_gameOver && _gameInitialized)
                {
                    if (_camera != null)
                    {
                        var viewMatrix = _camera.GetViewMatrix();

                        _gl.DepthFunc(DepthFunction.Lequal);
                        _skybox?.Draw(viewMatrix, _projection);
                        _gl.DepthFunc(DepthFunction.Less);

                        if (!_firstPersonView && _spaceship != null)
                        {
                            _spaceship.Draw(viewMatrix, _projection);
                        }

                        RenderAsteroids(viewMatrix);
                        _overlayUI?.DrawInGameUI(_playerLives, _firstPersonView, _gameTime, window.Size.X, window.Size.Y);
                    }
                }
                else if (currentState == GameState.GameOver || _gameOver)
                {
                    if (_camera != null && _skybox != null)
                    {
                        var viewMatrix = _camera.GetViewMatrix();
                        _gl.DepthFunc(DepthFunction.Lequal);
                        _skybox.Draw(viewMatrix, _projection);
                        _gl.DepthFunc(DepthFunction.Less);
                    }

                    _overlayUI?.DrawGameOverScreen(_gameTime, window.Size.X, window.Size.Y);
                }
            };

            window.FramebufferResize += size =>
            {
                _gl.Viewport(size);
                _projection = Matrix4X4.CreatePerspectiveFieldOfView(
                    MathHelper.DegreesToRadians(60f),
                    size.X / (float)size.Y,
                    0.1f,
                    1000f
                );
            };

            return window;
        }

        private static void RenderAsteroids(Matrix4X4<float> viewMatrix)
        {
            if (_asteroids == null) return;

            foreach (var asteroid in _asteroids)
            {
                if (asteroid == null) continue;

                var cameraPos = _camera?.Position ?? Vector3D<float>.Zero;
                var asteroidDistance = Vector3D.Distance(cameraPos, asteroid.Position);

                if (asteroidDistance > 1000f || asteroid.Position.Z > cameraPos.Z + 50f)
                {
                    continue;
                }

                asteroid.Draw(viewMatrix, _projection, _shader);
            }
        }

        private static void StartGame()
        {
            _playerLives = 3;
            _gameOver = false;
            _gameOverTimer = 0f;
            _gameTime = 0f;
            _asteroidsAvoided = 0;
            _lastCollisionTime = 0f;
            _asteroidSpawnTimer = 0f;

            _spaceship = new Spaceship(_gl, _shader);
            _spaceship.AttachInput(_input);

            InitializeAsteroids();

            _gameInitialized = true;
            _overlayUI?.SetGameState(GameState.Playing);
            Console.WriteLine("Game started successfully");
        }

        private static void InitializeAsteroids()
        {
            _asteroids = new List<Asteroid>();

            for (int i = 0; i < 35; i++)
            {
                CreateRandomAsteroid();
            }
        }

        private static void CreateRandomAsteroid()
        {
            if (_asteroids == null) return;

            float difficultyMultiplier = 1.0f + (_gameTime * 0.01f);

            Vector3D<float> startPosition;
            Vector3D<float> direction;
            float speed;

            if (_random.NextSingle() < 0.6f)
            {
                startPosition = CreateInterceptingSpawnPosition();
                direction = CreateInterceptingDirection(startPosition);
                speed = 120f + _random.NextSingle() * 180f;
            }
            else
            {
                startPosition = CreateRandomSpawnPosition();
                direction = CreateRandomDirection(startPosition);
                speed = 100f + _random.NextSingle() * 150f;
            }

            speed *= difficultyMultiplier;
            speed = Math.Min(speed, 400f);

            var asteroid = new Asteroid(
                _gl, _shader,
                "Assets/Models/Asteroids/Asteroid_Asset_Pack.obj",
                "Assets/Textures/Asteroid/albedo.jpg",
                startPosition,
                direction,
                speed,
                _random.NextSingle() * 2f
            );

            _asteroids.Add(asteroid);
        }

        private static Vector3D<float> CreateInterceptingSpawnPosition()
        {
            var spaceshipPos = _lastSpaceshipPosition;
            var spawnDistance = 800f + _random.NextSingle() * 1200f;
            var spawnAngle = _random.NextSingle() * MathF.PI * 2f;

            var randomX = spaceshipPos.X + MathF.Cos(spawnAngle) * spawnDistance;
            var randomY = spaceshipPos.Y + MathF.Sin(spawnAngle) * spawnDistance;
            var randomZ = spaceshipPos.Z - 300f - _random.NextSingle() * 400f;

            return new Vector3D<float>(randomX, randomY, randomZ);
        }

        private static Vector3D<float> CreateRandomSpawnPosition()
        {
            var randomX = (_random.NextSingle() - 0.5f) * 2000f;
            var randomY = (_random.NextSingle() - 0.5f) * 1600f;
            var randomZ = -200f - _random.NextSingle() * 800f;

            return new Vector3D<float>(randomX, randomY, randomZ);
        }

        private static Vector3D<float> CreateInterceptingDirection(Vector3D<float> startPosition)
        {
            var spaceshipPos = _lastSpaceshipPosition;
            var baseDirection = Vector3D.Normalize(spaceshipPos - startPosition);

            var randomOffset = new Vector3D<float>(
                (_random.NextSingle() - 0.5f) * 0.4f,
                (_random.NextSingle() - 0.5f) * 0.4f,
                _random.NextSingle() * 0.2f
            );

            return Vector3D.Normalize(baseDirection + randomOffset);
        }

        private static Vector3D<float> CreateRandomDirection(Vector3D<float> startPosition)
        {
            var targetX = _random.NextSingle() * 150f - 75f;
            var targetY = _random.NextSingle() * 150f - 75f;
            var targetZ = 250f + _random.NextSingle() * 150f;

            var targetVector = new Vector3D<float>(targetX, targetY, targetZ);
            return Vector3D.Normalize(targetVector - startPosition);
        }

        private static void HandleInput()
        {
            if (_input?.Keyboards == null || _input.Keyboards.Count == 0) return;

            var keyboard = _input.Keyboards[0];

            bool vKeyCurrentlyPressed = keyboard.IsKeyPressed(Key.V);
            if (vKeyCurrentlyPressed && !_vKeyPressed)
            {
                _firstPersonView = !_firstPersonView;
            }
            _vKeyPressed = vKeyCurrentlyPressed;

            bool escKeyCurrentlyPressed = keyboard.IsKeyPressed(Key.Escape);
            if (escKeyCurrentlyPressed && !_escKeyPressed)
            {
                Environment.Exit(0);
            }
            _escKeyPressed = escKeyCurrentlyPressed;
        }

        private static void UpdateAsteroids(float deltaTime)
        {
            if (_asteroids == null) return;

            int maxAsteroids = (int)(35 + (_gameTime * 0.8f));
            maxAsteroids = Math.Min(maxAsteroids, 60);

            _asteroidSpawnTimer += deltaTime;

            for (int i = _asteroids.Count - 1; i >= 0; i--)
            {
                if (_asteroids[i] == null) continue;

                _asteroids[i].Update(deltaTime);

                if (_asteroids[i].Position.Z > 150f) // no collision
                {
                    _asteroidsAvoided++;
                    _asteroids.RemoveAt(i);
                    continue;
                }

                if (Math.Abs(_asteroids[i].Position.X) > 1500f ||
                    Math.Abs(_asteroids[i].Position.Y) > 1200f ||
                    _asteroids[i].Position.Z < -1200f)
                {
                    _asteroids.RemoveAt(i);
                    continue;
                }
            }

            if (_asteroidSpawnTimer > 0.2f && _asteroids.Count < maxAsteroids)
            {
                CreateRandomAsteroid();
                _asteroidSpawnTimer = 0f;
            }

            if (_gameTime > 20f && _asteroidSpawnTimer > 0.3f && _random.NextSingle() < 0.15f)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (_asteroids.Count < maxAsteroids)
                    {
                        CreateRandomAsteroid();
                    }
                }
                _asteroidSpawnTimer = 0f;
            }
        }

        private static void UpdateCamera()
        {
            if (_camera == null || _spaceship == null) return;

            if (_firstPersonView)
            {
                var cockpitOffset = new Vector3D<float>(0, 2f, 3f);
                _camera.SetPosition(_spaceship.Position + cockpitOffset);
                _camera.SetFront(_spaceship.Direction);
            }
            else
            {
                var cameraOffset = -_spaceship.Direction * 25f + new Vector3D<float>(0, 8f, 0);
                var cameraPosition = _spaceship.Position + cameraOffset;

                _camera.SetPosition(cameraPosition);
                _camera.SetFront(Vector3D.Normalize(_spaceship.Position - cameraPosition));
            }
        }

        private static void CheckCollisions()
        {
            if (_spaceship == null || _asteroids == null) return;

            var spaceshipPos = _lastSpaceshipPosition;

            foreach (var asteroid in _asteroids)
            {
                if (asteroid == null) continue;

                float distance = Vector3D.Distance(spaceshipPos, asteroid.Position);
                float collisionDistance = _spaceship.GetCollisionRadius() + asteroid.Radius;

                if (distance < collisionDistance)
                {
                    _playerLives--;
                    _lastCollisionTime = _gameTime;

                    asteroid.ResetToFarPosition();

                    if (_playerLives <= 0)
                    {
                        _gameOver = true;
                        _gameOverTimer = 0f;
                        _overlayUI?.SetGameState(GameState.GameOver);
                    }

                    break;
                }
            }
        }

        public static void Run(IWindow window)
        {
            window.Run();
        }
    }
}
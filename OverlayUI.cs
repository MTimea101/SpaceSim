using Silk.NET.OpenGL;
using Silk.NET.Maths;
using Silk.NET.Input;
using System;
using System.Collections.Generic;

namespace SpaceSim
{
    public class OverlayUI
    {
        private readonly GL _gl;
        private readonly Shader _uiShader;
        private uint _quadVAO;
        private uint _quadVBO;
        private IKeyboard? _keyboard;
        private IMouse? _mouse;
        private bool _enterPressed = false;
        private bool _spacePressed = false;

        // Mouse state
        private bool _mousePressed = false;
        private Vector2D<float> _mousePosition = new(0, 0);

        // Button areas (for click detection)
        private Rectangle _startButtonArea;
        private bool _startButtonHovered = false;

        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public bool StartGameRequested { get; private set; } = false;

        // UI Colors
        private static readonly Vector3D<float> PrimaryColor = new(0.2f, 0.6f, 1.0f);   // Bright blue
        private static readonly Vector3D<float> SecondaryColor = new(0.1f, 0.4f, 0.8f); // Dark blue
        private static readonly Vector3D<float> AccentColor = new(0.8f, 0.9f, 1.0f);    // Light blue
        private static readonly Vector3D<float> WarningColor = new(1.0f, 0.4f, 0.2f);   // Orange-red
        private static readonly Vector3D<float> SuccessColor = new(0.2f, 0.8f, 0.4f);   // Green
        private static readonly Vector3D<float> DangerColor = new(0.9f, 0.2f, 0.2f);    // Red

        public struct Rectangle
        {
            public float X, Y, Width, Height;
            public Rectangle(float x, float y, float width, float height)
            {
                X = x; Y = y; Width = width; Height = height;
            }

            public bool Contains(Vector2D<float> point)
            {
                return point.X >= X && point.X <= X + Width &&
                       point.Y >= Y && point.Y <= Y + Height;
            }
        }

        public OverlayUI(GL gl)
        {
            _gl = gl;

            try
            {
                _uiShader = CreateUIShader();
                CreateQuad();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OverlayUI constructor error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public void AttachInput(IInputContext input)
        {
            if (input.Keyboards.Count > 0)
            {
                _keyboard = input.Keyboards[0];
            }

            if (input.Mice.Count > 0)
            {
                _mouse = input.Mice[0];
                _mouse.MouseMove += OnMouseMove;
                _mouse.MouseDown += OnMouseDown;
                _mouse.MouseUp += OnMouseUp;
            }
        }

        private void OnMouseMove(IMouse mouse, System.Numerics.Vector2 position)
        {
            _mousePosition = new Vector2D<float>(position.X, position.Y);

            // Check if hovering over start button
            _startButtonHovered = _startButtonArea.Contains(_mousePosition);
        }

        private void OnMouseDown(IMouse mouse, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                _mousePressed = true;
            }
        }

        private void OnMouseUp(IMouse mouse, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                _mousePressed = false;

                // Check if clicked on start button
                if (_startButtonArea.Contains(_mousePosition))
                {
                    HandleStartButtonClick();
                }
            }
        }

        private void HandleStartButtonClick()
        {
            if (CurrentState == GameState.MainMenu)
            {
                StartGameRequested = true;
                CurrentState = GameState.Playing;
            }
            else if (CurrentState == GameState.GameOver)
            {
                StartGameRequested = true;
                CurrentState = GameState.Playing;
            }
        }

        public void Update(float deltaTime)
        {
            if (_keyboard == null) return;
            HandleInput();
        }

        private void HandleInput()
        {
            if (_keyboard == null) return;

            bool enterCurrentlyPressed = _keyboard.IsKeyPressed(Key.Enter);
            bool spaceCurrentlyPressed = _keyboard.IsKeyPressed(Key.Space);

            if ((enterCurrentlyPressed && !_enterPressed) || (spaceCurrentlyPressed && !_spacePressed))
            {
                HandleStartButtonClick(); // Same logic as mouse click
            }

            _enterPressed = enterCurrentlyPressed;
            _spacePressed = spaceCurrentlyPressed;
        }

        public void SetGameState(GameState state)
        {
            CurrentState = state;
            StartGameRequested = false;
        }

        // PIXEL TEXT RENDERING METHODS
        private void DrawPixelText(string text, float x, float y, float scale, Vector3D<float> color, float alpha = 1.0f)
        {
            float currentX = x;
            float letterSpacing = 8 * scale;

            foreach (char c in text.ToUpper())
            {
                if (c == ' ')
                {
                    currentX += letterSpacing; // if space not drawing
                    continue;
                }

                DrawLetter(c, currentX, y, scale, color, alpha);
                currentX += letterSpacing; // go further
            }
        }

        private void DrawLetter(char letter, float x, float y, float scale, Vector3D<float> color, float alpha)
        {
            float pixelSize = 2 * scale;

            // Simple 5x7 pixel font patterns
            bool[,] pattern = GetLetterPattern(letter);

            for (int row = 0; row < 7; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    if (pattern[row, col])
                    {
                        DrawRect(x + col * pixelSize, y + row * pixelSize, pixelSize, pixelSize, color, alpha);
                    }
                }
            }
        }

        private bool[,] GetLetterPattern(char letter)
        {
            // 5x7 pixel patterns for letters
            switch (letter)
            {
                case 'S':
                    return new bool[,] {
                        {true, true, true, true, false},
                        {true, false, false, false, false},
                        {true, true, true, false, false},
                        {false, false, false, true, false},
                        {false, false, false, true, false},
                        {true, false, false, true, false},
                        {false, true, true, false, false}
                    };
                case 'P':
                    return new bool[,] {
                        {true, true, true, true, false},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, true, true, true, false},
                        {true, false, false, false, false},
                        {true, false, false, false, false},
                        {true, false, false, false, false}
                    };
                case 'A':
                    return new bool[,] {
                        {false, true, true, true, false},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, true, true, true, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true}
                    };
                case 'C':
                    return new bool[,] {
                        {false, true, true, true, false},
                        {true, false, false, false, true},
                        {true, false, false, false, false},
                        {true, false, false, false, false},
                        {true, false, false, false, false},
                        {true, false, false, false, true},
                        {false, true, true, true, false}
                    };
                case 'E':
                    return new bool[,] {
                        {true, true, true, true, true},
                        {true, false, false, false, false},
                        {true, false, false, false, false},
                        {true, true, true, true, false},
                        {true, false, false, false, false},
                        {true, false, false, false, false},
                        {true, true, true, true, true}
                    };
                case 'T':
                    return new bool[,] {
                        {true, true, true, true, true},
                        {false, false, true, false, false},
                        {false, false, true, false, false},
                        {false, false, true, false, false},
                        {false, false, true, false, false},
                        {false, false, true, false, false},
                        {false, false, true, false, false}
                    };
                case 'R':
                    return new bool[,] {
                        {true, true, true, true, false},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, true, true, true, false},
                        {true, false, true, false, false},
                        {true, false, false, true, false},
                        {true, false, false, false, true}
                    };
                case 'I':
                    return new bool[,] {
                        {false, true, true, true, false},
                        {false, false, true, false, false},
                        {false, false, true, false, false},
                        {false, false, true, false, false},
                        {false, false, true, false, false},
                        {false, false, true, false, false},
                        {false, true, true, true, false}
                    };
                case 'M':
                    return new bool[,] {
                        {true, false, false, false, true},
                        {true, true, false, true, true},
                        {true, false, true, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true}
                    };
                case 'L':
                    return new bool[,] {
                        {true, false, false, false, false},
                        {true, false, false, false, false},
                        {true, false, false, false, false},
                        {true, false, false, false, false},
                        {true, false, false, false, false},
                        {true, false, false, false, false},
                        {true, true, true, true, true}
                    };
                case 'O':
                    return new bool[,] {
                        {false, true, true, true, false},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {false, true, true, true, false}
                    };
                case 'N':
                    return new bool[,] {
                        {true, false, false, false, true},
                        {true, true, false, false, true},
                        {true, false, true, false, true},
                        {true, false, false, true, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true}
                    };
                case 'W':
                    return new bool[,] {
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, true, false, true},
                        {true, false, true, false, true},
                        {true, true, false, true, true},
                        {true, false, false, false, true}
                    };
                case 'V':
                    return new bool[,] {
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {false, true, false, true, false},
                        {false, true, false, true, false},
                        {false, false, true, false, false}
                    };
                case 'D':
                    return new bool[,] {
                        {true, true, true, true, false},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, true, true, true, false}
                    };
                case 'F':
                    return new bool[,] {
                        {true, true, true, true, true},
                        {true, false, false, false, false},
                        {true, false, false, false, false},
                        {true, true, true, true, false},
                        {true, false, false, false, false},
                        {true, false, false, false, false},
                        {true, false, false, false, false}
                    };
                case 'G':
                    return new bool[,] {
                        {false, true, true, true, false},
                        {true, false, false, false, true},
                        {true, false, false, false, false},
                        {true, false, true, true, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {false, true, true, true, false}
                    };
                case 'H':
                    return new bool[,] {
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, true, true, true, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true}
                    };
                case 'U':
                    return new bool[,] {
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {false, true, true, true, false}
                    };
                case 'B':
                    return new bool[,] {
                        {true, true, true, true, false},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, true, true, true, false},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, true, true, true, false}
                    };
                case 'K':
                    return new bool[,] {
                        {true, false, false, false, true},
                        {true, false, false, true, false},
                        {true, false, true, false, false},
                        {true, true, false, false, false},
                        {true, false, true, false, false},
                        {true, false, false, true, false},
                        {true, false, false, false, true}
                    };
                case 'Y':
                    return new bool[,] {
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {false, true, false, true, false},
                        {false, false, true, false, false},
                        {false, false, true, false, false},
                        {false, false, true, false, false},
                        {false, false, true, false, false}
                    };
                case '0':
                    return new bool[,] {
                        {false, true, true, true, false},
                        {true, false, false, false, true},
                        {true, false, false, true, true},
                        {true, false, true, false, true},
                        {true, true, false, false, true},
                        {true, false, false, false, true},
                        {false, true, true, true, false}
                    };
                case '1':
                    return new bool[,] {
                        {false, false, true, false, false},
                        {false, true, true, false, false},
                        {false, false, true, false, false},
                        {false, false, true, false, false},
                        {false, false, true, false, false},
                        {false, false, true, false, false},
                        {false, true, true, true, false}
                    };
                case '2':
                    return new bool[,] {
                        {false, true, true, true, false},
                        {true, false, false, false, true},
                        {false, false, false, false, true},
                        {false, false, false, true, false},
                        {false, false, true, false, false},
                        {false, true, false, false, false},
                        {true, true, true, true, true}
                    };
                case '3':
                    return new bool[,] {
                        {false, true, true, true, false},
                        {true, false, false, false, true},
                        {false, false, false, false, true},
                        {false, false, true, true, false},
                        {false, false, false, false, true},
                        {true, false, false, false, true},
                        {false, true, true, true, false}
                    };
                case '4':
                    return new bool[,] {
                        {false, false, false, true, false},
                        {false, false, true, true, false},
                        {false, true, false, true, false},
                        {true, false, false, true, false},
                        {true, true, true, true, true},
                        {false, false, false, true, false},
                        {false, false, false, true, false}
                    };
                case '5':
                    return new bool[,] {
                        {true, true, true, true, true},
                        {true, false, false, false, false},
                        {true, true, true, true, false},
                        {false, false, false, false, true},
                        {false, false, false, false, true},
                        {true, false, false, false, true},
                        {false, true, true, true, false}
                    };
                case '6':
                    return new bool[,] {
                        {false, false, true, true, false},
                        {false, true, false, false, false},
                        {true, false, false, false, false},
                        {true, true, true, true, false},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {false, true, true, true, false}
                    };
                case '7':
                    return new bool[,] {
                        {true, true, true, true, true},
                        {false, false, false, false, true},
                        {false, false, false, true, false},
                        {false, false, true, false, false},
                        {false, true, false, false, false},
                        {false, true, false, false, false},
                        {false, true, false, false, false}
                    };
                case '8':
                    return new bool[,] {
                        {false, true, true, true, false},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {false, true, true, true, false},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {false, true, true, true, false}
                    };
                case '9':
                    return new bool[,] {
                        {false, true, true, true, false},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {false, true, true, true, true},
                        {false, false, false, false, true},
                        {false, false, false, true, false},
                        {false, true, true, false, false}
                    };
                case '.':
                    return new bool[,] {
                        {false, false, false, false, false},
                        {false, false, false, false, false},
                        {false, false, false, false, false},
                        {false, false, false, false, false},
                        {false, false, false, false, false},
                        {false, false, false, false, false},
                        {false, true, true, false, false}
                    };
                case ':':
                    return new bool[,] {
                        {false, false, false, false, false},
                        {false, false, false, false, false},
                        {false, true, true, false, false},
                        {false, false, false, false, false},
                        {false, false, false, false, false},
                        {false, true, true, false, false},
                        {false, false, false, false, false}
                    };
                case '-':
                    return new bool[,] {
                        {false, false, false, false, false},
                        {false, false, false, false, false},
                        {false, false, false, false, false},
                        {true, true, true, true, false},
                        {false, false, false, false, false},
                        {false, false, false, false, false},
                        {false, false, false, false, false}
                    };
                default:
                    return new bool[7, 5]; // Empty pattern for unknown characters
            }
        }

        public void DrawMainMenu(int screenWidth, int screenHeight)
        {
            try
            {
                SetupUIRendering(screenWidth, screenHeight);


                // Subtle dark background overlay
                DrawRect(0, 0, screenWidth, screenHeight, new Vector3D<float>(0.02f, 0.02f, 0.08f), 0.85f);

                // === TITLE SECTION ===
                DrawRect(0, 30, screenWidth, 120, new Vector3D<float>(0.1f, 0.2f, 0.4f), 0.9f);
                DrawRect(0, 35, screenWidth, 110, new Vector3D<float>(0.15f, 0.3f, 0.6f), 0.7f);

                // Title text
                DrawPixelText("SPACESIM", screenWidth / 2 - 120, 70, 3.0f, new Vector3D<float>(0.8f, 1.0f, 1.0f));

                // Title accent line
                DrawRect(screenWidth / 2 - 200, 50, 400, 4, new Vector3D<float>(0.4f, 0.8f, 1.0f), 1.0f);
                DrawRect(screenWidth / 2 - 150, 130, 300, 2, new Vector3D<float>(0.4f, 0.8f, 1.0f), 0.8f);

                // === MAIN CONTENT PANEL ===
                float panelWidth = Math.Min(screenWidth * 0.85f, 1000f);
                float panelHeight = screenHeight * 0.65f;
                float panelX = (screenWidth - panelWidth) / 2;
                float panelY = 180;

                DrawRect(panelX - 3, panelY - 3, panelWidth + 6, panelHeight + 6, new Vector3D<float>(0.3f, 0.6f, 1.0f), 0.6f); // Border
                DrawRect(panelX, panelY, panelWidth, panelHeight, new Vector3D<float>(0.08f, 0.12f, 0.25f), 0.95f); // Background

                // === LEFT PANEL - CONTROLS ===
                float leftPanelX = panelX + 40;
                float leftPanelY = panelY + 60;
                float leftPanelW = (panelWidth - 120) * 0.48f;
                float leftPanelH = panelHeight - 180;

                // Controls panel
                DrawRect(leftPanelX - 2, leftPanelY - 2, leftPanelW + 4, leftPanelH + 4, new Vector3D<float>(0.2f, 0.4f, 0.8f), 0.7f); // Border
                DrawRect(leftPanelX, leftPanelY, leftPanelW, leftPanelH, new Vector3D<float>(0.06f, 0.1f, 0.2f), 0.9f);

                // Controls title bar and text
                DrawRect(leftPanelX, leftPanelY, leftPanelW, 35, new Vector3D<float>(0.15f, 0.25f, 0.5f), 0.9f);
                DrawPixelText("CONTROLS", leftPanelX + 10, leftPanelY + 12, 1.5f, new Vector3D<float>(1.0f, 1.0f, 1.0f));

                // Control instructions
                float textY = leftPanelY + 50;
                float lineSpacing = 25;
                DrawPixelText("ARROWS - MOVE", leftPanelX + 10, textY + lineSpacing, 1.0f, new Vector3D<float>(0.8f, 0.9f, 1.0f));
                DrawPixelText("V - CAMERA", leftPanelX + 10, textY + lineSpacing * 2, 1.0f, new Vector3D<float>(0.8f, 0.9f, 1.0f));
                DrawPixelText("ESC - EXIT", leftPanelX + 10, textY + lineSpacing * 3, 1.0f, new Vector3D<float>(0.8f, 0.9f, 1.0f));

                // === RIGHT PANEL - MISSION INFO ===
                float rightPanelX = panelX + panelWidth - leftPanelW - 40;
                float rightPanelY = leftPanelY;

                // Mission info panel
                DrawRect(rightPanelX - 2, rightPanelY - 2, leftPanelW + 4, leftPanelH + 4, new Vector3D<float>(0.2f, 0.6f, 0.4f), 0.7f); // Border
                DrawRect(rightPanelX, rightPanelY, leftPanelW, leftPanelH, new Vector3D<float>(0.05f, 0.12f, 0.08f), 0.9f);

                // Mission title bar and text
                DrawRect(rightPanelX, rightPanelY, leftPanelW, 35, new Vector3D<float>(0.1f, 0.3f, 0.2f), 0.9f);
                DrawPixelText("MISSION", rightPanelX + 10, rightPanelY + 12, 1.5f, new Vector3D<float>(1.0f, 1.0f, 1.0f));

                // Mission instructions
                float missionTextY = rightPanelY + 50;
                DrawPixelText("SURVIVE AS LONG AS POSSIBLE", rightPanelX + 10, missionTextY + lineSpacing, 1.0f, new Vector3D<float>(0.8f, 1.0f, 0.8f));
                DrawPixelText("3 LIVES TOTAL", rightPanelX + 10, missionTextY + lineSpacing * 2, 1.0f, new Vector3D<float>(0.8f, 1.0f, 0.8f));

                // === START BUTTON (CLICKABLE) ===
                float buttonWidth = 350;
                float buttonHeight = 70;
                float buttonX = (screenWidth - buttonWidth) / 2;
                float buttonY = panelY + panelHeight - 100;

                // Store button area for click detection
                _startButtonArea = new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight);

                // Button colors change based on hover
                Vector3D<float> buttonColor = _startButtonHovered ?
                    new Vector3D<float>(0.2f, 0.7f, 0.3f) :
                    new Vector3D<float>(0.15f, 0.6f, 0.25f);
                Vector3D<float> buttonHighlight = _startButtonHovered ?
                    new Vector3D<float>(0.3f, 0.9f, 0.5f) :
                    new Vector3D<float>(0.25f, 0.8f, 0.4f);
                Vector3D<float> buttonGlow = _startButtonHovered ?
                    new Vector3D<float>(0.5f, 1.0f, 0.7f) :
                    new Vector3D<float>(0.4f, 1.0f, 0.6f);

                // Button glow effect (stronger when hovered)
                float glowSize = _startButtonHovered ? 8f : 5f;
                DrawRect(buttonX - glowSize, buttonY - glowSize, buttonWidth + glowSize * 2, buttonHeight + glowSize * 2,
                         new Vector3D<float>(0.2f, 0.8f, 0.3f), _startButtonHovered ? 0.6f : 0.4f);

                // Main button
                DrawRect(buttonX, buttonY, buttonWidth, buttonHeight, buttonColor, 0.95f);

                // Button highlight
                DrawRect(buttonX + 8, buttonY + 8, buttonWidth - 16, buttonHeight - 16, buttonHighlight, 0.8f);

                // Button inner glow
                DrawRect(buttonX + 15, buttonY + 15, buttonWidth - 30, buttonHeight - 30, buttonGlow, 0.6f);

                // Button text
                DrawPixelText("START MISSION", buttonX + 80, buttonY + 25, 2.0f, new Vector3D<float>(1.0f, 1.0f, 1.0f));

                // === STATUS INDICATORS ===
                // Lives indicator in top right
                float livesX = screenWidth - 250;
                float livesY = 30;
                DrawRect(livesX - 2, livesY - 2, 220, 84, new Vector3D<float>(0.8f, 0.2f, 0.2f), 0.6f); // Border
                DrawRect(livesX, livesY, 216, 80, new Vector3D<float>(0.15f, 0.05f, 0.05f), 0.9f);

                // Lives title bar
                DrawRect(livesX, livesY, 216, 25, new Vector3D<float>(0.4f, 0.1f, 0.1f), 0.9f);
                DrawPixelText("LIVES", livesX + 10, livesY + 8, 1.2f, new Vector3D<float>(1.0f, 1.0f, 1.0f));

                // Life indicators
                for (int i = 0; i < 3; i++)
                {
                    DrawRect(livesX + 20 + i * 50, livesY + 35, 40, 30, new Vector3D<float>(0.9f, 0.2f, 0.2f), 0.8f);
                }

                // === DECORATIVE ELEMENTS ===
                // Corner accents
                DrawRect(0, 0, 100, 4, new Vector3D<float>(0.4f, 0.8f, 1.0f), 0.8f);
                DrawRect(0, 0, 4, 100, new Vector3D<float>(0.4f, 0.8f, 1.0f), 0.8f);

                DrawRect(screenWidth - 100, 0, 100, 4, new Vector3D<float>(0.4f, 0.8f, 1.0f), 0.8f);
                DrawRect(screenWidth - 4, 0, 4, 100, new Vector3D<float>(0.4f, 0.8f, 1.0f), 0.8f);

                DrawRect(0, screenHeight - 4, 100, 4, new Vector3D<float>(0.4f, 0.8f, 1.0f), 0.8f);
                DrawRect(0, screenHeight - 100, 4, 100, new Vector3D<float>(0.4f, 0.8f, 1.0f), 0.8f);

                DrawRect(screenWidth - 100, screenHeight - 4, 100, 4, new Vector3D<float>(0.4f, 0.8f, 1.0f), 0.8f);
                DrawRect(screenWidth - 4, screenHeight - 100, 4, 100, new Vector3D<float>(0.4f, 0.8f, 1.0f), 0.8f);

                RestoreRendering();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DrawMainMenu error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public void DrawInGameUI(int lives, bool firstPersonView, float gameTime, int screenWidth, int screenHeight)
        {
            try
            {
                SetupUIRendering(screenWidth, screenHeight);

                // === TOP BAR ===
                // Main background with gradient
                DrawRect(0, 0, screenWidth, 90, new Vector3D<float>(0.0f, 0.0f, 0.0f), 0.7f);
                DrawRect(0, 85, screenWidth, 5, new Vector3D<float>(0.2f, 0.6f, 1.0f), 0.8f); // Bottom accent line

                // === LIVES DISPLAY (TOP LEFT) ===
                float heartSize = 45;
                float heartSpacing = 55;
                float heartsStartX = 25;

                // Lives panel background
                DrawRect(15, 15, 180, 60, new Vector3D<float>(0.1f, 0.0f, 0.0f), 0.8f);
                DrawRect(15, 15, 180, 25, new Vector3D<float>(0.3f, 0.1f, 0.1f), 0.9f); // Title bar

                // Lives label
                DrawPixelText("LIVES", 20, 18, 1.2f, new Vector3D<float>(1.0f, 1.0f, 1.0f));

                // Heart icons
                for (int i = 0; i < 3; i++)
                {
                    Vector3D<float> heartColor = i < lives ? new Vector3D<float>(0.9f, 0.2f, 0.2f) : new Vector3D<float>(0.3f, 0.1f, 0.1f);
                    float heartAlpha = i < lives ? 1.0f : 0.5f;

                    // Heart glow effect
                    if (i < lives)
                    {
                        DrawRect(heartsStartX + i * heartSpacing - 3, 42, heartSize + 6, heartSize + 6, heartColor, 0.3f);
                    }

                    DrawRect(heartsStartX + i * heartSpacing, 45, heartSize, heartSize, heartColor, heartAlpha);
                }

                // === GAME STATS (TOP CENTER) ===
                float statsWidth = 320;
                float statsX = (screenWidth - statsWidth) / 2;

                // Stats panel background
                DrawRect(statsX - 2, 13, statsWidth + 4, 66, new Vector3D<float>(0.1f, 0.3f, 0.6f), 0.8f); // Border
                DrawRect(statsX, 15, statsWidth, 62, new Vector3D<float>(0.05f, 0.1f, 0.2f), 0.9f);
                DrawRect(statsX, 15, statsWidth, 22, new Vector3D<float>(0.1f, 0.2f, 0.4f), 0.9f); // Title bar

                // Stats labels and values
                DrawPixelText("TIME", statsX + 10, 18, 1.0f, new Vector3D<float>(1.0f, 1.0f, 1.0f));
                DrawPixelText($"{gameTime:F1}S", statsX + 10, 40, 1.2f, new Vector3D<float>(0.4f, 0.8f, 1.0f));

                // Stats divider
                DrawRect(statsX + statsWidth / 2 - 1, 37, 2, 38, new Vector3D<float>(0.3f, 0.6f, 1.0f), 0.6f);

                // === CAMERA MODE (TOP RIGHT) ===
                float cameraWidth = 180;
                float cameraX = screenWidth - cameraWidth - 25;

                // Camera panel
                Vector3D<float> cameraColor = firstPersonView ? new Vector3D<float>(0.8f, 0.4f, 0.1f) : new Vector3D<float>(0.1f, 0.4f, 0.8f);
                DrawRect(cameraX - 2, 13, cameraWidth + 4, 66, cameraColor, 0.8f); // Border
                DrawRect(cameraX, 15, cameraWidth, 62, new Vector3D<float>(0.05f, 0.05f, 0.1f), 0.9f);
                DrawRect(cameraX, 15, cameraWidth, 22, cameraColor, 0.7f); // Title bar

                // Camera mode text
                DrawPixelText("CAMERA", cameraX + 10, 18, 1.0f, new Vector3D<float>(1.0f, 1.0f, 1.0f));
                string cameraMode = firstPersonView ? "COCKPIT" : "EXTERNAL";
                DrawPixelText(cameraMode, cameraX + 10, 40, 1.0f, new Vector3D<float>(1.0f, 1.0f, 0.8f));

                // === WARNING EFFECTS FOR LOW HEALTH ===
                if (lives == 1)
                {
                    // Animated warning border
                    float warningAlpha = 0.6f;
                    DrawRect(0, 0, screenWidth, 8, DangerColor, warningAlpha);              // Top
                    DrawRect(0, screenHeight - 8, screenWidth, 8, DangerColor, warningAlpha); // Bottom
                    DrawRect(0, 0, 8, screenHeight, DangerColor, warningAlpha);             // Left
                    DrawRect(screenWidth - 8, 0, 8, screenHeight, DangerColor, warningAlpha); // Right

                    // Critical warning indicator
                    float warningX = screenWidth / 2 - 100;
                    DrawRect(warningX - 2, 100, 204, 34, DangerColor, 0.9f);
                    DrawRect(warningX, 102, 200, 30, new Vector3D<float>(0.2f, 0.0f, 0.0f), 0.95f);
                    DrawPixelText("CRITICAL", warningX + 10, 110, 1.5f, new Vector3D<float>(1.0f, 0.8f, 0.8f));
                }

                // === MINI CONTROLS PANEL (BOTTOM RIGHT) ===
                float controlsWidth = 240;
                float controlsHeight = 110;
                float controlsX = screenWidth - controlsWidth - 20;
                float controlsY = screenHeight - controlsHeight - 20;

                // Controls panel
                DrawRect(controlsX - 2, controlsY - 2, controlsWidth + 4, controlsHeight + 4, new Vector3D<float>(0.1f, 0.2f, 0.1f), 0.6f); // Border
                DrawRect(controlsX, controlsY, controlsWidth, controlsHeight, new Vector3D<float>(0.03f, 0.08f, 0.03f), 0.8f);
                DrawRect(controlsX, controlsY, controlsWidth, 25, new Vector3D<float>(0.1f, 0.2f, 0.1f), 0.9f); // Title bar

                // Controls text
                DrawPixelText("CONTROLS", controlsX + 10, controlsY + 5, 1.0f, new Vector3D<float>(1.0f, 1.0f, 1.0f));
                DrawPixelText("ARROWS TO MOVE", controlsX + 10, controlsY + 35, 0.8f, new Vector3D<float>(0.8f, 1.0f, 0.8f));
                DrawPixelText("V CAMERA", controlsX + 10, controlsY + 50, 0.8f, new Vector3D<float>(0.8f, 1.0f, 0.8f));
                DrawPixelText("ESC EXIT", controlsX + 10, controlsY + 65, 0.8f, new Vector3D<float>(0.8f, 1.0f, 0.8f));

                // === SUBTLE CORNER INDICATORS ===
                // Top corners
                DrawRect(0, 0, 60, 3, new Vector3D<float>(0.3f, 0.7f, 1.0f), 0.7f);
                DrawRect(0, 0, 3, 60, new Vector3D<float>(0.3f, 0.7f, 1.0f), 0.7f);

                DrawRect(screenWidth - 60, 0, 60, 3, new Vector3D<float>(0.3f, 0.7f, 1.0f), 0.7f);
                DrawRect(screenWidth - 3, 0, 3, 60, new Vector3D<float>(0.3f, 0.7f, 1.0f), 0.7f);

                RestoreRendering();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DrawInGameUI error: {ex.Message}");
                throw;
            }
        }

        public void DrawGameOverScreen(float gameTime, int screenWidth, int screenHeight)
        {
            try
            {
                SetupUIRendering(screenWidth, screenHeight);

                // Full screen dark overlay
                DrawRect(0, 0, screenWidth, screenHeight, new Vector3D<float>(0.0f, 0.0f, 0.0f), 0.8f);

                // Game Over banner
                float bannerWidth = screenWidth * 0.8f;
                float bannerHeight = 150;
                float bannerX = (screenWidth - bannerWidth) / 2;
                float bannerY = 50;

                DrawRect(bannerX, bannerY, bannerWidth, bannerHeight, DangerColor, 0.9f);
                DrawRect(bannerX + 10, bannerY + 10, bannerWidth - 20, bannerHeight - 20, new Vector3D<float>(0.6f, 0.1f, 0.1f), 0.8f);

                // Game Over text
                DrawPixelText("MISSION FAILED", bannerX + 50, bannerY + 60, 3.0f, new Vector3D<float>(1.0f, 1.0f, 1.0f));

                // Stats panel
                float panelWidth = 500;
                float panelHeight = 200;
                float panelX = (screenWidth - panelWidth) / 2;
                float panelY = (screenHeight - panelHeight) / 2;

                DrawRect(panelX - 3, panelY - 3, panelWidth + 6, panelHeight + 6, new Vector3D<float>(0.3f, 0.3f, 0.6f), 0.8f); // Border
                DrawRect(panelX, panelY, panelWidth, panelHeight, new Vector3D<float>(0.1f, 0.1f, 0.2f), 0.9f);

                // Stats title
                DrawRect(panelX, panelY, panelWidth, 40, new Vector3D<float>(0.2f, 0.2f, 0.4f), 0.9f);
                DrawPixelText("MISSION REPORT", panelX + 20, panelY + 15, 1.5f, new Vector3D<float>(1.0f, 1.0f, 1.0f));

                // Stats content
                float statY = panelY + 60;
                DrawPixelText("SURVIVAL TIME", panelX + 20, statY, 1.2f, new Vector3D<float>(0.8f, 0.9f, 1.0f));
                DrawPixelText($"{gameTime:F1} SECONDS", panelX + 20, statY + 25, 1.5f, new Vector3D<float>(0.4f, 0.8f, 1.0f));

                // Restart button (clickable)
                float buttonWidth = 300;
                float buttonHeight = 80;
                float buttonX = (screenWidth - buttonWidth) / 2;
                float buttonY = screenHeight - 150;

                _startButtonArea = new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight);

                Vector3D<float> restartColor = _startButtonHovered ?
                    new Vector3D<float>(0.3f, 0.9f, 0.5f) : SuccessColor;
                Vector3D<float> restartHighlight = _startButtonHovered ?
                    new Vector3D<float>(0.4f, 1.0f, 0.6f) : new Vector3D<float>(0.3f, 0.9f, 0.5f);

                // Button with glow
                if (_startButtonHovered)
                {
                    DrawRect(buttonX - 5, buttonY - 5, buttonWidth + 10, buttonHeight + 10, restartColor, 0.4f);
                }

                DrawRect(buttonX, buttonY, buttonWidth, buttonHeight, restartColor, 0.9f);
                DrawRect(buttonX + 8, buttonY + 8, buttonWidth - 16, buttonHeight - 16, restartHighlight, 0.7f);

                // Button text
                DrawPixelText("RESTART", buttonX + 90, buttonY + 30, 2.0f, new Vector3D<float>(1.0f, 1.0f, 1.0f));

                RestoreRendering();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DrawGameOverScreen error: {ex.Message}");
                throw;
            }
        }

        private void SetupUIRendering(int screenWidth, int screenHeight)
        {
            _gl.Disable(GLEnum.DepthTest);
            _gl.Enable(GLEnum.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            var projection = Matrix4X4.CreateOrthographicOffCenter(0, screenWidth, screenHeight, 0, -1.0f, 1.0f);
            _uiShader.Use();
            _uiShader.SetMatrix4("projection", projection);
        }

        private void RestoreRendering()
        {
            _gl.Disable(GLEnum.Blend);
            _gl.Enable(GLEnum.DepthTest);
        }

        private void DrawRect(float x, float y, float width, float height, Vector3D<float> color, float alpha)
        {
            _uiShader.SetVector3("color", color);
            _uiShader.SetFloat("alpha", alpha);

            var model = Matrix4X4.CreateScale(width, height, 1.0f) *
                       Matrix4X4.CreateTranslation(x, y, 0.0f);

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

            uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
            _gl.ShaderSource(vertexShader, vertexSource);
            _gl.CompileShader(vertexShader);

            // Check compilation
            _gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
            {
                string log = _gl.GetShaderInfoLog(vertexShader);
                Console.WriteLine($"Vertex shader compilation failed: {log}");
                throw new Exception($"Vertex shader compilation failed: {log}");
            }

            uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
            _gl.ShaderSource(fragmentShader, fragmentSource);
            _gl.CompileShader(fragmentShader);

            // Check compilation
            _gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
            {
                string log = _gl.GetShaderInfoLog(fragmentShader);
                Console.WriteLine($"Fragment shader compilation failed: {log}");
                throw new Exception($"Fragment shader compilation failed: {log}");
            }

            uint program = _gl.CreateProgram();
            _gl.AttachShader(program, vertexShader);
            _gl.AttachShader(program, fragmentShader);
            _gl.LinkProgram(program);

            // Check linking
            _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int lStatus);
            if (lStatus != (int)GLEnum.True)
            {
                string log = _gl.GetProgramInfoLog(program);
                throw new Exception($"Shader program linking failed: {log}");
            }

            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);
            return new Shader(_gl, program);
        }

        private unsafe void CreateQuad()
        {
            float[] quadVertices = {
                0.0f, 1.0f,   0.0f, 1.0f,
                1.0f, 0.0f,   1.0f, 0.0f,
                0.0f, 0.0f,   0.0f, 0.0f,

                0.0f, 1.0f,   0.0f, 1.0f,
                1.0f, 1.0f,   1.0f, 1.0f,
                1.0f, 0.0f,   1.0f, 0.0f
            };

            _quadVAO = _gl.GenVertexArray();
            _quadVBO = _gl.GenBuffer();

            _gl.BindVertexArray(_quadVAO);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _quadVBO);

            fixed (float* v = quadVertices)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(quadVertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
            }

            _gl.EnableVertexAttribArray(0);
            _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);
            _gl.EnableVertexAttribArray(1);
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
        }
    }
}
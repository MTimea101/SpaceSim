# OpenGL Spaceship Simulator

This is a 3D OpenGL simulation project written in **C#** using **Silk.NET**, where you control a spaceship flying through a starry sky environment. The simulation features real-time rendering, object collision, and dynamic camera views.

---

## Features

- 🛸 Spaceship control with keyboard input
- 🌌 Skybox with starry background
- 🪨 Moving asteroids in the scene
- 🎥 Switchable internal/external camera views
- 💥 Collision detection with objects
- 🧱 .OBJ model loading for 3D assets
- 💡 Phong lighting and basic shading
- 🎮 Real-time rendering with OpenGL context

---

## Tech Stack

- **Language:** C#
- **Graphics API:** OpenGL via [Silk.NET](https://github.com/dotnet/Silk.NET)
- **Development Environment:** Visual Studio / Rider
- **Model Format:** .obj
- **Shaders:** GLSL

---

## Getting Started

1. **Clone the repository:**

   ```bash
   git clone https://github.com/YourUsername/OpenGL-Spaceship-Sim.git
   cd OpenGL-Spaceship-Sim
   ```

2. **Open the project in Visual Studio** or your preferred C# IDE.

3. **Build and run the project.**

---

## Controls

- `W`, `A`, `S`, `D`: Move spaceship
- `Arrow keys`: Adjust camera
- `C`: Switch camera view
- `ESC`: Exit simulation

---

## Assets

- Models are located in the `/Assets/Models/` folder (e.g., spaceship.obj)
- Skybox textures in `/Assets/Textures/Skybox/`
- Shaders in `/Assets/Shaders/`

---

## 📁 Project Structure

```
OpenGL-Spaceship-Sim/
│
├── Assets/
│   ├── Models/          # .obj 3D models
│   ├── Textures/        # Texture maps (skybox, ship, etc.)
│   └── Shaders/         # Vertex and fragment shaders
│
├── Core/                # Main rendering and game logic
├── Camera/              # Camera handling and view switching
├── Collision/           # Basic collision detection logic
├── Program.cs           # Entry point
├── README.md
└── .gitignore
```

---

## License

This project is licensed under the MIT License.

---

## Author

**Timea Majercsik**  
GitHub: [@MTimea101](https://github.com/MTimea101)

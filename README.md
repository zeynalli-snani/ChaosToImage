# Image Particle Simulator
A Windows WPF app that turns an uploaded image into a particle-based "magic" animation.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?logo=windows&logoColor=white)

---

## 📝 Description
Image Particle Simulator is a desktop application built with WPF and .NET 8 that animates an uploaded image using a swarm of particles. It is designed for people who want a visually interesting image-to-particle effect with a simple upload-and-play workflow, while still giving control over particle density.

![Simulation Preview](./ImageParticleSimulatorWPF/res/preview2.gif)


### ✨ Key Features & Technical Highlights
* **Impulse-Based Physics Engine:** A custom-built 2D collision resolution system handling particle-to-particle and particle-to-boundary interactions.
* **Recording & Replay Logic:** Implements a "Recording Phase" to capture final positions and an "Assembly Phase" that uses inverted physics to reconstruct the image.
* **High-Performance Rendering:** Optimized for .NET 8 using `WriteableBitmap` and asynchronous processing to maintain fluid UI frame rates even at high particle densities.
* **Dynamic Radius Calculation:** Uses an Area Fill Ratio algorithm to automatically scale particle sizes based on the count and canvas dimensions.
---

## 🛠️ Built With
* [.NET 8](https://dotnet.microsoft.com/) - Application runtime and SDK
* [WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/) - Desktop UI framework
* C# - Core application language
* MVVM pattern - UI and logic separation

---

## 🧠 Technical Deep Dive

### Collision Resolution
The simulator uses a sub-stepping approach for physics calculations (`CollisionPasses = 3`) to ensure stability during high-velocity bursts. It implements impulse-based momentum exchange:
$$impulse = \frac{-2.0 \cdot \text{relativeVelocity} \cdot \text{normal}}{2}$$

### Image-to-Particle Mapping
During the recording phase, the application samples the `WriteableBitmap` of the source image. It maps the spatial coordinates of each particle to the nearest pixel to extract ARGB data, which is then stored in a `BallData` model for the assembly phase.

## ⚙️ Getting Started

---

Follow these steps to set up the project locally.

### Prerequisites
* [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
* Visual Studio 2022 or another editor with WPF support

```bash
dotnet --version
```

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/your-username/ImageParticleSimulatorWPF.git
   ```
2. Navigate to the project directory:
   ```bash
   cd ImageParticleSimulatorWPF
   ```
3. Restore and build the project:
   ```bash
   dotnet build
   ```
4. Open the solution in Visual Studio if you want to run it from the IDE, or launch it with `dotnet run` from the project folder:
   ```bash
   dotnet run
   ```

---

## 🚀 Usage

1. **Load Image:** Click the "Upload" button to select a local file.
2. **Configure Swarm:** Use the slider to set the particle count (optimized for up to 5,000 particles).
3. **Run Simulation:**
   - **Phase 1:** Particles are fired from the center with random velocities.
   - **Phase 2:** After 5 seconds, particles "seek" their original image positions using stored velocity vectors to recreate the image.

---

## 🤝 Contributing
Contributions make the open-source community an amazing place.

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 📄 License
Distributed under the MIT License. See `LICENSE.txt` for more information.

---

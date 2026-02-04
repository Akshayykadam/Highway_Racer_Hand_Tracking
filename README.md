# 🚗 Highway Racer - Gesture Control Edition

A **Highway Racing** game powered by **MediaPipe** for real-time hand and body tracking. Control your car using intuitive gestures via your webcam!

![Unity](https://img.shields.io/badge/Unity-6000.2.8f1-black?logo=unity)
![MediaPipe](https://img.shields.io/badge/MediaPipe-0.16.3-blue)
![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20iOS%20%7C%20Editor-green)

---

## ✨ Features

- **👋 Hand Gesture Steering**: Steer your car by moving your hand left/right
- **🏃 Body Gesture Control**: Use your body movement to steer, accelerate, and brake
- **🔥 Real-time Tracking**: 21-point hand landmarks or 33-point body pose detection
- **📱 Cross-Platform**: Runs on Desktop (Editor) and Mobile devices
- **🚀 Auto-Speed Mode**: Focus on steering while the car maintains constant speed

---

## 🎮 Controls

### Hand Control Mode

Control the car steering with **hand position**:

| Gesture | Action |
| :--- | :--- |
| 🖐️ **Move Hand Left** | Steer Left |
| 🖐️ **Move Hand Right** | Steer Right |
| 🖐️ **Hand in Center** | Go Straight |

> **Note:** The car uses auto-throttle by default. Simply steer with your hand position!

### Body Control Mode

Control the car with **body movements**:

| Gesture | Action |
| :--- | :--- |
| 🚶 **Lean Left** | Steer Left |
| 🚶 **Lean Right** | Steer Right |
| 🙋 **Move Up** | Accelerate |
| 🧎 **Move Down** | Brake |

---

## 🚀 Getting Started

### Prerequisites

- **Unity 6 (6000.2.8f1)** or later
- **Webcam** (for Editor/Desktop)

### Installation

1. **Clone the Repository**
   ```bash
   git clone https://github.com/yourusername/HighwayRacer-GestureControl.git
   ```

2. **Open in Unity**
   - Launch Unity Hub
   - Add/Open the project folder

3. **Play**
   - Open the main scene (`Assets/Highway Racer/Scenes/`)
   - Press **Play** in the Editor
   - Allow camera access if prompted

### 📱 Android Build Settings

To run on Android with GPU acceleration:

1. Go to `Project Settings > Player > Android > Other Settings`
2. Uncheck `Auto Graphics API`
3. **Remove** `Vulkan` (MediaPipe GPU requires OpenGLES)
4. Add `OpenGLES3`
5. Set **Minimum API Level** to `Android 7.0 (Nougat)` or higher

---

## 🛠️ Technical Details

### Core Scripts

| Script | Location | Description |
| :--- | :--- | :--- |
| `HandGestureCarController.cs` | `Scripts/MediaPipe/` | Hand position to steering input |
| `HandLandmarkerRunner.cs` | `Scripts/MediaPipe/` | MediaPipe hand tracking wrapper |
| `BodyGestureCarController.cs` | `Scripts/MediaPipe/` | Body pose to car controls |
| `BodyLandmarkerRunner.cs` | `Scripts/MediaPipe/` | MediaPipe pose tracking wrapper |
| `HR_PlayerHandler.cs` | `Scripts/` | Player car management |
| `HR_GamePlayHandler.cs` | `Scripts/` | Game state and loop |

### Gesture Recognition

- **Hand Control**: Uses wrist position (landmark 0) to determine steering direction
- **Body Control**: Uses nose position to detect leaning and forward/backward movement

### Key Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `autoThrottle` | 0.5 | Constant speed in hand mode |
| `steerSensitivity` | 2.0 | Steering response multiplier |
| `deadZone` | 0.1 | Center dead zone (no steering) |
| `steerSmoothing` | 8.0 | Smoothing factor |

---

## 📁 Project Structure

```
Assets/
├── Highway Racer/          # Core game assets
│   ├── Scripts/
│   │   ├── MediaPipe/      # Gesture control scripts
│   │   │   ├── HandGestureCarController.cs
│   │   │   ├── HandLandmarkerRunner.cs
│   │   │   ├── BodyGestureCarController.cs
│   │   │   └── BodyLandmarkerRunner.cs
│   │   ├── HR_PlayerHandler.cs
│   │   ├── HR_GamePlayHandler.cs
│   │   └── ...
│   ├── Prefabs/
│   └── Scenes/
├── PoseLandmarkSDK/        # Pose detection SDK
├── StreamingAssets/        # Model files
└── TextMesh Pro/           # UI text rendering
```

---

## 📄 License

This project is open-source.

## 🙏 Acknowledgments

- **MediaPipe** for the hand and pose tracking technology
- **Homuler** for the [MediaPipe Unity Plugin](https://github.com/homuler/MediaPipeUnityPlugin)
- **Highway Racer** base game assets

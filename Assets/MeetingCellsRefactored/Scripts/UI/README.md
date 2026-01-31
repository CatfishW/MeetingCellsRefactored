# GamePlayer UI System

A production-ready, cross-platform UI system for Unity games. Compatible with PC, WebGL, and Android platforms.

## Features

- **Cross-Platform Support**: Automatically detects platform and adjusts UI accordingly
- **Settings Management**: Complete settings panel with audio, graphics, and control options
- **Camera Mode Switching**: First-person and third-person camera toggle with smooth transitions
- **Mobile Controls**: Floating joysticks and action buttons for Android/iOS
- **Modern Aesthetics**: Clean, minimalist design with smooth animations
- **Audio Feedback**: UI sound effects for all interactions

## System Components

### Core Scripts

1. **GamePlayerUI.cs** - Main UI controller, manages all HUD elements
2. **InputManager.cs** - Centralized input handling for all platforms
3. **UIManager.cs** - Panel navigation and state management
4. **SettingsManager.cs** - Persistent settings (audio, graphics, controls)
5. **CameraModeController.cs** - First/third person camera switching
6. **FloatingJoystick.cs** - Mobile touch joystick implementation
7. **UISoundController.cs** - UI audio feedback system
8. **UIPanel.cs** - Base class for animated UI panels

## Setup Instructions

### 1. Create the UI Canvas

1. Create a new Canvas in your scene (GameObject > UI > Canvas)
2. Set Render Mode to "Screen Space - Overlay"
3. Add the `GamePlayerUI.cs` script to the Canvas
4. Configure the Canvas Scaler:
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 x 1080 (PC) or 1080 x 1920 (Mobile)
   - Screen Match Mode: Match Width Or Height
   - Match: 0.5

### 2. Create UI Panels

#### HUD Panel
Create a Panel as a child of Canvas for the in-game HUD:
- Settings Button (top-right corner)
- Camera Mode Button (top-right, next to settings)
- Mobile Controls (bottom of screen, only visible on mobile)

#### Settings Panel
Create a Panel for settings:
- Master Volume Slider
- Music Volume Slider
- SFX Volume Slider
- Quality Dropdown
- Resolution Dropdown
- Fullscreen Toggle
- Close Button

### 3. Setup Mobile Controls (for Android/iOS)

Create a Panel for mobile controls:
- Left Joystick (bottom-left)
- Right Joystick (bottom-right)
- Jump Button
- Sprint Button
- Interact Button

Add the `FloatingJoystick.cs` script to joystick GameObjects.

### 4. Camera Setup

1. Create two CinemachineCamera objects:
   - FirstPersonCamera (Priority: 10)
   - ThirdPersonCamera (Priority: 0)
2. Add `CameraModeController.cs` to your player or a dedicated manager object
3. Assign camera references in the inspector

### 5. Input System Setup

1. Ensure Unity Input System package is installed
2. Assign your Input Action Asset to the InputManager
3. The system automatically switches between PC and mobile input

## Platform Detection

The system automatically detects the platform:
```csharp
// PC: Full mouse/keyboard support, no mobile controls
// WebGL: Same as PC
// Android/iOS: Touch joysticks, touch-optimized buttons
```

To force a specific platform for testing:
```csharp
GamePlayerUI.Instance.forceMobileControls = true; // Force mobile UI
GamePlayerUI.Instance.forcePCControls = true;     // Force PC UI
```

## Usage Examples

### Toggle Settings
```csharp
GamePlayerUI.Instance.ToggleSettings();
```

### Switch Camera Mode
```csharp
GamePlayerUI.Instance.ToggleCameraMode();
// or
GamePlayerUI.Instance.SetCameraMode(GamePlayerUI.CameraMode.FirstPerson);
```

### Access Input
```csharp
Vector2 moveInput = InputManager.Instance.MoveInput;
Vector2 lookInput = InputManager.Instance.LookInput;
```

### Listen to Events
```csharp
GamePlayerUI.Instance.OnSettingsPanelToggled += (isOpen) => {
    Debug.Log($"Settings panel: {isOpen}");
};

GamePlayerUI.Instance.OnCameraModeChanged += (mode) => {
    Debug.Log($"Camera mode: {mode}");
};
```

## Customization

### Change Colors
Edit the `GamePlayerUI.cs` script or use Unity's UI theming system.

### Add New Settings
Extend `SettingsManager.cs` with new PlayerPrefs keys and events.

### Custom Animations
Extend `UIPanel.cs` and override animation methods.

## Integration with StorySystem

The UI system integrates seamlessly with the existing StorySystem:

```csharp
// Pause game when dialogue is active
StoryManager.Instance.OnStoryStarted += () => {
    InputManager.Instance.EnableUIInput();
};

StoryManager.Instance.OnStoryEnded += () => {
    InputManager.Instance.EnablePlayerInput();
};
```

## Performance Considerations

- UI uses object pooling for dynamic elements
- Mobile controls are disabled on PC builds (no overhead)
- Settings are cached to reduce PlayerPrefs calls
- Animations use unscaledDeltaTime for pause menu compatibility

## Assembly Definition

The UI system is in its own assembly (`UI.asmdef`) for faster compilation:
- References: TextMeshPro, InputSystem, Cinemachine
- Namespace: MeetingCellsRefactored.UI

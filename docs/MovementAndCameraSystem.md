# Movement and Camera System

## Reference code inspected

- `_references/LevelScripts/`
- `_references/Player/`

The useful ideas came from the old `InputManager`, `PlayerController`, `BallRollController`, and `CameraModeManager` scripts: separated input reads, action callbacks, camera-relative movement, grounded probing, jump buffering, coyote-style jump forgiveness, and a yaw/pitch camera pivot.

## What was rewritten

The old system was not copied. The new portfolio controller is a fresh, smaller `CharacterController` setup built for a walkable 3D portfolio world rather than a high-speed movement game.

Reused as design ideas only:

- New Input System action map reads for Move, Look, and Jump.
- WASD movement as a `Vector2`.
- Jump press/release events.
- Camera-relative movement direction.
- Ground checks with a probe.
- Jump buffering and coyote time.
- Smooth camera yaw/pitch input.

## Scripts created

- `Assets/Portfolio/Scripts/Input/InputManager.cs`
- `Assets/Portfolio/Scripts/Player/PlayerController.cs`
- `Assets/Portfolio/Scripts/Camera/PlayerCameraController.cs`
- `Assets/Portfolio/Editor/PortfolioMovementCameraSetup.cs`

Compatibility scripts updated:

- `Assets/Portfolio/Scripts/Player/PortfolioAvatarController.cs` now aliases `PlayerController` for older prototype scene references.
- `Assets/Portfolio/Scripts/Camera/PortfolioCameraController.cs` now bridges old serialized target references to the new Cinemachine controller when both are present.
- The old empty `Assets/MyAssets/Scripts/Player/Movement/InputManager.cs` and `PlayerController.cs` placeholders were moved into a legacy namespace so they no longer conflict with the portfolio controller names.

## Player setup

Add these components to the player object:

- `CharacterController`
- `Portfolio.Input.InputManager`
- `Portfolio.Player.PlayerController`

Recommended `CharacterController` values:

- Center: `(0, 0.9, 0)`
- Height: `1.8`
- Radius: `0.38`
- Step Offset: `0.3`
- Slope Limit: `45`

The player controller exposes tunable movement speed, acceleration, gravity, jump height, coyote time, jump buffering, ground layers, and jump-cut settings in the Inspector.

Use the editor menu for safe setup:

- `Portfolio/Setup/Movement + Camera/Setup Active Scene`
- `Portfolio/Setup/Movement + Camera/Setup PlayerObj Prefab`

## Cinemachine camera setup

Cinemachine is already installed in `Packages/manifest.json` as `com.unity.cinemachine` and the project uses Cinemachine 3 APIs.

Scene setup:

- Main Camera has a `Camera` and `CinemachineBrain`.
- A virtual camera object has:
  - `CinemachineCamera`
  - `CinemachineThirdPersonFollow`
  - `CinemachineRotationComposer`
  - `Portfolio.Cameras.PlayerCameraController`
- `PlayerCameraController` targets the player and drives a `PortfolioCameraFollowRig` transform with smoothed yaw/pitch input.

The player movement is camera-relative. `PlayerCameraController` shares the active main camera transform back to `PlayerController` so WASD feels natural around the orbiting camera.

## Input bindings

The project input asset at `Assets/InputSystem_Actions.inputactions` already contains the required Player bindings:

- Move: WASD and arrow keys
- Jump: Space
- Look: Pointer delta / mouse movement

`InputManager` can also create a small runtime fallback map with WASD, Space, and mouse delta if the asset is not assigned.

## Intentionally left out

These old-project mechanics were deliberately not imported:

- Wall running
- Sliding
- Grappling and swinging
- Dashing
- Rails
- Combat
- Trick and score systems
- Speed lines
- Ball mode
- Camera mode switching
- Wallrun cameras
- Sprint, dash, grapple, or ball FOV changes
- Old game-specific UI/audio/data-manager dependencies

## Known issues and next steps

- Run `Portfolio/Setup/Movement + Camera/Setup Active Scene` on any scene that should become walkable.
- Run `Portfolio/Setup/Movement + Camera/Setup PlayerObj Prefab` if `Assets/MyAssets/Prefabs/Player/PlayerObj.prefab` should carry the controller by default.
- Tune move speed, jump height, camera distance, pitch limits, and damping after testing inside the portfolio hub environment.
- If future WebGL UX needs click-to-lock instead of auto cursor lock, expose that policy from `InputManager`.

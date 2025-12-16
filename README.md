# GameArena

Third person action prototype built in Unity. Player fights enemies in an arena style map, manages ammo and health, and can restart or return to the main menu on death.

## Project Summary
- **Perspective:** Third Person
- **Core loop:** Explore arena → fight enemies → manage ammo/health → survive
- **Scenes:**
  - `GameArena/MainMenu` (build index 0)
  - `GameArena/Map_v1` (build index 1)

## Gameplay Features
### Player
- Movement with:
  - Walk
  - Sprint
  - Jump
  - Crouch
- Third person camera:
  - Cinemachine based pan/tilt
  - Aim toggle (drives Animator `Aim` boolean)
- Health system:
  - `Health` component with death event (`OnDeath`)
  - `IDamageable` interface for damage handling

### Weapons
- `Rifle`:
  - Hitscan shooting (camera based raycast)
  - Ammo + reload
  - Muzzle flash

### Enemies
- `MechMutantEnemy`:
  - NavMeshAgent chasing
  - Attack behavior
  - Own health, audio, animations

### Pickups and Spawning
- `AmmoPickup` and `HealthPickup`
- `RandomScatterSpawner` for distributing pickups or objects in the arena

### Interaction
- `IInteractable` + `PlayerInteractor` for interaction based features (pickup or other interact actions depending on setup)

## UI
### In game HUD
Typical HUD elements include:
- Remaining or tracked enemies (`EnemyTrackerHUD`)
- Player health bar (`PlayerHealthBar`)
- Weapon ammo UI (`RifleAmmoUI`)
- Minimap follow (`MinimapFollow`) if enabled

### Game Over
- Game over screen is shown when the player dies.
- Player death flow is handled by `PlayerHealth`:
  - listens to `Health.OnDeath`
  - disables control and other input scripts
  - triggers death animation
  - shows game over UI after the death animation finishes (optionally after an extra delay)
- Game over menu buttons:
  - **Restart** reloads current gameplay scene
  - **Main Menu** loads build index 0


## Controls
Controls depend on the input mappings in the current Unity project, but the gameplay expects:
- Move
- Look
- Jump
- Sprint
- Crouch
- Fire
- Reload
- Aim toggle

## How to Run
1. Open the project in Unity.
2. Open `GameArena/Map_v1`or `GameArena/MainMenu`.
3. Press Play.

## Script Organization

- `Core/` (Health, base systems)
- `Core/Interfaces/` (IDamageable)
- `Player/` (ControlScript, PlayerHealth, camera controller, death helpers)
- `Combat/` (Rifle)
- `Enemies/` (MechMutantEnemy)
- `UI/` (GameOverUI, HUD scripts, minimap)
- `Pickups/` (AmmoPickup, HealthPickup)
- `Interaction/` (IInteractable, PlayerInteractor)
- `Spawning/` (RandomScatterSpawner)
- `SceneManagement/` (MainMenu, MapSceneFixer)


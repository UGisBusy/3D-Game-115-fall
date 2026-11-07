# Obstacle Level Spec

Owner: jtntjtra04
Scene: `Assets/Scenes/ObstacleLevel.unity`

## Goal
Cross a chain of floating islands from the start to the goal without falling, while avoiding spikes and spinning blades, and also other obstacle in the future. Touching the **Star** at the end completes the level.

## Layout
For now, the level is a line of 5 islands, with a different challanges each island platform. As the progress goes on, I will improve the level design.

| Islands/platform | What the Player must do |
|-|-|
| Start Island | Move, jump and look around safely. The first Checkpoint is here. |
| Jump Islands | Jump across gaps that get wider. |
| Spike Island | Read the spike types and time a path through them. |
| Blade Islands | Cross narrow paths past spinning blades. |
| Final Island | Spikes and blades together. The Star is at the end. |

- Falling off any island kills the Player (Death Zone below the whole level).
- Hearts float along the path to recover HP.

## Level Manager
State machine, built with `TickState`, `Link` and `LevelTransitions`.

| State | Behaviour | Links |
|-|-|-|
| **Start** | Asks `SpawnManager` to spawn the Player at the first Checkpoint. | `StartFinished` → **Play** (next frame) |
| **Play** | Normal gameplay. Listens to `LevelEvents.Collected`. | ESC: `PauseRequested` → **Pause**<br>Star collected: `Completed` → **Complete** |
| **Pause** | `Time.timeScale = 0`, Player input off. Both are restored on exit. | ESC: `ResumeRequested` → **Play** |
| **Complete** | Player input off. | TBD |

## Spawn and Checkpoints
- `SpawnManager` holds the level's Checkpoints. The first one in its list is the level start.
- Only one Checkpoint is active at a time. 
- For now, to make the difference about it, touching a Checkpoint changes its color to green.
- When the Player dies and presses Space, the Player respawns at the active Checkpoint (green).

## Death Zone
- A trigger box under the level. When the Player falls into it, the Player dies.

## Collectibles
| Type | Effect | How it appears |
|-|-|-|
| **Heart** | Player HP + 1 (handled by Player) | `CollectibleSpawnManager` spawns one at every child spot. On every Player spawn (level start and every respawn), leftover Hearts are removed and a full set is spawned again. |
| **Star** | Completes the level | Placed by hand at the end of the level. |

## Traps

### Spikes
A tray of spikes that acts as one trap: one collider box covers the whole spikes.
- Active: cones visible. A Player inside the trigger takes 1 damage, then the trap waits `hitCooldown` (4 s) before it can hurt again.
- Inactive: cones hidden, no damage.
- A hit blocked by the Player's invincibility does not start the cooldown. For now.
- The asset of the spikes is created by myself using probuilder.

| Prefab | Toggle type | Behaviour |
|-|-|-|
| `NormalSpikes` | AlwaysActive | Always visible, always hurts. |
| `PeriodicSpikes` | Periodic | Shows and hides every `toggleInterval` (2 s). |
| `HiddenSpikes` | OnPlayerTouch | Starts hidden. When the Player steps in, the cones appear and the Player gets `touchGraceTime` to step out before it can hurt. Stays active until the Player respawns, then hides again. |


### Spinning Blade
- A long rod with a blade on each side, spinning in place
- Touching any part hurts the Player (1 damage) and pushes the Player away from the blade.

## UI
- HP text in the top-left corner: `HP: current/max`, updated from `PlayerEvents.HpChanged`.
- TBD

## Controls
Same as the Player (WASD, Space, Right Click dash, mouse camera), plus **ESC** to pause and resume.

## Planned
- Pause Menu and Level Complete Screen, shown from the Pause and Complete states.
- More obstacles, such as moving platforms.
- TBD

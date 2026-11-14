# Obstacle Level Spec

Owner: jtntjtra04
Scene: `Assets/Scenes/ObstacleLevel.unity`

## Goal
Cross a chain of floating islands from the start to the goal. Collect all 5 **Crystals** on the way to make the **Star** appear. Touching the Star completes the level.

## Layout
- 10 islands and a Star island, harder with each island.
- Each island uses the traps and Env Obj below in a different way.
- Falling off any island kills the Player (Death Zone below the whole level).
- Hearts float along the path to recover HP.
- The level doesn't have assets yet.

## Level Manager
State machine, built with `TickState`, `Link` and `LevelTransitions`.

| State | Behaviour | Links |
|-|-|-|
| **Start** | Asks `SpawnManager` to spawn the Player at the start Checkpoint. Counts the Crystals and hides the Star. | `StartFinished` → **Play** (next frame) |
| **Play** | Normal gameplay. Listens to `LevelEvents.Collected` and `ObstacleLevelEvents.CrystalCollected`. Shows the Star when the last Crystal is collected. | ESC: `PauseRequested` → **Pause**<br>Star collected: `Completed` → **Complete** |
| **Pause** | `Time.timeScale = 0`, Player input off. Both are restored on exit. | ESC: `ResumeRequested` → **Play** |
| **Complete** | Player input off. | TBD |

## Spawn and Checkpoints
- `ObstacleLevelManager` holds the start Checkpoint and gives it to `SpawnManager` when the level starts.
- Only one Checkpoint is active at a time. Touching a Checkpoint makes it the active one and changes its color to green.
- When the Player dies and presses Space, the Player respawns at the active Checkpoint (green).

## Death Zone
- A trigger box under the level. When the Player falls into it, the Player dies.
- When a box (Crate or Boulder) falls into it, the box is destroyed and goes back to its start position.

## Collectibles
| Type | Effect | How it appears |
|-|-|-|
| **Heart** | Player HP + 1 (handled by Player) | `CollectibleSpawnManager` spawns one at every child spot. On every Player spawn (level start and every respawn), leftover Hearts are removed and a full set is spawned again. |
| **Crystal** | Counts toward the Star. A collected Crystal stays collected after a respawn. | Placed by hand. There are 5 in the level. |
| **Star** | Completes the level | Placed by hand on the Star island. Stays hidden until all Crystals are collected. |

Crystal events are in `ObstacleLevelEvents`, which holds the events only this level uses.

## Env Obj

### Breakable Wall
- Blocks the Player. A dash destroys it.

### Crate (Movable) and Boulder (Dashable)
One base class, `PushableBox`. The two types of box have a different way to move them.

| Prefab | Script | How to move it |
|-|-|-|
| `Crate` | `MovableBox` | Pushed by walking or dashing. |
| `Boulder` | `DashableBox` | Pushed by a dash only, then glides to a stop. |

- A box follows the Player's speed and locks in place when nobody pushes it.
- A destroyed box goes back to its start position.

### Pressure Plate and Gate
- Only a box opens the Gate. The Player standing on the Plate does nothing.
- A fully opened Gate will stay open.

### Moving Platform (Cloud)
- Follows a route of points, then goes straight back to its start.
- Only starts moving while the Player stands on it.
- Can carry the Player and the box.
- Goes back to its start when the Player respawns.

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

- Hidden spikes can destroy a box that lands on them (only when the hidden spikes come out). The purpose is to let the Player check if there are any hidden spikes there.

### Spinning Blade
- A long rod with a blade on each side, spinning in place
- Touching any part hurts the Player (1 damage) and pushes the Player away from the blade.

## UI
- HP text in the top-left corner: `HP: current/max`, updated from `PlayerEvents.HpChanged`.
- Crystal text below it: `Crystals: collected/total`, updated from `ObstacleLevelEvents.CrystalCountChanged`.

## Controls
Same as the Player (WASD, Space, Right Click dash, mouse camera), plus **ESC** to pause and resume.

## Planned
- Pause Menu and Level Complete Screen, shown from the Pause and Complete states.
- Assets for the level.
- TBD

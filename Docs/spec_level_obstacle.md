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
- Models: Quaternius Platformer Game Kit and Kenney Platform Kit (both CC0). Grass blocks for floors, hedges for walls, trees, bushes and rocks on large floors, and clouds under the level.

## Level Manager
State machine, built with `TickState`, `Link` and `LevelTransitions`.

| State        | Behaviour                                                                                                                   | Links                                                                           |
| ------------ | --------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| **Start**    | Asks `SpawnManager` to spawn the Player at the start Checkpoint. Counts the Crystals and hides the Star.                    | `StartFinished` → **Play** (next frame)                                         |
| **Play**     | Normal gameplay. Listens to `LevelEvents.Collected`. Counts each Crystal and shows the Star when the last one is collected. | ESC: `PauseRequested` → **Pause**<br>Star collected: `Completed` → **Complete** |
| **Pause**    | `Time.timeScale = 0`, Player input off. Both are restored on exit.                                                          | ESC: `ResumeRequested` → **Play**                                               |
| **Complete** | Player input off.                                                                                                           | TBD                                                                             |

## Spawn and Checkpoints
- `ObstacleLevelManager` holds the start Checkpoint and gives it to `SpawnManager` when the level starts.
- Only one Checkpoint is active at a time. Touching a Checkpoint makes it the active one and changes its color to green.
- When the Player dies and presses Space, the Player respawns at the active Checkpoint (green).

## Death Zone
- A trigger box under the level. When the Player falls into it, the Player dies.
- When a box (Crate or Boulder) falls into it, the box is destroyed and goes back to its start position.

## Collectibles
| Type        | Effect                                                                       | How it appears                                                                                                                                                                |
| ----------- | ---------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Heart**   | Player HP + 1 (handled by Player)                                            | `CollectibleSpawnManager` spawns one at every child spot. On every Player spawn (level start and every respawn), leftover Hearts are removed and a full set is spawned again. |
| **Crystal** | Counts toward the Star. A collected Crystal stays collected after a respawn. | Placed by hand. There are 5 in the level. The Level Manager counts them when the level starts.                                                                                |
| **Star**    | Completes the level                                                          | Placed by hand on the Star island. Stays hidden until all Crystals are collected.                                                                                             |

The Crystal count for the HUD is sent through `ObstacleLevelEvents`, which holds the events only this level uses.

## Env Obj

### Breakable Wall
- Blocks the Player. A dash destroys it.

### Crate (Movable) and Boulder (Dashable)
One base class, `PushableBox`. The two types of box have a different way to move them.

| Prefab    | Script        | How to move it                                                                                                                                         |
| --------- | ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `Crate`   | `MovableBox`  | Pushed by walking or dashing.                                                                                                                          |
| `Boulder` | `DashableBox` | Pushed by a dash only. One dash moves it exactly `moveDistance`, as if it is on a grid. If a wall or another box is closer, it stops flush against it. |

- A box follows the Player's speed and locks in place when nobody pushes it.
- A destroyed box goes back to its start position.

### Pressure Plate and Gate
- Only a box opens the Gate. The Player standing on the Plate does nothing.
- A fully opened Gate will stay open.

### Moving Platform (Cloud)
- A light blue cloud with a flat round top, 4 x 4 m, so the Player and a box fit on it. It looks different from the white clouds under the level.
- Keeps moving along a route of points, then goes straight back to its start, again and again. It waits `waitTime` at every point.
- `startDelay` makes it wait at its start point before it moves for the first time. Platforms in a chain use it so each one waits next to the one before it, and the Player can jump from one to the next.
- Can carry the Player and the box. A wall in a box's way stops the box, and the platform goes on without it.

## Traps

### Spikes
A tray of spikes that acts as one trap: one collider box covers the whole spikes.
- Active: cones visible. A Player inside the trigger takes 1 damage, then the trap waits `hitCooldown` (4 s) before it can hurt again.
- Inactive: cones hidden, no damage.
- A hit blocked by the Player's invincibility does not start the cooldown. For now.
- The asset of the spikes is created by myself using probuilder.

| Prefab           | Toggle type   | Behaviour                                                                                                                                                                                |
| ---------------- | ------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `NormalSpikes`   | AlwaysActive  | Always visible, always hurts.                                                                                                                                                            |
| `PeriodicSpikes` | Periodic      | Shows and hides every `toggleInterval` (2 s).                                                                                                                                            |
| `HiddenSpikes`   | OnPlayerTouch | Starts hidden. When the Player steps in, the cones appear and the Player gets `touchGraceTime` to step out before it can hurt. Stays active until the Player respawns, then hides again. |


### Spinning Blade
- A long rod with a blade on each side, spinning in place
- Touching any part hurts the Player (1 damage) and pushes the Player away from the blade.

## UI
The HUD (scripts in `UISystem`):

| Element                  | Shows                                                                                                                                                                                    | Updated from                                                                                    |
| ------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| Hearts (top left)        | One heart per HP point; lost HP is faded                                                                                                                                                 | `PlayerEvents.HpChanged`                                                                        |
| Crystal panel (top left) | `collected / total` and one slot per Crystal, lit when collected                                                                                                                         | `ObstacleLevelEvents.CrystalCountChanged`                                                       |
| Minimap (top right)      | A round top-down view around the Player, north up, with an arrow for the Player's facing. Hearts and Crystals show as icons; a far Crystal stays on the map's edge to show its direction | A second camera that follows the Player and draws into a texture. Icons come from `MinimapIcon` |
| Dash ring (bottom left)  | Empties when the Player dashes and refills over the 2 s cooldown                                                                                                                         | `DashAbility.CooldownRemaining`                                                                 |

## Effects and Sky
Every effect is a particle (Cartoon FX Remaster Free) and a sound (Kenney Impact and Interface Sounds), set in the Inspector with `Effect`.

| When                                                               | Played by                                                     |
| ------------------------------------------------------------------ | ------------------------------------------------------------- |
| Player is hurt, dies, spawns, dashes (wind whoosh)                 | `LevelEffects` (listens to `PlayerEvents`)                    |
| Heart, Crystal, Star collected                                     | `LevelEffects` (listens to `LevelEvents.Collected`)           |
| Checkpoint activated                                               | `LevelEffects` (listens to `LevelEvents.CheckpointActivated`) |
| Breakable Wall breaks                                              | `BreakableWall`                                               |
| Boulder is pushed by a dash (rock sliding)                         | `DashableBox`                                                 |
| Crate is pushed and moving (box sliding, keeps playing)            | `MovableBox` with `LoopingSound`                              |
| A box comes back after it breaks                                   | `PushableBox`                                                 |
| A box gets on the Plate                                            | `PressurePlate` (sound only)                                  |
| Gate is moving (stone gate rumble, keeps playing until it is gone) | `Gate` with `LoopingSound`                                    |
| Gate is fully open                                                 | `Gate`                                                        |
| Hidden spikes pop up                                               | `Spike` (sound only)                                          |

Sounds for the crate, boulder, gate, crystal, wall break and dash are made by me (`Assets/Audio/ObstacleLevel`).
While paused, all sounds pause too.

Sky: Fantasy Skybox FREE, `FS000_Day_01`.

### When the Star appears
The Player has to know the Star appeared, wherever they are in the level:
- The sky changes from day to sunset (`FS003_Sunset`).
- A tall light beam stands on the Star.
- The Star shows on the Minimap and stays on its edge when far away, to point the way.
- The Crystal panel's hint changes to "The Star has appeared! Follow the light beam!"
- A firework and a sound play on the Star.

## Controls
Same as the Player (WASD, Space, Right Click dash, mouse camera), plus **ESC** to pause and resume.

## Planned
- Pause Menu and Level Complete Screen, shown from the Pause and Complete states.
- Assets for the level.
- TBD

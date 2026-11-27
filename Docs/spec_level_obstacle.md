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

| State | Behaviour | Links |
|-|-|-|
| **Start** | Asks `SpawnManager` to spawn the Player at the start Checkpoint. Counts the Crystals and hides the Star. | `StartFinished` → **Play** (next frame) |
| **Play** | Normal gameplay. Listens to `LevelEvents.Collected`. Counts each Crystal and shows the Star when the last one is collected. | ESC: `PauseRequested` → **Pause**<br>Star collected: `Completed` → **Complete** |
| **Pause** | `Time.timeScale = 0`, Player input off. Both are restored on exit. | ESC: `ResumeRequested` → **Play** |
| **Complete** | Player input off. | TBD |

## Spawn and Checkpoints
- `ObstacleLevelManager` holds the start Checkpoint and gives it to `SpawnManager` when the level starts.
- Only one Checkpoint is active at a time. Touching a Checkpoint makes it the active one and changes its color to green.
- When the Player dies and presses Space, the Player respawns at the active Checkpoint (green).

## Death Zone
- A trigger box under the level. When the Player falls into it, the Player dies.
- A box (Crate or Boulder) never reaches it: once a falling box is falling faster than 12 m/s (about 7 m down, deeper than any drop between floors), it breaks in the air where the Player still sees it, and goes back to its start position.

## Collectibles
| Type | Effect | How it appears |
|-|-|-|
| **Heart** | Player HP + 1 (handled by Player) | `CollectibleSpawnManager` spawns one at every child spot. On every Player spawn (level start and every respawn), leftover Hearts are removed and a full set is spawned again. |
| **Crystal** | Counts toward the Star. A collected Crystal stays collected after a respawn. | Placed by hand. There are 5 in the level. The Level Manager counts them when the level starts. |
| **Star** | Completes the level | Placed by hand on the Star island. Stays hidden until all Crystals are collected. |

The Crystal count for the HUD is sent through `ObstacleLevelEvents`, which holds the events only this level uses.

## Env Obj

### Breakable Wall
- Blocks the Player. A dash destroys it and bounces the Player back (the dash ends), so a dash into a wall at an edge doesn't carry the Player over it.

### Crate (Movable) and Boulder (Dashable)
One base class, `PushableBox`. The two types of box have a different way to move them.

| Prefab | Script | How to move it |
|-|-|-|
| `Crate` | `MovableBox` | Pushed by walking or dashing. |
| `Boulder` | `DashableBox` | Pushed by a dash only. It moves on the level's 2 m grid, counted from its start: one dash moves it to the furthest grid point within `moveDistance`, or flush against something closer. So it lines up with the 1-cell pits and drops into them as a bridge, and after stopping against a wall the next push lines it up again. Its collider is 1.9 m wide (5 cm margin on each side, so it never jams in a 2 m pit) and a full 2 m tall, so a bridge of boulders is level with the road and a Crate can be pushed across it. The dash ends and the Player bounces back, also when the Boulder is blocked. A Boulder that can't move the way it is pushed (stuck against a wall or in a corner) slides back toward the Player instead, so it can never be soft-locked. Sandstone colored, so it doesn't look like the grey decor rocks. |

- A box follows the Player's speed and locks in place when nobody pushes it.
- A box whose middle is not over anything (it only rests on an edge with a corner) slides off that edge and falls, so it never hangs in the air or blocks the way.
- A Crate that is stuck (on every side it either can't move, or there is no room for the Player to push from, like in a corner) goes back to its start after the Player pushes it for 1 s, so the puzzle can't be soft-locked.
- A destroyed box goes back to its start position. If something is there (another box, the Player), it waits out of sight until the start is free, so it never respawns inside something.
- A Crate passes through Spinning Blades, so a blade can't knock it off the path.

### Pressure Plate and Gate
- The Player or a box pressing the Plate's red pad opens the Gate. It must cover at least `pressThreshold` (50%) of the pad, seen from above, so touching the edge does nothing. The red pad is 1 m wide: a Crate opens it when about half on it, the Player has to stand almost fully on it (the Player covers at most about 56%).
- The Plate makes no sound itself; the Gate's rumble is the feedback.
- The Gate stays open only while the Plate is pressed, and closes again when it is released. It never disappears: opening, it sinks its whole height under the ground, and while it is fully down it is hidden from the minimap.
- So to walk through, the Player leaves a box on the Plate: the Player learns the Plate works by pressing, and has to keep it pressed.

### Moving Platform (Cloud)
- A light blue cloud with a flat round top, 4.4 m across, so the Player and a box fit on it. It looks different from the white clouds under the level. Its collider is that round top, so there are no invisible square corners.
- Keeps moving along a route of points, then goes straight back to its start, again and again. It waits `waitTime` at every point.
- `startDelay` makes it wait at its start point before it moves for the first time. Platforms in a chain use it so each one waits next to the one before it, and the Player can jump from one to the next.
- Can carry the Player and the box. A wall in a box's way stops the box, and the platform goes on without it.
- Path preview: light blue dots mark the whole route, with a bigger dot at every stop, so the Player can see where it goes.

## Traps

### Spikes
A tray of spikes that acts as one trap: one collider box covers the whole spikes.
- Active: cones visible. A Player inside the trigger takes 1 damage. The spike has no cooldown of its own: the Player's invincibility after a hit decides when it can hurt again.
- Inactive: cones hidden, no damage.
- The asset of the spikes is created by myself using probuilder.
- The tray floor is one flat collider (base up to the rim), so walking or landing on it is like walking on flat ground.

| Prefab | Toggle type | Behaviour |
|-|-|-|
| `NormalSpikes` | AlwaysActive | Always visible, always hurts. |
| `PeriodicSpikes` | Periodic | Shows and hides every `toggleInterval` (2 s). |
| `HiddenSpikes` | OnPlayerTouch | Starts hidden. When the Player or a Boulder steps in, a warning sound plays, only the tips of the cones show, and the Player gets `touchGraceTime` (0.8 s) to step out. Then the cones come out and hurt at once if the Player is still on it. Stays active until the Player respawns, then hides again. |


### Spinning Blade
- A long rod with a blade on each side, spinning in place
- Touching any part hurts the Player (1 damage), pushes the Player away from the blade and bumps them up, so they can recover in the air.

## UI
The HUD (scripts in `UISystem`):

| Element | Shows | Updated from |
|-|-|-|
| Hearts (top left) | One heart per HP point; lost HP is faded | `PlayerEvents.HpChanged` |
| Crystal panel (top left) | `collected / total` and one slot per Crystal, lit when collected | `ObstacleLevelEvents.CrystalCountChanged` |
| Minimap (top right) | A round radar around the Player, north up, with an arrow (black outline) for the Player's facing. It shows only floors (grass green) and walls (hedge green) on the sky's blue, with black rings and a crosshair over them; trees, rocks, clouds and other decoration are left out. Icons, each a small top-down picture of the object with a black outline: Crates, Boulders, Plates, Crystals, Checkpoints (grey, green when active), Gates and Breakable Walls (bars that turn like the object), Moving Platforms (white), Hearts and the Star. Sizes are a bit bigger than the real objects so they read clearly. A far Crystal or the Star stays on the edge to show its direction; a Crystal inside the radar sends out sonar rings | At the start it makes one flat shape per static box collider, far below the level where the main camera never looks; a second camera above them draws into a texture. Icons come from `MinimapIcon`. The round mask, rim and dash ring use a sharp 512 px circle sprite |
| Dash ring (bottom left) | Empties when the Player dashes and refills over the 2 s cooldown | `DashAbility.CooldownRemaining` |

## Effects and Sky
Every effect is a particle (Cartoon FX Remaster Free) and a sound (Kenney Impact and Interface Sounds), set in the Inspector with `Effect`.

| When | Played by |
|-|-|
| Player is hurt, dies, spawns, dashes (a burst of air left behind the Player, and a whoosh) | `LevelEffects` (listens to `PlayerEvents`) |
| Heart, Crystal, Star collected | `LevelEffects` (listens to `LevelEvents.Collected`) |
| Checkpoint activated | `LevelEffects` (listens to `LevelEvents.CheckpointActivated`) |
| Breakable Wall breaks | `BreakableWall` |
| Boulder is pushed by a dash (rock sliding) | `DashableBox` |
| Crate is pushed and moving (box sliding, keeps playing) | `MovableBox` with `LoopingSound` |
| A box breaks (falls off the level, or a stuck Crate) | `PushableBox` |
| A box comes back after it breaks | `PushableBox` |
| Gate is moving (stone gate rumble, keeps playing while it moves) | `Gate` with `LoopingSound` |
| Player steps on hidden spikes (warning) | `Spike` (sound only) |
| Hidden spikes pop up | `Spike` (sound only) |

Sounds for the crate, boulder, gate, crystal and wall break are made by me (`Assets/Audio/ObstacleLevel`). The hidden spike sounds and the dash whoosh are free sounds from Pixabay.
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

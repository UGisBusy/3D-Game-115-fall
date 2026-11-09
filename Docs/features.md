"Hero's Revenge" is an adventure game combining platforming, puzzle, and combat elements. You play as Hero, who is on his way of the revenge for the evil boss. Hero must conquer 3 level to face the boss.

DECISION:
- everything relate to UX is count as ART:
  - sound, VFX, sprite, ui image, ui bar ...
- barebone:
  - meaning: lowest requirement we should complete
  - spec of 3 level is barebone
  - not barebone:
    - save load
    - check point

NOTIFY CHANGE:
- candle mechanic
- combat level need to defeat all
- combat level wave: next Wave spawn after current Wave cleared (no more timed overlapping Waves)
- combat level respawn: clear Enemy and Collectible, restart current Wave
- combat level Collectible Spot: replace "Spawn Arrow Collectible every x second", spawn Arrow/Heart by weight, max 1 per Spot
- combat level Wave Info: remove enemy formation, add count and delay; Enemy spawn at random position in area


- UI and Game Flow
  - Main Menu
    - Play Button to enter Level Select Menu
    - Load Button to enter Save Load Menu
    - Exit Button to close Game
    
  - Level Select Menu
    - show 3 level
    - Start Button, Start From Checkpoint Button for each level
    - Back button to enter Main Menu
    - when enter Level Select Menu, load level progress of Save File
      - level progress contain 3 levels' checkpoint state 
      - depending on checkpoint state, disable Start From Checkpoint Button
      - use first save file by default
    - Start Button to start corresponding level at start point
    - Start From Checkpoint Button to start corresponding level at checkpoint
    
  - Pause Menu
    - Resume Button to resume game and close Pause Menu
    - Respawn Button to respawn Player at checkpoint
    - Exit To Level Select Button to enter Level Select Menu
    - Save And Exit To Level Select Button to save and enter Level Select Menu
    - Exit Game Button to close Game
    - Save And Exit Button to save and close Game
    
  - Save Load Menu
    - show 3 Save File info
    - Select Button, Delete Button for Save File
    - Select Button to select corresponding Save File
    - Delete Button to delete corresponding Save File
    - Back Button to enter Main Menu
  
  - Level Complete Screen
    - Press Enter to go back Level Select Screen
    - Button to write Level in Save File
    - Button to reset Level in Save File

  - Sequence Manager
    - Control high level gameflow
    - Main Menu State: show Main Menu, listening to Main Menu action
    - Level Select State: show Level Select Menu, listening to Level Menu action
    - Save Load State: show Save Load Menu, listening to Save Load Menu action
    - Game State: enter level, User play game
    - Exit State: cleanup, close Game

- Gameplay
  - Player:
    - Camera:
      - third person camera attach to Player
      - Mouse control Camera
      - ⚠️ prevent Camera clipping? 
        
    - Movement
      - Control:
        - W: rotate and move Camera's forward
        - A: rotate and move Camera's Left
        - S: rotate and move Camera's Back
        - D: rotate and move Camera's Right
        - Space: jump, can jump if player on top of Env Obj 
      - gravity affect Player
        
    - Dash
      - press right click, Player Dash Camera's forward
        - enter dash mode, set high velocity, disable control and gravity for 0.25 sec.
        - after 0.25 sec, activate control and gravity, exit dash mode
      - when in dash mode, Player invincible 
      - dash have 2 sec cooldown
        
    - Interaction
      - vs Env Obj
      - vs Enemy
      - vs Checkpoint
      - vs Collectible

    - Damage
      - Player has max x HP, when HP=0, Player die
      - when Player hurt, HP-x, if not die Player invincible for x sec
      - when Player die, press space to respawn Player
      - when Player knockback, knockback source push Player
    
    - Spawn
      - when Player spawn, Player invincible for x sec
      - reset Camera, Facing, Movement, Dash, HP
      - Player spawn at active Checkpoint (DUP)
    
  - All level
    - Level Manager ((each level have diff implementation))
      - Start State
        - Sequence Manager init Level Manager
        - Level Manager init Player, Enemy Spawn Manager, Collectible Spawn Manager
        - Level Manager enter Play State
      
      - Play State
        - spawn Player at Checkpoint
        - spawn Enemy with Enemy Spawn Manager 
        - press ESC, pause game and enter Pause State
      
      - Pause State
        - show Pause Menu
      
      - Complete State
        - show Level Complete Screen

      - Exit State
        - if save & exit, Save File
        - cleanup

    - Enemy Spawn Manager ((each level have diff implementation))
      - spawn Enemy
      - despawn Enemy
    
    - Collectible Spawn Manager ((each level have diff implementation))
      - spawn Collectible
      - despawn Collectible
    
    - Enemy
      - Player move into Enemy, Enemy knockback Player, Player hurt 
      - Player dash through Enemy, nothing happen
      - move pattern, implement by type
      - Damage
        - Enemy has 1 HP, when HP=0, Enemy die
        - when Enemy die, it disappear
    
    - Collectible
      - Player touch Collectible, Collectible trigger
      - when trigger, apply effect and disappear
      - Heart Collectible type:
        - effect: Player HP + 1
      - Star Collectible type:
        - effect: complete level
  
    - Checkpoint
      - Player touch Checkpoint, Checkpoint activate
      - only one Checkpoint activate at Level
      - Player spawn at active Checkpoint (DUP)

    - Env Obj
      - Independent
        - Obj Move on its own, Player cannot interfere
        - Block Player
        - Type: Moving Obj
          - move pattern
          - Obj move into Player, Obj push Player
        - Type: Gate
          - in shut state, Obj Block Player
          - in open state, Obj is invincible

        - Player move into Independent Env Obj, block Player
        - Player dash into Independent Env Obj, block Player
    
      - Dashable
        - Player Dash into special side trigger effect 
        - Type: Box
          - effect: move predefine distance
        - Type: Breakable Wall
          - effect: break

        - Player move into Dashable Obj, Obj block Player
        - Player dash into Dashable Obj, Obj trigger effect

      - Movable
        - have weight
        - interact with Player Move

        - Player move into Movable Obj, Player push obj
        - Player dash into Movable Obj, Player push obj


  - Obstacle level:
    - level gameplay design
      - Scene, Env Obj, Collectible, everything setup
      - Level Manager
      - Star Collectible place at destination
    
    - platform
      - walkable platform
      - if Player drop, Player die
    
    - Traps:
      - Spike
        - if active and Player touch Spike, Player hurt
        - if not active and Player touch Spike, nothing happen
        - toggle Type:
          - toggle active periodically
          - toggle after Player touch it
          - always active
      
      - Spinning Blade
        - Independent Env Obj
        - long rod with blade on each side
        - spin stationary
        - if Player touch it, Player hurt, push Player 

  - Puzzle level:
    - level gameplay design
      - Scene, Env Obj, Collectible, everything setup
      - Level Manager
    
    - dark room
    - light source light up area
    - light source type:
      - Type: env light
      - Type: refuel spot      
      - Type: Candle
        - attach to Player
        - have x fuel
        - if have fuel, light up area
        - if no fuel, not light up area
        - if Player touch env light refuel spot, refuel Candle
        - if Player Dash, fuel - 1
    - if Player in dark, Player hurt every x second
    
    - Note count
    - Note Collectible Type:
      - effect: Note count + 1
    - if Note count = 5, spawn Star Collectible

  - Combat level:
    - level gameplay design
      - Scene, Env Obj, Collectible, everything setup
      - Level Manager

    - Bow and Arrow
      - Player have max x Arrow
      - Collectible Type: Arrow Collectible
        - effect: Player Arrow +1
      - Arrow Collectible spawn at Collectible Spot
      - if Player have Arrow, Player can Charge and Fire Bow
      
      - When hold down Left Click:
        - Bow charge
        - Player face camera front horizontally, cannot jump, move slow
      - When not hold down Left Click:
        - Bow charge decrease
        - Player movement recover
      - When Bow charge fully and release Left Click
        - Arrow fire with Player face direction
        - Arrow fly horizontally, gravity has no effect on Arrow
        - Arrow - 1

      - When Arrow touch Independent Env Obj, Arrow break
      - When Arrow touch Dashable Env Obj, Arrow break
      - When Arrow touch Movable Env Obj, Arrow break, Arrow push Obj
      - When Arrow touch Enemy, Arrow Break, hurt Enemy 

      - When Arrow Break, Arrow disappear
      
    - Collectible Spot
      - place in level, spawn Collectible on its own
      - hold max 1 Collectible at a time
      - when Spot empty (start of level, picked up, or cleared), wait x second, then spawn 1 Collectible
      - Collectible type is picked randomly by weight
        - e.g. Arrow 3, Heart 1 => 75% Arrow, 25% Heart
        - new type = add one more entry to the list
      - when Player spawn, clear Collectible on Spot and restart timer


    - Enemy
      - more Enemy type:
        - Heavy Enemy Type   : higher life, move slow
        - Light Enemy Type   : lower life, move fast
        - Charge Enemy Type  : stand still and charge periodically
        - Jump Enemy Type    : stand still and jump periodically
        - Shield Enemy Type  : have a Dashable Obj Shield

    - Enemy Wave
      - x total Waves
      - only one Wave active at a time
      - next Wave spawn x second after current Wave cleared (no Enemy left, no group waiting to spawn)
      - load Wave Info in Waves File

      - Wave Info:
        - enemy groups
          - enemy type
          - spawn area
          - count
          - delay after Wave start
      - Enemy spawn at random position in spawn area, not overlapping
      
      - When last Wave cleared, Star Collectible spawn

    - Player Respawn
      - when Player respawn:
        - clear all Enemy and Collectible
        - spawn Player at center
        - restart current Wave after x second


- Art TODO
  - All level:
  - Obstacle level:
  - Puzzle level:
  - Combat level:

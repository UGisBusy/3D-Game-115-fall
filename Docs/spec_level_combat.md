# Combat Level Spec

## Level Manager (level logic)
- Read enemy wave scriptable object, call `EnemySpawn` to spawn enemies
- [TODO] Spawn star when all level clear
- [TODO] Retry / player died features
- [TODO] Checkpoint features
- Pause and continue level
- [TODO] Exit and dave level
- [TODO] Start from savefile features


## Enemy
- Light
  - Move Faster
- Heavy
  - Move Slower
  - High Hp, speed up when damaged
  - Slam when player get closed
- Charge
  - Charge forward long distance
  - Strong knock back while charging
- Jump
  - Jump and land on predicted location
- Shield
  - Has a shield blocking arrows
  - Break the shield by dash into it, bounces player backward
  - Stunned for a while after the shield breaks, then runs faster


## Enemy Waves
- 6 waves
- Introduce all types in early waves (1~3)
- More and combination of multiples enemy at later waves (4~6)
- [TODO] Restart the wave which player died to


## Enemy Spawns
- 4 Enemy Spawns: N,S,E,W
- [TODO] Telegraph when about to spawn enemies


## Collectible Spots
- 4 spots (NE, NW, SE, SW) that spawn a collectibles helping player
- They can hold one collectible at a time, and regenerate another when empty
- Collectible spawns:
  - Fireball Charge: refill 2 charge of fireball
  - [TODO] Heart
- [TODO] Clear after player respawn


## Fireball Ability
- Hold 1 second to charge with a magic circle telegraph
- Shoot fireball when release at full charge, decrease while not charging
- Limited 5 charges
- [TODO] Start and respawn set charges to 5
- [TODO?] Rename in code (they still call bow and arrow)


## TODO - Combat Level
- UI:
  - HUD:
    - Minimap
    - Hp
    - Fireball charge
    - Timer?
  - Screen:
    - Pause screen (Global)
    - Complete screen?
- Visual:
  - Ground, pillars, walls
- Visual Effect:
  - Enemy hurt?
  - Enemy defeat?
  - Better enemy attack telegraph?
- Sound Effect

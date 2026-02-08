DeepSeaSoftReset
================

Description
-----------
DeepSeaSoftReset is an Oxide plugin for Rust that performs a *soft reset* of
Deep Sea entities during an active Deep Sea event.

Instead of forcing a full Deep Sea wipe, this plugin intelligently respawns
destroyed loot containers and NPCs when players are not nearby, keeping the
event active and populated without breaking immersion or performance.

It also allows full server-side control of Deep Sea convars directly through
the plugin configuration.

Features
--------
- Tracks destroyed Deep Sea loot containers and NPCs
- Automatically respawns entities when players are far enough away
- Optional respawn of hackable crates on random ghost ships
- Admin chat command to force a soft reset
- Periodic background checker coroutine
- Automatically applies Deep Sea convars from config on startup
- Clears tracked entities when Deep Sea opens or closes
- Designed to be lightweight and non-intrusive

Chat Commands
-------------
/deepseasoftreset

- Admin only
- Immediately runs a soft reset pass on all tracked entities

How It Works
------------
- When a Deep Sea storage container or Scientist NPC is killed, it is recorded
  with its prefab, position, rotation, and parent entity.
- A background coroutine periodically checks if these entities can be safely
  respawned.
- Entities will only respawn if no players are within the configured minimum
  distance.
- Hackable crates can optionally respawn by spawning a new crate on a random
  ghost ship instead of recreating the original entity.
- Tracked entities are cleared automatically when Deep Sea opens or closes.

Configuration
-------------
The configuration file is generated at:
oxide/config/DeepSeaSoftReset.json

Config Options:

- Check For Loot/Npc Respawn Ever (Mins)
  How often the plugin checks for respawn opportunities.

- Min Distance From Player To Allow Respawn
  Minimum distance (in meters) a player must be away before an entity respawns.

- Respawn Hackable Create On Random Ghostship
  If true, hackable crates respawn on random ghost ships instead of their
  original position.

- Number Of Hackable Crates To Spawn
  Number of hackable crates allowed to exist.

- Block Building In Deep Sea
  Enables or disables building in the Deep Sea.

- Deep Sea Portal Edge
  0 = Map-based
  1 = North
  2 = East
  3 = South
  4 = West

- Floating City Count
- Deep Sea Island Count
- Deep Sea Ghostship Count
- Deep Sea Rhib Count

- Deep Sea Wipe Duration
  Duration (seconds) of the Deep Sea event.

- Deep Sea Wipe Cooldown
  Cooldown (seconds) before Deep Sea re-opens.

- Duration In Seconds Of The Final Wipe Phase

- Seconds Before Radiation Starts To Ramp Up Before Deep Sea Wipe

- Should The Deep Sea Map Be Covered By Fog Of War

All Deep Sea related convars are automatically applied on server startup.

Requirements
------------
- Rust Dedicated Server
- Oxide/uMod installed
- Deep Sea content enabled on the server

Installation
------------
1. Place `DeepSeaSoftReset.cs` into:
   oxide/plugins/

2. Reload or restart the server:
   oxide.reload DeepSeaSoftReset

3. Configure settings in:
   oxide/config/DeepSeaSoftReset.json

Notes
-----
- This plugin is designed to complement the Deep Sea event, not replace it.
- Respawns are intentionally delayed and distance-checked to avoid abuse.
- Safe to run on live servers.
- Uses coroutines and pooling to minimize performance impact.

License
-------
Free to use and modify.
Do not resell without permission.


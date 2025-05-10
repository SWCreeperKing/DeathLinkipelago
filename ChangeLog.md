# v.0.13.0

- [Yaml] Added `send_scout_hints` yaml option
- [Client] Added a pie chart for death counts
- [Client] Added a line chart for last time since death and who caused it to reset
- [Client] Added a priority system to the death shop
- [Client] Added a buy command `buy [item #]` i.e. `@[slot name] buy 7`
- [Client] Added an inventory check command `inv [shield/coin]` i.e. `@[slot name] inv coin`
- [Client] Scout hints only send if the items are visible in the shop and `send_scout_hints` is enabled
- [Client] Changed compatability version from v.0.12 to v.0.13.0
- [APWorld] Changed compatability version from v.0.12 to v.0.13.0
- [APWorld] Fixed (actually this time) a problem with low checks and goal being possible immediately
  - As a consequence a few things have changed:
    - There is now a starting check
    - Goal requires `Death Grass` item to goal

---
# v.0.12.1hf

- [Client] Stopped controller inputs
- [Client] Now correctly sends out shop hints for progressive items in shop

---
# v.0.12

- [Client] Full remake in Godot from Raylib.cs + DearImgui
- [Client] Added life shop
- [Client] Added life coins
- [Client] Fixed crashing other games by misfiling the deathlink packet
- [Client] Added version compatibility
- [Client] Progressive items are now scout hinted
- [APWorld] Fixed (hopefully) some goal logic
- [APWorld] Added version compatability
- [Yaml] Added `seconds_per_life_coin` yaml setting
- [Yaml] Added `use_global_counter` yaml setting

---
# v.0.11.3hf

- [Client] Fixed a bug preventing login
- [Client] Fixed not receiving a death link
- [Client] Fixed hints not showing
- [Client] Added Status to Player List
- [Client] Fixed Player List not displaying
- [Client] Fixed commands being strange

---
# v.0.11.2hf

- [APWorld] Removed a missed print line from testing XD (oopps) (no other change (meaning the client doesn't need it))
- [Client] Removed a log line that made the console less readable
- [Client] Moved most AP Backend to a separate project (to make this one smaller)
- [Client] Made the client more resilient to a few specific crashes 
- [Client] Fixed a Small Object Heap leak

---
# v.0.11.1hf

- [Client] Fixed hints with locations causing a crash
- [Client] Added a horizontal scrollbars to a few tabs
- [Client] Very slightly increased performance when rendering hints and item logs

---
# v.0.11

- [Client] Added copy button to hints
- [Client] Added entrance to hint tab 
- [Client] Added player list tab
- [Client] Added a credits tab
- [Client] Removed the text input in the item log
- [Client] Added icon made by Raphael2512
- [Client] Fixed crash with the hint tab

---
#  v.0.10

- [Client] Fixed bug with hinted items and items not appearing in shop
- [Client] Added hints to the text client tab (PAIN AAAAAAAA)
- [Client] Added an item log that details who sent who what 
- [Client] Various small UI changes

---
# v.0.9.1hf

- [Client] Hints tab now displays location of hint

---
# v.0.9

- [Client] Time since last death starts counting after the first death
- [Client] Added a text client
- [Client] Added a hint tab
- [Client] Added a few more commands
- [Client] Hinted items are blue and moved to the top

---
# v.0.8

- [Client] Added shop level (how many `Progressive Death Shops` you have)
- [Client] Added a console use the `help` command for a list of commands
- [Client] Fixed counting death from `Death Trap`s twice
- [Client] Fixed possibly major location breaking bug?
- [Client] Hides buy buttons when you don't have any coins 

---
# v.0.7.2hf

- [Client] Fixed not receiving items correctly
- [Client] Removed `Gain Coin` debug button

---
# v.0.7.1hf

- [Client] Fixed rendering death table twice (oops)

---
# v.0.7

- [Yaml] Added `send_traps_after_goal` yaml setting
- [Client] Removed some extra padding in the death table
- [Client] Fixed cases where games (*cough cough* ror2 *cough cough*) use neither the slot name or a slot name's alias when sending a death link
- [Client] Set target FPS to 30 instead of 60
- [Client] Will send death links from traps if goaled if `send_traps_after_goal` is true

---
# v.0.6

- [APWorld] Renamed `DeathLinkipelago Death Shop` to `Death Shop`
- [Client] Fixed not receiving a `Death Coin` when sending a death link
- [Client] Added a time since last death and a longest time since last death (neither are saved)
- [Client] Tattletales on the last person that sent a death link
- [Client] No longer sends death links from traps if goaled
- [Client] Added colors for items

---
# v.0.5.2hf

- [APWorld] ACTUALLY Fixed (like frfr this time) location access rules
- [Client] Disguised traps a tiny amount

---
# v.0.5.1hf

- [APWorld] Fixed (hopefully) location access rules

---
# v.0.5

- [Yaml] Added `death_trap_percent` yaml setting
- [Yaml] Added `progressive_items_per_shop` yaml setting
- [APWorld] Rewrote into a shop style progression
- [Client] Small UI changes
- [Client] Fixed a bug where restarting the client would give you 2x the received items
- [Client] Rewrote into a shop style progression

---
# v.0.4

- [Yaml] Added `has_funny_button` yaml setting
- [APWorld] Added `allow_funny_button` host setting
- [APWorld] Added `extend_death_limit` host setting
- [APWorld] Allowed for max checks to be 999 if host setting is enabled
- [Client] Slot Aliases no longer have their own death count
- [Client] Limited the `Send Death` button to yaml settings

---
# v.0.3

- [Client] Fixed not correctly receiving items

---
# v.0.2
---
- [Client] Fixed `Death Traps` not being correctly counted when used
- [Client] Reduced the timer for sending death links from `Death Traps` from 10s to 3s 
- [Client] Made data save to the Slot not the Multiworld

---
# v.0.1

- [Client] Initial Release
- [APWorld] Initial Release
- [Yaml] Initial Release
# v.0.7.2hf

---
- [Client] Fixed not receiving items correctly
- [Client] Removed `Gain Coin` debug button


# v.0.7.1hf

---
- [Client] Fixed rendering death table twice (oops)

# v.0.7

---
- [Yaml] Added `send_traps_after_goal` yaml setting
- [Client] Removed some extra padding in the death table
- [Client] Fixed cases where games (*cough cough* ror2 *cough cough*) use neither the slot name or a slot name's alias when sending a death link
- [Client] Set target FPS to 30 instead of 60
- [Client] Will send death links from traps if goaled if `send_traps_after_goal` is true

# v.0.6

---
- [APWorld] Renamed `DeathLinkipelago Death Shop` to `Death Shop`
- [Client] Fixed not receiving a `Death Coin` when sending a death link
- [Client] Added a time since last death and a longest time since last death (neither are saved)
- [Client] Tattletales on the last person that sent a death link
- [Client] No longer sends death links from traps if goaled
- [Client] Added colors for items

# v.0.5.2hf

---
- [APWorld] ACTUALLY Fixed (like frfr this time) location access rules
- [Client] Disguised traps a tiny amount

# v.0.5.1hf

---
- [APWorld] Fixed (hopefully) location access rules

# v.0.5

---
- [Yaml] Added `death_trap_percent` yaml setting
- [Yaml] Added `progressive_items_per_shop` yaml setting
- [APWorld] Rewrote into a shop style progression
- [Client] Small UI changes
- [Client] Fixed a bug where restarting the client would give you 2x the received items
- [Client] Rewrote into a shop style progression

# v.0.4

---
- [Yaml] Added `has_funny_button` yaml setting
- [APWorld] Added `allow_funny_button` host setting
- [APWorld] Added `extend_death_limit` host setting
- [APWorld] Allowed for max checks to be 999 if host setting is enabled
- [Client] Slot Aliases no longer have their own death count
- [Client] Limited the `Send Death` button to yaml settings

# v.0.3

---
- [Client] Fixed not correctly receiving items

# v.0.2

---
- [Client] Fixed `Death Traps` not being correctly counted when used
- [Client] Reduced the timer for sending death links from `Death Traps` from 10s to 3s 
- [Client] Made data save to the Slot not the Multiworld

# v.0.1

---
- [Client] Initial Release
- [APWorld] Initial Release
- [Yaml] Initial Release
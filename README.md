# Archipelago XIV

This repo contains two separate, yet intertwined projects.

`src` contains an APWorld for Final Fantasy 14, built on top of Manual.
`ArchipelagoXIV` contains a Dalamud plugin that is a tracker/client for PC players

Both of them probably need more love.

## Summary

Checks: Duties and overworld activities
Items: Zone access and Progressive Level Caps

What you need: A character that's up to date on the MSQ, and has everything unlocked.


## Locations

Note: Almost everything listed below can be disabled with yaml options, have a read of the template yaml for customization details

* Fates
  * With fatesanity enabled, each named fate is a unique check.  Otherwise, do up to 5 fates per zone.
* Dungeons
* Trials
* Normal Raids
* Alliance Raids
* Guildhests
* Ocean Fishing Routes (bihourly fishing raids)
* Bozja ARs (CLL, DR, DAL)
* Fishsanity: Catch every fish
* Returning to the Waking Sands

## Items

* Progressive Level caps
  * 5 Levels per bundle, separated by class.  If you don't have everything at max, I suggest setting `force_classes` with the classes you have/can get to cap within the duration of your run.
* Zone Access
  * You cannot pass through zones you do not have access to. So Outer La Noscea is useless without Upper La Noscea, and so on.
  * You always have access to the three starting cities, and can use boats, airships, and zone edges to traverse.
* Raid Unlocks
  * Because hitting a given expansion's postgame unlocks so much to do at once, this attempts to slow down how many checks unlock at once.
* Fishsanity: Permittted Baits
* "Memories of a Distant Land"
  * Collect these to finish the game.

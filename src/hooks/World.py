from collections import defaultdict
import json
import math
import os
import pkgutil
from typing import Any

import Utils
from BaseClasses import CollectionState, Item, ItemClassification, LocationProgressType, MultiWorld, Entrance
from Options import OptionError
from worlds.AutoWorld import World
from worlds.generic.Rules import add_rule

# Raw JSON data from the Manual apworld, respectively:
#          data/game.json, data/items.json, data/locations.json, data/regions.json
#
from ..Data import item_table, location_table

# These helper methods allow you to determine if an option has been set, or what its value is, for any player in the multiworld
from ..Helpers import get_option_value, is_option_enabled

# Object classes from Manual -- extending AP core -- representing items and locations that are used in generation
from ..Items import ManualItem, item_name_to_item
from ..Locations import victory_names, location_name_to_location
from .Data import BOSS_GOAL_KEY_LOCATIONS, CASTER, DOH, HEALERS, MELEE, RANGED, TANKS, WORLD_BOSSES, categorizedLocationNames, bait_to_fish, FILLER_EMOTES
from .Helpers import get_int_value, is_fishing_enabled
from .Options import LevelCap

########################################################################################
## Order of method calls when the world generates:
##    1. create_regions - Creates regions and locations
##    2. create_items - Creates the item pool
##    3. set_rules - Creates rules for accessing regions and locations
##    4. generate_basic - Runs any post item pool options, like place item/category
##    5. pre_fill - Creates the victory location
##
## The create_item method is used by plando and start_inventory settings to create an item from an item name.
## The fill_slot_data method will be used to send data to the Manual client for later use, like deathlink.
########################################################################################


def get_duty_count(duty_type: str, duty_diff: int, multiworld: MultiWorld, player: int) -> int | None:
    if duty_type == "Dungeon":
        return get_int_value(multiworld, player, "dungeon_count")
    if duty_type == "Variant Dungeon":
        return get_int_value(multiworld, player, "variant_dungeon_count")
    if duty_type == "Trial":
        if duty_diff == 1:  # Normal
            return get_int_value(multiworld, player, "trial_count")
        if duty_diff == 2:  # Extreme
            return get_int_value(multiworld, player, "extreme_trial_count")
        if duty_diff == 4:  # Endgame
            return get_int_value(multiworld, player, "endgame_trial_count")
    if duty_type in ["Raid", "Normal Raid", "Savage Raid", "Endgame Raid"]:
        if duty_diff == 1:  # Normal
            return get_int_value(multiworld, player, "normal_raid_count")
        if duty_diff == 3:  # Savage
            return get_int_value(multiworld, player, "savage_raid_count")
        if duty_diff == 4:  # Endgame
            return get_int_value(multiworld, player, "endgame_raid_count")
    if duty_type == "Alliance Raid":
        return get_int_value(multiworld, player, "alliance_raid_count")
    if duty_type == "Ultimate":
        return get_int_value(multiworld, player, "ultimate_count")
    if duty_type == "Guildhest":
        return None
    if duty_type == "PvP":
        return None
    if duty_type == "Field Operation":
        return None
    raise ValueError(f"Unknown duty type {duty_type}")

# Use this function to change the valid filler items to be created to replace item links or starting items.
# Default value is the `filler_item_name` from game.json
def hook_get_filler_item_name(world: World, multiworld: MultiWorld, player: int) -> str | bool:
    return world.random.choice(FILLER_EMOTES)

def before_generate_early(world: World, multiworld: MultiWorld, player: int) -> None:
    """
    This is the earliest hook called during generation, before anything else is done.
    Use it to check or modify incompatible options, or to set up variables for later use.
    """

    goal = victory_names[get_option_value(multiworld, player, 'goal')]  # type: ignore
    goal_location = next(loc for loc in location_table if loc.get('victory') and loc['name'] == goal)
    level_cap = get_option_value(multiworld, player, 'level_cap')
    goal_level = goal_location.get('level', 0)
    if not get_option_value(multiworld, player,"include_dungeons"):
        world.options.dungeon_count.value = 0

    if hasattr(multiworld, "re_gen_passthrough"):
        slot_data = multiworld.re_gen_passthrough.get(world.game, {})
        world.mcguffins_needed = slot_data['mcguffins_needed']
    else:
        world.mcguffins_needed = get_option_value(multiworld, player, "mcguffins_needed")

    if goal_level and goal_level > level_cap:
        raise OptionError(f"The selected goal '{goal}' requires level {goal_location.get('level')}, which exceeds the level cap of {level_cap}.")

    has_fates = get_option_value(multiworld, player, 'fatesanity') or get_int_value(multiworld, player, 'fates_per_zone') > 0
    has_duties = get_int_value(multiworld, player, 'max_party_size') > 0 and get_int_value(multiworld, player, 'duty_difficulty') > 0
    has_dungeons = get_int_value(multiworld, player, 'dungeon_count') > 0 and has_duties
    has_fish = is_option_enabled(multiworld, player, 'fishsanity')

    if not has_fates and not has_dungeons and not has_fish:
        raise OptionError("You can't disable everything.")

    if not has_dungeons and not has_fish and not get_option_value(multiworld, player, 'fatesanity') and get_int_value(multiworld, player, 'fates_per_zone') < 3:
        world.options.fates_per_zone.value = 3


# Called before regions and locations are created. Not clear why you'd want this, but it's here. Victory location is included, but Victory event is not placed yet.
def before_create_regions(world: World, multiworld: MultiWorld, player: int):
    world.skipped_duties: set[str] = set()

    if not getattr(multiworld, 'generation_is_fake', False):
        for category, names in categorizedLocationNames.items():
            dutyType, _dutyExpansion, dutyDifficulty = category
            count = get_duty_count(dutyType, dutyDifficulty, multiworld, player)

            if count is None:
                continue

            count = min(len(names), count)
            used_names = world.random.sample(names, count)

            goal_name = victory_names[get_option_value(multiworld, player, "goal")]
            base_name = BOSS_GOAL_KEY_LOCATIONS.get(goal_name)

            # Force the goal's required trial into the location pool if it belongs to this category
            if dutyType == "Trial" and base_name:
                goal_trial = next((n for n in names if n == f"The {base_name}" or n == base_name), None)

                if goal_trial and goal_trial not in used_names:
                    used_names.append(goal_trial)

            for name in names:
                if name not in used_names:
                    world.skipped_duties.add(name)

    tanks = TANKS.copy()
    healers = HEALERS.copy()
    melee = MELEE.copy()
    caster = CASTER.copy()
    ranged = RANGED.copy()
    doh = DOH.copy()

    world.random.shuffle(tanks)
    world.random.shuffle(healers)
    world.random.shuffle(melee)
    world.random.shuffle(caster)
    world.random.shuffle(ranged)
    world.random.shuffle(doh)
    force_jobs = sorted(get_option_value(multiworld, player, "force_jobs"))
    if force_jobs:
        if len(force_jobs) > 5:
            world.random.shuffle(force_jobs)
            force_jobs = force_jobs[:5]
        prog_classes = force_jobs
    else:
        prog_classes = [tanks[0], healers[0], melee[0], caster[0], ranged[0]]
    world.prog_classes = prog_classes
    world.prog_levels = [f"5 {job} Levels" for job in world.prog_classes]
    world.prog_doh = doh[0]

# Called after regions and locations are created, in case you want to see or modify that information. Victory location is included.
def after_create_regions(world: World, multiworld: MultiWorld, player: int):
    locationNamesToRemove = []
    locationNamesToExclude = []
    empty_regions = []
    if not is_option_enabled(multiworld, player, "include_unreasonable_fates"):
        locationNamesToRemove.extend(WORLD_BOSSES)

    level_cap = get_option_value(multiworld, player, "level_cap") or LevelCap.range_end

    if not is_option_enabled(multiworld, player, "allow_main_scenario_duties"):
        goal_name = victory_names[get_option_value(multiworld, player, "goal")]
        goal_base_name = BOSS_GOAL_KEY_LOCATIONS.get(goal_name)
        locations_to_remove = ["Castrum Meridianum", "The Praetorium"]

        if goal_base_name != "Porta Decumana":
            locations_to_remove.append("The Porta Decumana")

        locationNamesToRemove.extend(locations_to_remove)


    # Find all region access items.
    access_items = {item['name']: item for item in item_table if item['name'].endswith(" Access")}
    regions = {multiworld.get_region("Manual", player)}
    checked_regions = set()
    distance = 0

    while regions:
        next_regions = set()
        for region in regions:
            if not getattr(region, "distance", None):
                region.distance = distance
            next_regions.update({e.connected_region for e in region.exits if e.connected_region not in checked_regions})
            for location in region.locations:
                location.level = int(location_name_to_location[location.name].get("level", 0))
        checked_regions.update(regions)
        regions = next_regions
        distance += 1

    for region in checked_regions:
        # Get the item required to access the region to determine the level requirement of the region.
        access_item_name = region.name + " Access"
        access_item = access_items.get(access_item_name)
        # If there is an item for this region and the level requirement is above the level cap, remove all the locations
        # in the region.
        if access_item is not None and access_item.get("level", 0) > level_cap:
            # print(f"Removing all locations in region {region.name} from {player}'s world")
            for location in list(region.locations):
                # print(f"  Removing {location.name}")
                region.locations.remove(location)
        # Remove/exclude locations in `locationNamesToRemove`/`locationNamesToExclude`.
        else:
            for location in list(region.locations):
                if location.name in locationNamesToRemove:
                    # print(f"Removing {location.name} from {player}'s pool")
                    region.locations.remove(location)
                elif location.name in locationNamesToExclude:
                    location.progress_type = LocationProgressType.EXCLUDED
        if not region.locations:
            empty_regions.append(region)
    empty_regions = sorted(empty_regions, key=lambda r: r.distance, reverse=True)
    culled_regions = set()
    culled_access_items = set()
    for region in empty_regions:
        connected = {e.connected_region for e in region.exits if e.connected_region.distance > region.distance}
        cullable = all(c in culled_regions for c in connected)
        if cullable:
            # print(f'Culling unused {region.name}')
            access_item_name = region.name + " Access"
            culled_access_items.add(access_item_name)
            culled_regions.add(region)

        pass

    if hasattr(multiworld, "clear_location_cache"):
        multiworld.clear_location_cache()
    world.culled_access_items = culled_access_items
    world.culled_regions = culled_regions

# This hook allows you to access the item names & counts before the items are created. Use this to increase/decrease the amount of a specific item in the pool
# Valid item_config key/values:
# {"Item Name": 5} <- This will create qty 5 items using all the default settings
# {"Item Name": {"useful": 7}} <- This will create qty 7 items and force them to be classified as useful
# {"Item Name": {"progression": 2, "useful": 1}} <- This will create 3 items, with 2 classified as progression and 1 as useful
# {"Item Name": {0b0110: 5}} <- If you know the special flag for the item classes, you can also define non-standard options. This setup
#       will create 5 items that are the "useful trap" class
# {"Item Name": {ItemClassification.useful: 5}} <- You can also use the classification directly
def before_create_items_all(item_config: dict[str, int|dict], world: World, multiworld: MultiWorld, player: int) -> dict[str, int|dict]:
    # Remove all auto generated key items so we can add only the variant we need (key vs key piece)
    for goal_base_area_name in BOSS_GOAL_KEY_LOCATIONS.values():
        for variant in (f"{goal_base_area_name} Key", f"{goal_base_area_name} Key Piece"):
            if variant in item_config:
                item_config[variant] = 0

    goal_name = victory_names[get_option_value(multiworld, player, "goal")]
    goal_base_area_name = BOSS_GOAL_KEY_LOCATIONS.get(goal_name)
    boss_key_pieces = get_int_value(multiworld, player, "boss_key_pieces")

    if boss_key_pieces > 0:
        if goal_base_area_name:
            key_item = f"{goal_base_area_name} Key" if boss_key_pieces == 1 else f"{goal_base_area_name} Key Piece"
            item_config[key_item] = boss_key_pieces
            world._boss_key_item = key_item
            world._boss_key_pieces = boss_key_pieces
        else:
            world._boss_key_item = ""
            world._boss_key_pieces = 0
    else:
        world._boss_key_item = ""
        world._boss_key_pieces = 0

    for name in world.culled_access_items:
        if name in item_config:
            item_config[name] = 0

    # Remove the goal trial cleared item from random generation
    if goal_base_area_name:
        cleared_name = f"{goal_base_area_name} Cleared"
        if cleared_name in item_config:
            item_config[cleared_name] = 0

    all_location_names = {l.name for l in world.get_locations()}
    for bait, fish in bait_to_fish.items():
        if not fish & all_location_names:
            item_config[bait] = 0

    item_count = sum(item_config.values())
    location_count = len(world.get_locations())

    # If there's a boss goal, its trial location will host a "cleared" event
    world._goal_trial = None
    if goal_base_area_name:
        for (duty_type, _, _), names in categorizedLocationNames.items():
            if duty_type == "Trial":
                goal_trial = next((n for n in names if n == f"The {goal_base_area_name}" or n == goal_base_area_name), None)

                if goal_trial and goal_trial in all_location_names:
                    world._goal_trial = goal_trial
                    location_count -= 1
                    break

    level_cap = get_int_value(multiworld, player, "level_cap") or LevelCap.range_end
    actual_level_cap = max([int(location_name_to_location[l.name].get("level", 0)) for l in world.get_locations()])
    capped_count = math.ceil(min(level_cap, actual_level_cap) / 5)
    prog_levels = world.prog_levels

    remaining = location_count - item_count - capped_count

    if goal_base_area_name:
        # Boss goal: no McGuffins needed
        item_config['Memory of a Distant World'] = 0
        world.mcguffins_needed = 0
    else:
        item_config['Memory of a Distant World'] = min(remaining // 4, 50)
        world.mcguffins_needed = int(item_config['Memory of a Distant World'] * (get_int_value(multiworld, player, "mcguffins_needed") / 100))
    item_count += item_config['Memory of a Distant World']

    if is_fishing_enabled(multiworld, player):
        prog_levels = ['5 FSH Levels'] + prog_levels

    remaining = location_count - item_count
    if remaining < 50:
        prog_levels = prog_levels[:2]

    if remaining < 100:
        prog_levels = prog_levels[:3]

    for name in prog_levels:
        remaining = location_count - item_count
        if remaining < capped_count:
            break
        item_config[name] = {"progression": capped_count}
        item_count += capped_count

    remaining = location_count - item_count
    if remaining > 0:
        filler_levels = [f"5 {job} Levels" for job in TANKS + HEALERS + MELEE + CASTER + RANGED + DOH]
        world.random.shuffle(filler_levels)
        for name in filler_levels:
            item_config[name] = min(remaining, capped_count)
            item_count += item_config[name]
            remaining = location_count - item_count
            if remaining < capped_count:
                break

    return item_config

# The item pool before starting items are processed, in case you want to see the raw item pool at that stage
def before_create_items_starting(item_pool: list, world: World, multiworld: MultiWorld, player: int) -> list:
    return item_pool


# The item pool after starting items are processed but before filler is added, in case you want to see the raw item pool at that stage
def before_create_items_filler(
    item_pool: list[ManualItem], world: World, multiworld: MultiWorld, player: int
) -> list:
    prog_levels = world.prog_levels
    start_class = world.random.choice(prog_levels)
    prog_doh = f"5 {world.prog_doh} Levels"
    level_cap = get_option_value(multiworld, player, "level_cap") or LevelCap.range_end

    seen_levels = {}
    locations_per_depth = defaultdict(list)
    locations_per_level = defaultdict(list)
    cummulative_locations_per_depth = {}
    for location in world.get_locations():
        depth = (location.level // 5) + location.parent_region.distance
        locations_per_depth[depth].append(location)
        locations_per_level[location.level].append(location)

    for depth in range(max(locations_per_depth.keys()) + 1):
        locations = locations_per_depth[depth]
        cummulative_locations_per_depth[depth] = cummulative_locations_per_depth.get(depth - 1, 0) + len(locations)

    starting_level = 10
    if cummulative_locations_per_depth[3] < 10:
        starting_level += 5

    reduced_item_pool = []
    for item in item_pool:
        if item.name in prog_levels:
            item.classification = ItemClassification.progression
        if prog_doh and item.name == prog_doh:
            item.classification = ItemClassification.progression
            prog_doh = ""

        if "Levels" in item.name:
            # Add the levels from this item, always 5 currently.
            seen_levels[item.name] = seen_levels.get(item.name, 0) + 5
            # If it is the first levels for the starting class, add the item to starting inventory.
            if item.name == start_class and seen_levels[item.name] <= starting_level:
                # Added to starting inventory instead of the item pool.
                multiworld.push_precollected(item)
                continue
            if item.name == "5 FSH Levels" and seen_levels[item.name] <= 5:
                multiworld.push_precollected(item)
                continue

        if item_name_to_item[item.name].get("level", 0) > level_cap:
            # Do not add the item to the item pool if the level requirement is above the level cap.
            continue

        reduced_item_pool.append(item)

    return reduced_item_pool

    # Some other useful hook options:

    ## Place an item at a specific location
    # location = next(l for l in multiworld.get_unfilled_locations(player=player) if l.name == "Location Name")
    # item_to_place = next(i for i in item_pool if i.name == "Item Name")
    # location.place_locked_item(item_to_place)
    # item_pool.remove(item_to_place)


# The complete item pool prior to being set for generation is provided here, in case you want to make changes to it
def after_create_items(item_pool: list[ManualItem], world: World, multiworld: MultiWorld, player: int) -> list:
    return item_pool

# Called before rules for accessing regions and locations are created. Not clear why you'd want this, but it's here.
def before_set_rules(world: World, multiworld: MultiWorld, player: int):
    world._rule_data = {}
    file = Utils.user_path('data', 'ffxiv_rule_data.json')
    try:
        if os.path.exists(file):
            with open(file, "r") as f:
                world._rule_data = json.load(f)
    except Exception as e:
        print(f"Error loading rule data cache: {e}")
    world._rule_data.setdefault("locations", {})
    world._rule_data.setdefault("entrances", {})


# Called after rules for accessing regions and locations are created, in case you want to see or modify that information.
def after_set_rules(world: World, multiworld: MultiWorld, player: int):
    if not Utils.is_frozen():
        file = Utils.user_path('data', 'ffxiv_rule_data.json')
        with open(file, "w") as f:
            json.dump(world._rule_data, f, indent=1)

    for region in world.culled_regions:
        for entrance in region.entrances:
            entrance.hide_path = True
            entrance.access_rule = Entrance.access_rule

    goal_name = victory_names[get_option_value(multiworld, player, "goal")]
    base_name = BOSS_GOAL_KEY_LOCATIONS.get(goal_name)
    goal_location = multiworld.get_location(goal_name, player)
    goal_trial = getattr(world, '_goal_trial', None)

    if base_name and goal_trial:
        event_name = f"{base_name} Cleared"
        trial_location = multiworld.get_location(goal_trial, player)

        # Put goal's "Cleared" item into it's trial location and require it for goal
        item_id = world.item_name_to_id.get(event_name)
        trial_event = ManualItem(event_name, ItemClassification.progression, item_id, player)
        trial_location.place_locked_item(trial_event)

        def has_cleared_goal_trial(state: CollectionState) -> bool:
            return state.has(event_name, player)

        add_rule(goal_location, has_cleared_goal_trial)

        key_item = getattr(world, "_boss_key_item", "")
        key_count = getattr(world, "_boss_key_pieces", 0)

        if key_item and key_count > 0:
            def has_enough_key_pieces(state: CollectionState) -> bool:
                return state.count(key_item, player) >= key_count

            add_rule(trial_location, has_enough_key_pieces)

# This method is called before the victory location has the victory event placed and locked
def before_pre_fill(world: World, multiworld: MultiWorld, player: int):
    pass

# This method is called after the victory location has the victory event placed and locked
def after_pre_fill(world: World, multiworld: MultiWorld, player: int):
    pass

# The item name to create is provided before the item is created, in case you want to make changes to it
def before_create_item(item_name: str, world: World, multiworld: MultiWorld, player: int) -> str:
    return item_name

# The item that was created is provided after creation, in case you want to modify the item
def after_create_item(item: ManualItem, world: World, multiworld: MultiWorld, player: int) -> ManualItem:
    if getattr(multiworld, 'generation_is_fake', False):
        if "Levels" in item.name:
            item.classification = ItemClassification.progression
    # elif item.name in getattr(world, "prog_levels", []) or item.name in ["5 FSH Levels", "5 BLU Levels"]:
    #     item.classification = ItemClassification.progression
    return item

# This method is run towards the end of pre-generation, before the place_item options have been handled and before AP generation occurs
def before_generate_basic(world: World, multiworld: MultiWorld, player: int) -> None:
    pass

# This method is run at the very end of pre-generation, once the place_item options have been handled and before AP generation occurs
def after_generate_basic(world: World, multiworld: MultiWorld, player: int):
    pass

# This method is run every time an item is added to the state, can be used to modify the value of an item.
# IMPORTANT! Any changes made in this hook must be cancelled/undone in after_remove_item
def after_collect_item(world: World, state: CollectionState, Changed: bool, item: Item):
    # the following let you add to the Potato Item Value count
    # if item.name == "Cooked Potato":
    #     state.prog_items[item.player][format_state_prog_items_key(ProgItemsCat.VALUE, "Potato")] += 1
    pass

# This method is run every time an item is removed from the state, can be used to modify the value of an item.
# IMPORTANT! Any changes made in this hook must be first done in after_collect_item
def after_remove_item(world: World, state: CollectionState, Changed: bool, item: Item):
    # the following let you undo the addition to the Potato Item Value count
    # if item.name == "Cooked Potato":
    #     state.prog_items[item.player][format_state_prog_items_key(ProgItemsCat.VALUE, "Potato")] -= 1
    pass

# This is called before slot data is set and provides an empty dict ({}), in case you want to modify it before Manual does
def before_fill_slot_data(slot_data: dict, world: World, multiworld: MultiWorld, player: int) -> dict:
    slot_data["prog_classes"] = world.prog_classes
    slot_data["skipped_duties"] = list(world.skipped_duties)
    apJson = json.loads(pkgutil.get_data(__package__.rsplit('.', 1)[0], 'archipelago.json'))
    slot_data["world_version"] = apJson.get("world_version", "0.0.0")

    return slot_data

# This is called after slot data is set and provides the slot data at the time, in case you want to check and modify it after Manual is done with it
def after_fill_slot_data(slot_data: dict, world: World, multiworld: MultiWorld, player: int) -> dict:
    slot_data["mcguffins_needed"] = world.mcguffins_needed
    slot_data["boss_key_pieces"] = getattr(world, "_boss_key_pieces", 0)
    slot_data["boss_key_item"] = getattr(world, "_boss_key_item", "")

    return slot_data

# This is called right at the end, in case you want to write stuff to the spoiler log
def before_write_spoiler(world: World, multiworld: MultiWorld, spoiler_handle) -> None:
    pass

# This is called when you want to add information to the hint text
def before_extend_hint_information(hint_data: dict[int, dict[int, str]], world: World, multiworld: MultiWorld, player: int) -> None:

    ### Example way to use this hook:
    # if player not in hint_data:
    #     hint_data.update({player: {}})
    # for location in multiworld.get_locations(player):
    #     if not location.address:
    #         continue
    #
    #     use this section to calculate the hint string
    #
    #     hint_data[player][location.address] = hint_string

    pass

def after_extend_hint_information(hint_data: dict[int, dict[int, str]], world: World, multiworld: MultiWorld, player: int) -> None:
    pass


def hook_interpret_slot_data(world: World, player: int, slot_data: dict[str, Any]) -> dict[str, Any]:
    """
        Called when Universal Tracker wants to perform a fake generation
        Use this if you want to use or modify the slot_data for passed into re_gen_passthrough
    """
    return slot_data

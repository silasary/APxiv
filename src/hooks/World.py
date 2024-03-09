# Object classes from AP core, to represent an entire MultiWorld and this individual World that's part of it
from worlds.AutoWorld import World
from BaseClasses import MultiWorld, ItemClassification

# Object classes from Manual -- extending AP core -- representing items and locations that are used in generation
from ..Items import ManualItem
from ..Locations import ManualLocation

# Raw JSON data from the Manual apworld, respectively:
#          data/game.json, data/items.json, data/locations.json, data/regions.json
#
from ..Data import game_table, item_table, location_table, region_table

# These helper methods allow you to determine if an option has been set, or what its value is, for any player in the multiworld
from ..Helpers import is_option_enabled, get_option_value
from .Data import TANKS, HEALERS, MELEE, CASTER, RANGED, DOH



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



# Called before regions and locations are created. Not clear why you'd want this, but it's here. Victory location is included, but Victory event is not placed yet.
def before_create_regions(world: World, multiworld: MultiWorld, player: int):
    pass

# Called after regions and locations are created, in case you want to see or modify that information. Victory location is included.
def after_create_regions(world: World, multiworld: MultiWorld, player: int):
    locationNamesToRemove = []
    if not is_option_enabled(multiworld, player, "include_unreasonable_fates"):
        locationNamesToRemove.extend(["He Taketh It with His Eyes (FATE)", "Steel Reign (FATE)", "Coeurls Chase Boys Chase Coeurls (FATE)", "Prey Online (FATE)", "A Horse Outside (FATE)", "Foxy Lady (FATE)", "A Finale Most Formidable (FATE)", "The Head the Tail the Whole Damned Thing (FATE)", "Devout Pilgrims vs. Daivadipa (FATE)", "Omicron Recall: Killing Order (FATE)"])

    duty_diff = get_option_value(multiworld, player, "difficulty") or 0
    for location in location_table:
        if "diff" in location:
            if location["diff"] > duty_diff:
                print(f"Removing {location['name']} from {player}'s pool")
                locationNamesToRemove.append(location["name"])

    for region in multiworld.regions:
        if region.player == player:
            for location in list(region.locations):
                if location.name in locationNamesToRemove:
                    # print(f"Removing {location.name} from {player}'s pool")
                    region.locations.remove(location)

# The item pool before starting items are processed, in case you want to see the raw item pool at that stage
def before_create_items_starting(item_pool: list, world: World, multiworld: MultiWorld, player: int) -> list:
    return item_pool

# The item pool after starting items are processed but before filler is added, in case you want to see the raw item pool at that stage
def before_create_items_filler(item_pool: list, world: World, multiworld: MultiWorld, player: int) -> list:
    # Use this hook to remove items from the item pool
    itemNamesToRemove = [] # List of item names

    # Add your code here to calculate which items to remove.
    #
    # Because multiple copies of an item can exist, you need to add an item name
    # to the list multiple times if you want to remove multiple copies of it.

    for itemName in itemNamesToRemove:
        item = next(i for i in item_pool if i.name == itemName)
        item_pool.remove(item)

    return item_pool

    # Some other useful hook options:

    ## Place an item at a specific location
    # location = next(l for l in multiworld.get_unfilled_locations(player=player) if l.name == "Location Name")
    # item_to_place = next(i for i in item_pool if i.name == "Item Name")
    # location.place_locked_item(item_to_place)
    # item_pool.remove(item_to_place)

# The complete item pool prior to being set for generation is provided here, in case you want to make changes to it
def after_create_items(item_pool: list, world: World, multiworld: MultiWorld, player: int) -> list:
    return item_pool

# The item pool before starting items are processed, in case you want to see the raw item pool at that stage
def before_create_items_starting(item_pool: list, world: World, multiworld: MultiWorld, player: int) -> list:
    return item_pool

# The item pool after starting items are processed but before filler is added, in case you want to see the raw item pool at that stage
def before_create_items_filler(item_pool: list, world: World, multiworld: MultiWorld, player: int) -> list:
    # Use this hook to remove items from the item pool
    itemNamesToRemove = [] # List of item names

    # Add your code here to calculate which items to remove.
    #
    # Because multiple copies of an item can exist, you need to add an item name
    # to the list multiple times if you want to remove multiple copies of it.

    for itemName in itemNamesToRemove:
        item = next(i for i in item_pool if i.name == itemName)
        item_pool.remove(item)

    return item_pool

    # Some other useful hook options:

    ## Place an item at a specific location
    # location = next(l for l in multiworld.get_unfilled_locations(player=player) if l.name == "Location Name")
    # item_to_place = next(i for i in item_pool if i.name == "Item Name")
    # location.place_locked_item(item_to_place)
    # item_pool.remove(item_to_place)

# The complete item pool prior to being set for generation is provided here, in case you want to make changes to it
def after_create_items(item_pool: list, world: World, multiworld: MultiWorld, player: int) -> list:
    return item_pool

# Called before rules for accessing regions and locations are created. Not clear why you'd want this, but it's here.
def before_set_rules(world: World, multiworld: MultiWorld, player: int):
    pass

# Called after rules for accessing regions and locations are created, in case you want to see or modify that information.
def after_set_rules(world: World, multiworld: MultiWorld, player: int):
    # goal_count = get_option_value(world, player, "mcguffins_needed")
    # multiworld.completion_condition[player] = lambda state: state.count("Memory of a Distant World", player) > goal_count
    # for region in multiworld.get_regions(player):
    #     for location in region.locations:
    #         if location.name == "__Manual Game Complete__":
    #             location.access_rule = lambda state: state.count("Memory of a Distant World", player) > goal_count
    pass

# The complete item pool prior to being set for generation is provided here, in case you want to make changes to it
def before_generate_basic(item_pool: list[ManualItem], world: World, multiworld: MultiWorld, player: int) -> list:
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
    force_jobs = get_option_value(multiworld, player, "force_jobs")
    if force_jobs:
        prog_classes = force_jobs
    else:
        prog_classes = [tanks[0], healers[0], melee[0], caster[0], ranged[0]]
    world.prog_classes = prog_classes
    prog_levels = [f"5 {job} Levels" for job in prog_classes]
    prog_fish = 0
    prog_doh = doh[0]
    for item in item_pool:
        if item.name in prog_levels:
            item.classification = ItemClassification.progression
        if item.name == "5 FSH Levels" and prog_fish < 12:
            # Let's make one level of Fisher required
            item.classification = ItemClassification.progression
            prog_fish += 1
        if prog_doh and item.name == f'5 {prog_doh} Levels':
            item.classification = ItemClassification.progression
            prog_doh = None
    return item_pool

# This method is run at the very end of pre-generation, once the place_item options have been handled and before AP generation occurs
def after_generate_basic(world: World, multiworld: MultiWorld, player: int):
    pass

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
    return item

# This method is run towards the end of pre-generation, before the place_item options have been handled and before AP generation occurs
def before_generate_basic(world: World, multiworld: MultiWorld, player: int) -> list:
    pass

# This method is run at the very end of pre-generation, once the place_item options have been handled and before AP generation occurs
def after_generate_basic(world: World, multiworld: MultiWorld, player: int):
    pass

# This is called before slot data is set and provides an empty dict ({}), in case you want to modify it before Manual does
def before_fill_slot_data(slot_data: dict, world: World, multiworld: MultiWorld, player: int) -> dict:
    slot_data["prog_classes"] = world.prog_classes
    return slot_data

# This is called after slot data is set and provides the slot data at the time, in case you want to check and modify it after Manual is done with it
def after_fill_slot_data(slot_data: dict, world: World, multiworld: MultiWorld, player: int) -> dict:
    return slot_data

# This is called right at the end, in case you want to write stuff to the spoiler log
def before_write_spoiler(world: World, multiworld: MultiWorld, spoiler_handle) -> None:
    pass

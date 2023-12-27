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



########################################################################################
## Order of method calls when the world generates:
##    1. create_regions - Creates regions and locations
##    2. set_rules - Creates rules for accessing regions and locations
##    3. generate_basic - Creates the item pool and runs any place_item options
##    4. pre_fill - Creates the victory location
##
## The create_item method is used by plando and start_inventory settings to create an item from an item name.
## The fill_slot_data method will be used to send data to the Manual client for later use, like deathlink.
########################################################################################



# Called before regions and locations are created. Not clear why you'd want this, but it's here.
def before_create_regions(world: World, multiworld: MultiWorld, player: int):
    pass

# Called after regions and locations are created, in case you want to see or modify that information.
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

# Called before rules for accessing regions and locations are created. Not clear why you'd want this, but it's here.
def before_set_rules(world: World, multiworld: MultiWorld, player: int):
    pass

# Called after rules for accessing regions and locations are created, in case you want to see or modify that information.
def after_set_rules(world: World, multiworld: MultiWorld, player: int):
    pass

# The complete item pool prior to being set for generation is provided here, in case you want to make changes to it
def before_generate_basic(item_pool: list[ManualItem], world: World, multiworld: MultiWorld, player: int) -> list:
    tanks = ["PLD","WAR","DRK","GNB"]
    healers = ["WHM","SCH","AST","SGE"]
    melee = ["MNK","DRG","NIN","SAM","RPR"]
    caster = ["BLM","SMN","RDM",]
    ranged = ["BRD","MCH","DNC"]
    doh = ["CRP", "BSM", "ARM", "GSM", "LTW", "WVR", "ALC", "CUL"]

    world.random.shuffle(tanks)
    world.random.shuffle(healers)
    world.random.shuffle(melee)
    world.random.shuffle(caster)
    world.random.shuffle(ranged)
    world.random.shuffle(doh)
    prog_classes = [tanks[0], healers[0], melee[0], caster[0], ranged[0]]
    prog_levels = [f"5 {job} Levels" for job in prog_classes]
    prog_fish = False
    prog_doh = doh[0]
    for item in item_pool:
        if item.name in prog_levels:
            item.classification = ItemClassification.progression
        if item.name == "5 FSH Levels" and not prog_fish:
            # Let's make one level of Fisher required
            item.classification = ItemClassification.progression
            prog_fish = True
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

# This is called before slot data is set and provides an empty dict ({}), in case you want to modify it before Manual does
def before_fill_slot_data(slot_data: dict, world: World, multiworld: MultiWorld, player: int) -> dict:
    return slot_data

# This is called after slot data is set and provides the slot data at the time, in case you want to check and modify it after Manual is done with it
def after_fill_slot_data(slot_data: dict, world: World, multiworld: MultiWorld, player: int) -> dict:
    return slot_data

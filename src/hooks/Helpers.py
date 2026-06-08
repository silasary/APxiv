from typing import Any, Optional

from BaseClasses import MultiWorld

from .. import Helpers

def get_int_value(multiworld: MultiWorld, player: int, option_name: str) -> int:
    from ..Helpers import get_option_value
    value = get_option_value(multiworld, player, option_name)
    assert isinstance(value, int)
    return value

def is_fishsanity_only(multiworld: MultiWorld, player: int) -> bool:
    from ..Helpers import is_option_enabled
    is_fishsanity = is_option_enabled(multiworld, player, "fishsanity")
    has_fates = is_option_enabled(multiworld, player, "fatesanity") or is_option_enabled(multiworld, player, "fates_per_zone")
    has_duties = get_int_value(multiworld, player, "duty_difficulty") > 0
    return is_fishsanity and not (has_fates or has_duties)

def is_fishing_enabled(multiworld, player):
    from ..Helpers import is_option_enabled
    return is_option_enabled(multiworld, player, "fishsanity") or is_option_enabled(multiworld, player, "include_ocean_fishing")

# Use this if you want to override the default behavior of is_option_enabled
# Return True to enable the category, False to disable it, or None to use the default behavior
def before_is_category_enabled(multiworld: MultiWorld, player: int, category_name: str) -> Optional[bool]:
    if category_name == "FATEsanity":
        return Helpers.is_option_enabled(multiworld, player, "fatesanity")
    if category_name == "FATEs":
        return not Helpers.is_option_enabled(multiworld, player, "fatesanity")
    if category_name == "fishsanity":
        return get_int_value(multiworld, player, "fishsanity") > 0
    if category_name == "Timed Fish":
        return get_int_value(multiworld, player, "fishsanity") > 1
    if category_name == "Big Fishing":
        return get_int_value(multiworld, player, "fishsanity") > 2
    if category_name == "McGuffin":
        return get_int_value(multiworld, player, "mcguffins_needed") > 0
    return None

# Use this if you want to override the default behavior of is_option_enabled
# Return True to enable the item, False to disable it, or None to use the default behavior
def before_is_item_enabled(multiworld: MultiWorld, player: int, item: dict[str, Any]) -> Optional[bool]:
    from .Data import BOSS_GOAL_DATA
    item_name = item.get('name', '')

    for duty_name, _, _ in BOSS_GOAL_DATA.values():
        # Disable all boss key/key piece items. The correct one is re-enabled in before_create_items_all
        if item_name in (f"{duty_name} Key", f"{duty_name} Key Piece"):
            return False
        # Disable all boss cleared items. The correct one is placed as a locked item in after_set_rules
        if item_name == f"{duty_name} Cleared":
            return False


    return None

# Use this if you want to override the default behavior of is_option_enabled
# Return True to enable the location, False to disable it, or None to use the default behavior
def before_is_location_enabled(multiworld: MultiWorld, player: int, location: dict[str, Any]) -> Optional[bool]:
    level_cap = get_int_value(multiworld, player, "level_cap")
    if location.get('victory'):  # This should get fixed in the main code
        return True
    if location.get("duty_name") in multiworld.worlds[player].skipped_duties:
        return False
    if "diff" in location and location["diff"] > get_int_value(multiworld, player, "duty_difficulty"):
        return False
    if "party" in location and location["party"] > get_int_value(multiworld, player, "max_party_size"):
        return False
    if "level" in location and int(location["level"]) > level_cap:
        return False
    if "fate_number" in location and location["fate_number"] > get_int_value(multiworld, player, "fates_per_zone"):
        return False
    if "extra_number" in location and location["extra_number"] > get_int_value(multiworld, player, "extra_dungeon_checks"):
        return False
    #HW locations
    if location['region'] == "Ishgard" and level_cap < 51:
        return False
    if location['region'] == "The Firmament" and level_cap < 51:
        return False
    if location['region'] == "Coerthas Western Highlands" and level_cap < 51:
        return False
    if location['region'] == "The Sea of Clouds" and level_cap < 51:
        return False
    if location['region'] == "The Dravanian Forelands" and level_cap < 51:
        return False
    if location['region'] == "The Churning Mists" and level_cap < 51:
        return False
    if location['region'] == "The Dravanian Hinterlands" and level_cap < 51:
        return False
    if location['region'] == "Azys Lla" and level_cap < 51:
        return False
    if location['region'] == "Alexander" and level_cap < 51:
        return False
    #StB locations
    if location['region'] == "Rhalgr's Reach" and level_cap < 61:
        return False
    if location['region'] == "The Fringes" and level_cap < 61:
        return False
    if location['region'] == "The Peaks" and level_cap < 61:
        return False
    if location['region'] == "The Lochs" and level_cap < 61:
        return False
    if location['region'] == "Interdimensional Rift" and level_cap < 61:
        return False
    if location['region'] == "Kugane" and level_cap < 61:
        return False
    if location['region'] == "Shirogane" and level_cap < 61:
        return False
    if location['region'] == "The Ruby Sea" and level_cap < 61:
        return False
    if location['region'] == "Yanxia" and level_cap < 61:
        return False
    if location['region'] == "The Azim Steppe" and level_cap < 61:
        return False
    #ShB locations
    if location['region'] == "The Crystarium" and level_cap < 71:
        return False
    if location['region'] == "Eulmore" and level_cap < 71:
        return False
    if location['region'] == "Lakeland" and level_cap < 71:
        return False
    if location['region'] == "Kholusia" and level_cap < 71:
        return False
    if location['region'] == "Amh Araeng" and level_cap < 71:
        return False
    if location['region'] == "Il Mheg" and level_cap < 71:
        return False
    if location['region'] == "The Rak'tika Greatwood" and level_cap < 71:
        return False
    if location['region'] == "The Tempest" and level_cap < 71:
        return False
    if location['region'] == "The Empty" and level_cap < 71:
        return False
    if location['region'] == "Gangos" and level_cap < 71:
        return False
    #EnW locations
    if location['region'] == "Old Sharlayan" and level_cap < 81:
        return False
    if location['region'] == "Radz-at-Han" and level_cap < 81:
        return False
    if location['region'] == "Labyrinthos" and level_cap < 81:
        return False
    if location['region'] == "Thavnair" and level_cap < 81:
        return False
    if location['region'] == "Garlemald" and level_cap < 81:
        return False
    if location['region'] == "Mare Lamentorum" and level_cap < 81:
        return False
    if location['region'] == "Elpis" and level_cap < 81:
        return False
    if location['region'] == "Ultima Thule" and level_cap < 81:
        return False
    if location['region'] == "Elysion" and level_cap < 81:
        return False
    #DT locations
    if location['region'] == "Tuliyollal" and level_cap < 91:
        return False
    if location['region'] == "Solution Nine" and level_cap < 91:
        return False
    if location['region'] == "Urqopacha" and level_cap < 91:
        return False
    if location['region'] == "Kozama'uka" and level_cap < 91:
        return False
    if location['region'] == "Yak T'el" and level_cap < 91:
        return False
    if location['region'] == "Shaaloani" and level_cap < 91:
        return False
    if location['region'] == "Heritage Found" and level_cap < 91:
        return False
    if location['region'] == "Living Memory" and level_cap < 91:
        return False
    if location['region'] == "The Arcadion" and level_cap < 91:
        return False
    if location['region'] == "Mist Continent" and level_cap < 91:
        return False
    return None


def before_is_event_enabled(multiworld: MultiWorld, player: int, event: dict[str, Any]) -> Optional[bool]:
    return None

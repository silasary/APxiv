from typing import Any, Optional

from BaseClasses import MultiWorld

from .. import Helpers

REGION_LEVEL_GATES: dict[str, int] = {
    # Heavensward
    "The Firmament":              51,
    "Coerthas Western Highlands": 51,
    "The Sea of Clouds":          51,
    # Stormblood
    "The Fringes":                61,
    "The Peaks":                  61,
    # Shadowbringers
    "Lakeland":                   71,
    "Kholusia":                   71,
    "Amh Araeng":                 71,
    # Endwalker
    "Old Sharlayan":              81,
    "Labyrinthos":                81,
    "Thavnair":                   81,
    # Dawntrail
    "Urqopacha":                  91,
    "Kozama'uka":                 91,
}

def get_int_value(multiworld: MultiWorld, player: int, option_name: str) -> int:
    from ..Helpers import get_option_value
    value = get_option_value(multiworld, player, option_name)
    assert isinstance(value, int)
    return value

def get_excluded_expansions(multiworld: MultiWorld, player: int) -> set[str]:
    """Excluded by exclude_expansions plus free_trial"""
    from ..Helpers import get_option_value, is_option_enabled
    from .Data import FREE_TRIAL_EXCLUDED_EXPANSIONS

    excluded = set(get_option_value(multiworld, player, "exclude_expansions"))

    if is_option_enabled(multiworld, player, "free_trial"):
        excluded.update(FREE_TRIAL_EXCLUDED_EXPANSIONS)
        
    return excluded

def get_excluded_jobs(multiworld: MultiWorld, player: int) -> set[str]:
    """Jobs excluded directly via exclude_jobs plus all jobs of any excluded expansion"""
    from ..Helpers import get_option_value
    from .Data import JOB_EXPANSION
    
    excluded = set(get_option_value(multiworld, player, "exclude_jobs"))
    expansions = get_excluded_expansions(multiworld, player)
    excluded.update(job for job, exp in JOB_EXPANSION.items() if exp in expansions)
    
    return excluded

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

    excluded_expansions = get_excluded_expansions(multiworld, player)
    location_expansion = location.get("expansion")
    
    if location_expansion in excluded_expansions:
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
    
    region_min = REGION_LEVEL_GATES.get(location['region'])
    
    if region_min is not None and level_cap < region_min:
        return False
    
    return None


def before_is_event_enabled(multiworld: MultiWorld, player: int, event: dict[str, Any]) -> Optional[bool]:
    return None

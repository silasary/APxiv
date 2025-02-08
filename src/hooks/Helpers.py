from typing import Optional, TYPE_CHECKING
from BaseClasses import MultiWorld, Item, Location
from .. import Helpers

if TYPE_CHECKING:
    from ..Items import ManualItem
    from ..Locations import ManualLocation

# Use this if you want to override the default behavior of is_option_enabled
# Return True to enable the category, False to disable it, or None to use the default behavior
def before_is_category_enabled(multiworld: MultiWorld, player: int, category_name: str) -> Optional[bool]:
    if category_name == "FATEsanity":
        return Helpers.is_option_enabled(multiworld, player, "fatesanity")
    if category_name == "FATEs":
        return not Helpers.is_option_enabled(multiworld, player, "fatesanity")
    if category_name == "fishsanity":
        return Helpers.get_option_value(multiworld, player, "fishsanity") > 0
    if category_name == "Timed Fish":
        return Helpers.get_option_value(multiworld, player, "fishsanity") > 1
    if category_name == "Big Fishing":
        return Helpers.get_option_value(multiworld, player, "fishsanity") > 2
    return None

# Use this if you want to override the default behavior of is_option_enabled
# Return True to enable the item, False to disable it, or None to use the default behavior
def before_is_item_enabled(multiworld: MultiWorld, player: int, item: "ManualItem") -> Optional[bool]:
    return None

# Use this if you want to override the default behavior of is_option_enabled
# Return True to enable the location, False to disable it, or None to use the default behavior
def before_is_location_enabled(multiworld: MultiWorld, player: int, location: "ManualLocation") -> Optional[bool]:
    return None

from typing import Optional
from BaseClasses import MultiWorld
from .. import Helpers


# Use this if you want to override the default behavior of is_option_enabled
# Return True to enable the category, False to disable it, or None to use the default behavior
def before_is_category_enabled(world: MultiWorld, player: int, category_name: str) -> Optional[bool]:
    if category_name == "FATEsanity":
        return Helpers.is_option_enabled(world, player, "fatesanity")
    if category_name == "FATEs":
        return not Helpers.is_option_enabled(world, player, "fatesanity")
    return None

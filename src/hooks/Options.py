# Object classes from AP that represent different types of options that you can create
from Options import FreeText, NumericOption, Toggle, DefaultOnToggle, Choice, TextChoice, Range, SpecialRange

# These helper methods allow you to determine if an option has been set, or what its value is, for any player in the multiworld
from ..Helpers import is_option_enabled, get_option_value

class OceanFishing(Toggle):
    """
    Ocean Fishing departs once every two real-world hours on a specified route.
    There are six total routes, four in Eorzea, two in the East.  They do not loop evenly, you may see duplicates before you see all six.
    This means it will take at least 12 hours to complete all the relevant checks.
    This option is absolutely not sync-viable.
    """
    display_name = "Enable Ocean Fishing"
    default = True

class UnreasonableFates(Toggle):
    """
    Include World Bosses and other FATEs that are not reasonable to complete.

    These fates often spawn once every 2-3 days, don't show up on the map, and can require a large number of players to defeat.
    If you use this option, keep an eye on Faloop (or your DC's equivalent) to know when they're up.
    """
    display_name = "Include Unreasonable FATEs"
    default = True

class Fatesanity(Toggle):
    """
    Include individual FATEs in the location pool.

    If enabled, each named FATE is a check.  If disabled, you only need to complete 5 FATEs of your choice per zone.
    """
    display_name = "Fatesanity"
    default = True

# This is called before any manual options are defined, in case you want to define your own with a clean slate or let Manual define over them
def before_options_defined(options: dict) -> dict:
    return options

# This is called after any manual options are defined, in case you want to see what options are defined or want to modify the defined options
def after_options_defined(options: dict) -> dict:
    options["include_ocean_fishing"] = OceanFishing
    options["include_unreasonable_fates"] = UnreasonableFates
    options["fatesanity"] = Fatesanity
    return options

# Object classes from AP that represent different types of options that you can create
from BaseClasses import PlandoOptions
from Options import Toggle, DefaultOnToggle, Choice, TextChoice, Range, SpecialRange, ItemSet, OptionSet
from worlds.AutoWorld import World
from Utils import get_fuzzy_results

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
    default = False

class Fatesanity(Toggle):
    """
    Include individual FATEs in the location pool.

    If enabled, each named FATE is a check.  If disabled, you only need to complete 5 FATEs of your choice per zone.
    """
    display_name = "Fatesanity"
    default = False

class UnreasonableFates(Toggle):
    """
    Include World Bosses and other FATEs that are not reasonable to complete.

    These fates often spawn once every 2-3 days, don't show up on the map, and can require a large number of players to defeat.
    If you use this option, keep an eye on Faloop (or your DC's equivalent) to know when they're up.
    """
    display_name = "Include Unreasonable FATEs"
    default = False

class DutyDifficulty(Choice):
    """
    Maximum difficulty of the duty content.
    [normal] Dungeons, trials, normal raids, and alliance raids are included in the location pool.
    [extreme] As above, but extreme trials are included in the location pool.
    [savage] As above, but old savage raids are included in the location pool.
    [endgame] As above, but the current savage tier is included in the location pool.
    """
    default = 1
    display_name = "Duty Difficulty"
    option_normal = 0
    option_extreme = 1
    option_savage = 2
    option_endgame = 3

class McGuffinsNeeded(Range):
    """
    Number of Distant Memories needed to win the game.
    """
    display_name = "McGuffins Needed"
    default = 80
    range_start = 1
    range_end = 100

class ForceJob(OptionSet):
    """
    Choose which classes are progression.

    If none are selected, five (one tank, one healer, one melee, one phys range, one caster) are chosen at random.
    """
    display_name = "Force Progression Jobs"

    def verify(self, world: type[World], player_name: str, plando_options: PlandoOptions) -> None:
        from .Data import TANKS, HEALERS, MELEE, CASTER, RANGED, DOH
        all = TANKS + HEALERS + MELEE + CASTER + RANGED + DOH
        print(f"{repr(self.value)}/{repr(all)}")
        for item_name in self.value:
            if item_name not in all:
                picks = get_fuzzy_results(item_name, all, limit=1)
                raise Exception(f"Item {item_name} from option {self} "
                                f"is not a valid job from {world.game}. "
                                f"Did you mean '{picks[0][0]}' ({picks[0][1]}% sure)")


        return super().verify(world, player_name, plando_options)

class LevelCap(Range):
    """
    Maximum level of the player.
    """
    display_name = "Level Cap"
    default = 90
    range_start = 10
    range_end = 90

# This is called before any manual options are defined, in case you want to define your own with a clean slate or let Manual define over them
def before_options_defined(options: dict) -> dict:
    return options

# This is called after any manual options are defined, in case you want to see what options are defined or want to modify the defined options
def after_options_defined(options: dict) -> dict:
    options["include_ocean_fishing"] = OceanFishing
    options["fatesanity"] = Fatesanity
    options["include_unreasonable_fates"] = UnreasonableFates
    options["difficulty"] = DutyDifficulty
    options["mcguffins_needed"] = McGuffinsNeeded
    options["force_jobs"] = ForceJob
    options["level_cap"] = LevelCap
    return options

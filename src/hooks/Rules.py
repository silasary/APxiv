
import dataclasses
from typing import TYPE_CHECKING
from BaseClasses import CollectionState, MultiWorld
from worlds.AutoWorld import World
from Utils import version_tuple

from ..Helpers import get_option_value
from ..Game import game_name

if TYPE_CHECKING:
    from .. import ManualWorld

use_rulebuilder = version_tuple >= (0, 6, 7)

# Sometimes you have a requirement that is just too messy or repetitive to write out with boolean logic.
# Define a function here, and you can use it in a requires string with (function_name()}.
# def overfishedAnywhere(world: World, mw: MultiWorld, state: CollectionState, player: int):
#     """Has the player collected all fish from any fishing log?"""
#     for cat, items in world.item_name_groups:
#         if cat.endswith("Fishing Log") and state.has_all(items, player):
#             return True
#     return False

# You can also pass an argument to your function, like |$function_name:arg|
def anyClassLevel(world: World, multiworld: MultiWorld, state: CollectionState, player: int, level: str):
    """Has the player reached the given level in any class?"""
    if int(level) < 5:
        return True
    for job in world.item_name_groups["DOW/DOM"]:
        if (state.count(job, player) * 5) >= int(level):
            return True
    return False

def anyCrafterLevel(world: World, multiworld: MultiWorld, state: CollectionState, player: int, level: str):
    """Has the player reached the given level in any class?"""
    if int(level) < 5:
        return True
    for job in world.item_name_groups["DOH"]:
        if (state.count(job, player) * 5) >= int(level):
            return True
    return False

def EnoughMemories(world: World, multiworld: MultiWorld, state: CollectionState, player: int):
    """Has the player collected enough Memories to complete the game?"""
    goal_count = get_option_value(multiworld, player, "mcguffins_needed")
    assert isinstance(goal_count, int)
    return state.count("Memory of a Distant World", player) >= goal_count

if use_rulebuilder:
    from rule_builder.rules import Rule, Has, True_, False_, HasAnyCount

    @dataclasses.dataclass()
    class anyClassLevelRule(Rule["ManualWorld"], game=game_name):
        level: int
        def _instantiate(self, world: "ManualWorld") -> Rule.Resolved:
            expected_count = int(self.level) // 5
            counts = {job: expected_count for job in world.item_name_groups["DOW/DOM"]}
            return HasAnyCount(counts).resolve(world)

    @dataclasses.dataclass()
    class anyCrafterLevelRule(Rule["ManualWorld"], game=game_name):
        level: int
        def _instantiate(self, world: "ManualWorld") -> Rule.Resolved:
            expected_count = int(self.level) // 5
            counts = {job: expected_count for job in world.item_name_groups["DOH"]}
            return HasAnyCount(counts).resolve(world)

    @dataclasses.dataclass()
    class EnoughMemoriesRule(Rule["ManualWorld"], game=game_name):
        def _instantiate(self, world: "ManualWorld") -> Rule.Resolved:
            return Has("Memory of a Distant World", world.options.mcguffins_needed.value).resolve(world)

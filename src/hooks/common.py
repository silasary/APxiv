import re
from dataclasses import dataclass
from typing import NotRequired, TypedDict

RE_N_JOB_LEVELS = re.compile(r"^(?P<num>\d{1,2})\s+(?P<job>[A-Z]{2,3})\s+Levels$")

TANKS = ["PLD", "WAR", "DRK", "GNB"]
HEALERS = ["WHM", "SCH", "AST", "SGE"]
MELEE = ["MNK", "DRG", "NIN", "SAM", "RPR", "VPR"]
CASTER = ["BLM", "SMN", "RDM", "PCT"]
RANGED = ["BRD", "MCH", "DNC"]
DOH = ["CRP", "BSM", "ARM", "GSM", "LTW", "WVR", "ALC", "CUL"]
DOL = ["MIN", "BTN", "FSH"]

LEVEL_CAP = 100
LIMITED_LEVEL_CAPS = {
    "BLU": 80,
    "BST": 50,
}

leading_words = ["Defeat", "Catch", "the"]

@dataclass
class GoalData:
    name: str
    location_name: str
    region: str = ""
    level: int = 0
    _trophy_name: str | None = None
    _key_name: str | None = None

    @property
    def trophy_name(self) -> str:
        if self._trophy_name is None:
            trophy_name = self.name
            for verb in leading_words:
                if trophy_name.startswith(verb):
                    trophy_name = trophy_name[len(verb):].strip()
            self._trophy_name = trophy_name.title() + " Trophy"
        return self._trophy_name

    @property
    def key_name(self) -> str:
        if self._key_name is None:
            key_name = self.location_name + " Key"
            if key_name.startswith("The "):
                key_name = key_name[4:].strip()
            self._key_name = key_name

        return self._key_name

goals = [
    GoalData(
        name="Defeat the Ultima Weapon",
        location_name="The Porta Decumana",
    ),
    GoalData(
        name="Defeat King Thordan",
        location_name="The Singularity Reactor",
    ),
    GoalData(
        name="Defeat Nidhogg",
        location_name="The Final Steps of Faith",
    ),
    GoalData(
        name="Defeat Shinryu",
        location_name="The Royal Menagerie",
    ),
    GoalData(
        name="Defeat Tsukuyomi",
        location_name="Castrum Fluminis",
    ),
    GoalData(
        name="Defeat Hades",
        location_name="The Dying Gasp",
    ),
    GoalData(
        name="Defeat the Warrior of Light",
        location_name="The Seat of Sacrifice",
    ),
    GoalData(
        name="Defeat the Endsinger",
        location_name="The Final Day",
    ),
    GoalData(
        name="Defeat Zeromus",
        location_name="The Abyssal Fracture",
    ),
    GoalData(
        name="Defeat Sphene",
        location_name="The Interphos",
    ),
    GoalData(
        name="Defeat Necron",
        location_name="The Ageless Necropolis",
    ),
]

BOSS_GOAL_DATA: dict[str, GoalData] = {goal.name: goal for goal in goals}

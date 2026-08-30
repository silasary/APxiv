import re
from typing import NotRequired, TypedDict

RE_N_JOB_LEVELS = re.compile(r"^(?P<num>\d{1,2})\s+(?P<job>[A-Z]{2,3})\s+Levels$")

class LocationDict(TypedDict):
    name: str
    region: str
    category: NotRequired[list[str]]
    requires: NotRequired[str]
    level: NotRequired[int | str]
    id: NotRequired[int]
    duty_name: NotRequired[str | None]
    party: NotRequired[int | None]
    diff: NotRequired[int | None]
    extra_number: NotRequired[int | None]
    expansion: NotRequired[str | None]
    filler: NotRequired[bool | None]
    victory: NotRequired[bool | None]
    is_dungeon: NotRequired[bool | None]
    rank: NotRequired[str | None]
    fate_number: NotRequired[int]


TANKS = ["PLD","WAR","DRK","GNB"]
HEALERS = ["WHM","SCH","AST","SGE"]
MELEE = ["MNK","DRG","NIN","SAM","RPR", "VPR"]
CASTER = ["BLM","SMN","RDM","PCT"]
RANGED = ["BRD","MCH","DNC"]
DOH = ["CRP", "BSM", "ARM", "GSM", "LTW", "WVR", "ALC", "CUL"]
DOL = ["MIN", "BTN", "FSH"]
LEVEL_CAP = 100
LIMITED_LEVEL_CAPS = {
    "BLU": 80,
    "BST": 50,
}

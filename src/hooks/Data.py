from math import ceil

import csv
import pkgutil

short_long = {
    "MLN": "Middle La Noscea",
    "LLN": "Lower La Noscea",
    "ELN": "Eastern La Noscea",
    "WLN": "Western La Noscea",
    "ULN": "Upper La Noscea",
    "OLN": "Outer La Noscea",

    "CS": "Central Shroud",
    "ES": "East Shroud",
    "SS": "South Shroud",
    "NS": "North Shroud",

    "CT": "Central Thanalan",
    "WT": "Western Thanalan",
    "ET": "Eastern Thanalan",
    "ST": "Southern Thanalan",
    "NT": "Northern Thanalan",

    "CCH": "Coerthas Central Highlands",
    "CWH": "Coerthas Western Highlands",

    "MD": "Mor Dhona",

    "TSC": "The Sea of Clouds",
    "AL": "Azys Lla",

    "TDF": "The Dravanian Forelands",
    "TCM": "The Churning Mists",
    "TDH": "The Dravanian Hinterlands",

    "TF": "The Fringes",
    "TP": "The Peaks",
    "TL": "The Lochs",

    "TRS": "The Ruby Sea",
    "Y": "Yanxia",
    "TAS": "The Azim Steppe",

    "L": "Lakeland",
    "K": "Kholusia",
    "IM": "Il Mheg",
    "AA": "Amh Araeng",
    "TRG": "The Rak'tika Greatwood",
    "TT": "The Tempest"
}

def generate_duty_list():
    duty_list = []

    dutyreader = csv.reader(pkgutil.get_data(__name__, "duties.csv").decode().splitlines(), delimiter=',', quotechar='|')

    for row in dutyreader:
        if row[0] not in ["", "Name", "ARR", "HW", "STB", "SHB"]:
            requires_str = "|" + row[4] + " Access:1| and |$anyClassLevel:" + row[2] + "|"
            requires_str += (" and |" + row[7] + "|") if  (row[7] != "") else ""
            duty_list.append(
                {
                    "name": row[0],
                    "region": "Duty",
                    "category": [row[1], row[4]],
                    "requires": requires_str,
                    #"level" : row[3]
                    "party" : row[5],
                    "diff" : row[6],
                }
            )

    return duty_list

duty_locations = generate_duty_list()

# called after the items.json file has been loaded, before any item loading or processing has occurred
# if you need access to the items after processing to add ids, etc., you should use the hooks in World.py
def after_load_item_file(item_table: list) -> list:
    classes = [
        # tanks
        "PLD",
        "WAR",
        "DRK",
        "GNB",
        # healers
        "WHM",
        "SCH",
        "AST",
        "SGE",
        # melee dps
        "MNK",
        "DRG",
        "NIN",
        "SAM",
        "RPR",
        # ranged dps
        "BRD",
        "MCH",
        "DNC",
        # caster dps
        "BLM",
        "SMN",
        "RDM",
        "BLU",
    ]
    # crafters
    DOH = [
        "CRP",
        "BSM",
        "ARM",
        "GSM",
        "LTW",
        "WVR",
        "ALC",
        "CUL",
    ]

    # gatherers
    DOL = [
        "MIN",
        "BTN",
        "FSH",
        ]
    max_level = 90

    for job in classes:
        item_table.append({
            "name": f"5 {job} Levels",
            "category": ["Class Level", "DOW/DOM"],
            "count": max_level / 5,
            "useful": True,
        })
    for job in DOH:
        item_table.append({
            "name": f"5 {job} Levels",
            "category": ["Class Level", "DOH"],
            "count": max_level / 5,
            "filler": True,
        })
    for job in DOL:
        item_table.append({
            "name": f"5 {job} Levels",
            "category": ["Class Level", "DOL"],
            "count": max_level / 5,
            "filler": True,
        })

    return item_table

# NOTE: Progressive items are not currently supported in Manual. Once they are,
#       this hook will provide the ability to meaningfully change those.
def after_load_progressive_item_file(progressive_item_table: list) -> list:
    return progressive_item_table

# called after the locations.json file has been loaded, before any location loading or processing has occurred
# if you need access to the locations after processing to add ids, etc., you should use the hooks in World.py
def after_load_location_file(location_table: list) -> list:
    #add FATE locations
    fate_list = []
    fate_zones = {
        "MLN": [3,3],
        "LLN": [3,3],
        "ELN": [30,30],
        "WLN": [10,10,],
        "ULN": [20,20],
        "OLN": [30,30],

        "CS": [4,4],
        "ES": [11,11],
        "SS": [21,21],
        "NS": [3,3],

        "CT": [5,5],
        "WT": [5,5],
        "ET": [15,15],
        "ST": [25,26],
        "NT": [49,49],

        "CCH": [35,35],
        "CWH": [50, 130],

        "MD": [44,44],

        "TSC": [50, 130],
        "AL": [59, 145],

        "TDF": [52, 130],
        "TCM": [54, 130],
        "TDH": [58, 145],

        "TF": [60,0],
        "TP": [60,0],
        "TL": [69,0],

        "TRS": [62],
        "Y": [64],
        "TAS": [65],

        "L": [70],
        "K": [70],
        "IM": [72],
        "AA": [76],
        "TRG": [74],
        "TT": [79]
    }

    for key in list(fate_zones.keys()):
        level = fate_zones[key][0]
        #ilvl = fate_zones[key][1]
        fate_list.append(create_FATE_location(1,key,level))
        fate_list.append(create_FATE_location(2,key,level))
        fate_list.append(create_FATE_location(3,key,level))
        fate_list.append(create_FATE_location(4,key,level))
        fate_list.append(create_FATE_location(5,key,level))

    location_table.extend(fate_list)
    location_table.extend(duty_locations)
    location_table.extend(ocean_fishing())
    return location_table

# called after the locations.json file has been loaded, before any location loading or processing has occurred
# if you need access to the locations after processing to add ids, etc., you should use the hooks in World.py
def after_load_region_file(region_table: dict) -> dict:
    return region_table

def create_FATE_location(number, key, lvl):
    return {
            "name": short_long[key] + ": FATE #" + str(number),
            "region": short_long[key],
            "category": ["FATEs", short_long[key]],
            "requires": "|$anyClassLevel:" + str(lvl) + "|"
        }

# called after the categories.json file has been loaded
def after_load_category_file(category_table: dict) -> dict:
    return category_table

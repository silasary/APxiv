from math import ceil

import csv
import pkgutil

def generate_duty_list():
    duty_list = []

    dutyreader = csv.reader(pkgutil.get_data(__name__, "duties.csv").decode().splitlines(), delimiter=',', quotechar='|')

    for row in dutyreader:
        if row[0] not in ["", "Name", "ARR", "HW", "STB", "SHB"]:
            requires_str = "|10 Equip Levels:" + str(ceil(int(row[2])/10)) + "| and |" + row[4] + " Access:1|"
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
    
    for job in classes:
        item_table.append({
            "name": f"5 {job} Levels",
            "category": ["Class Level"],
            "count":18,
            "progression": True,
        })
    for job in DOH:
        item_table.append({
            "name": f"5 {job} Levels",
            "category": ["Class Level"],
            "count":18,
            "progression": True,
        })
    for job in DOL:
        item_table.append({
            "name": f"5 {job} Levels",
            "category": ["Class Level"],
            "count":18,
            "progression": True,
        })
    
    return item_table

# NOTE: Progressive items are not currently supported in Manual. Once they are,
#       this hook will provide the ability to meaningfully change those.
def after_load_progressive_item_file(progressive_item_table: list) -> list:
    return progressive_item_table

# called after the locations.json file has been loaded, before any location loading or processing has occurred
# if you need access to the locations after processing to add ids, etc., you should use the hooks in World.py
def after_load_location_file(location_table: list) -> list:
    location_table.extend(duty_locations)
    return location_table

# called after the locations.json file has been loaded, before any location loading or processing has occurred
# if you need access to the locations after processing to add ids, etc., you should use the hooks in World.py
def after_load_region_file(region_table: dict) -> dict:
    return region_table

# called after the categories.json file has been loaded
def after_load_category_file(category_table: dict) -> dict:
    return category_table

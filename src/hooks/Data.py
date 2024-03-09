import csv
import json
import pkgutil

TANKS = ["PLD","WAR","DRK","GNB"]
HEALERS = ["WHM","SCH","AST","SGE"]
MELEE = ["MNK","DRG","NIN","SAM","RPR"]
CASTER = ["BLM","SMN","RDM",]
RANGED = ["BRD","MCH","DNC"]
DOH = ["CRP", "BSM", "ARM", "GSM", "LTW", "WVR", "ALC", "CUL"]

bonus_regions = {}

fate_zones = {
    "Middle La Noscea": [3,3],
    "Lower La Noscea": [3,3],
    "Eastern La Noscea": [30,30],
    "Western La Noscea": [10,10,],
    "Upper La Noscea": [20,20],
    "Outer La Noscea": [30,30],

    "Central Shroud": [4,4],
    "East Shroud": [11,11],
    "South Shroud": [21,21],
    "North Shroud": [3,3],

    "Central Thanalan": [5,5],
    "Western Thanalan": [5,5],
    "Eastern Thanalan": [15,15],
    "Southern Thanalan": [25,26],
    "Northern Thanalan": [49,49],

    "Coerthas Central Highlands": [35,35],
    "Coerthas Western Highlands": [50, 130],

    "Mor Dhona": [44,44],

    "The Sea of Clouds": [50, 130],
    "Azys Lla": [59, 145],

    "The Dravanian Forelands": [52, 130],
    "The Churning Mists": [54, 130],
    "The Dravanian Hinterlands": [58, 145],

    "The Fringes": [60,0],
    "The Peaks": [60,0],
    "The Lochs": [69,0],

    "The Ruby Sea": [62],
    "Yanxia": [64],
    "The Azim Steppe": [65],

    "Lakeland": [70],
    "Kholusia": [70],
    "Il Mheg": [72],
    "Amh Araeng": [76],
    "The Rak'tika Greatwood": [74],
    "The Tempest": [79],

    "Labyrinthos": [80],
    "Thavnair": [80],
    "Garlemald": [82],
    "Mare Lamentorum": [83],
    "Elpis": [86],
    "Ultima Thule": [88],
}

def generate_duty_list():
    duty_list = []
    difficulties = ["Normal", "Extreme", "Savage", "Endgame"]
    dutyreader = csv.reader(pkgutil.get_data(__name__, "duties.csv").decode().splitlines(), delimiter=',', quotechar='|')

    for row in dutyreader:
        row = [x.strip() for x in row]
        if row[0] not in ["", "Name", "ARR", "HW", "STB", "SHB", "EW"]:
            requires_str = "{anyClassLevel(" + row[2] + ")}"
            requires_str += (" and |" + row[7] + "|") if  (row[7] != "") else ""
            location = {
                    "name": row[0],
                    "region": row[4],
                    "category": [row[1], row[4]],
                    "requires": requires_str,
                    #"level" : row[3]
                    "party" : row[5],
                    "diff" : difficulties.index(row[6]),
                }
            if row[4] == "Gangos":
                location["category"].append("Bozja")
            duty_list.append(location)

    return duty_list

duty_locations = generate_duty_list()

def generate_fate_list():
    fate_list = []
    missing_fatesanity_zones = fate_zones.copy()
    fatereader = csv.reader(pkgutil.get_data(__name__, "fates.csv").decode().splitlines(), delimiter=',', quotechar='|')

    for row in fatereader:
        row = [x.strip() for x in row]
        if row[0] not in ["", "Name", "ARR", "HW", "STB", "SHB","EW"]:
            name = row[0]
            level = int(row[1])

            if row[2] == 'The Firmament':
                name += " (FETE)"
                fate_list.append(
                    {
                        "name": name,
                        "region": row[2],
                        "category": ["FATEsanity", row[2]],
                        "requires": "{anyCrafterLevel(" + str(level - 5) + ")}",
                        "level" : row[1],
                        "filler": True,
                    }
                )
                continue

            if "(FATE)" not in name:
                name += " (FATE)"

            location = {
                    "name": name,
                    "region": row[2],
                    "category": ["FATEsanity", row[2]],
                    "requires": "",
                    "level" : row[1],
                }
            if level > 5:
                location["requires"] = "{anyClassLevel(" + str(level - 5) + ")}"
            # if level > 30:
            #     location["filler"] = True

            fate_list.append(location)
            # remove generic FATEs from fate_zones if they exist
            if row[2] in missing_fatesanity_zones:
                missing_fatesanity_zones.pop(row[2])

    if missing_fatesanity_zones:
        # This is hacky, but it lets me slowly scrape the wiki for FATEs without abusing the API
        key = list(missing_fatesanity_zones.keys())[0]
        from . import wiki_scraper
        import os
        additional = wiki_scraper.find_fates(key)
        fates_path = os.path.join(os.path.dirname(__file__), 'fates.csv')
        with open(fates_path, 'a', newline='') as csvfile:
            writer = csv.writer(csvfile, delimiter=',', quotechar='|', quoting=csv.QUOTE_MINIMAL)
            for line in additional:
                writer.writerow(line.split(','))

    for key in list(fate_zones.keys()):
        level = fate_zones[key][0]
        #ilvl = fate_zones[key][1]
        fate_list.append(create_FATE_location(1,key,level))
        fate_list.append(create_FATE_location(2,key,level))
        fate_list.append(create_FATE_location(3,key,level))
        fate_list.append(create_FATE_location(4,key,level))
        fate_list.append(create_FATE_location(5,key,level))

    return fate_list

def generate_fish_list() -> list[dict]:
    fish = json.loads(pkgutil.get_data(__name__, "fish.json"))
    locations = []
    for name, data in fish.items():
        if len(data['zones']) > 1:
            # cry
            region = name
            bonus_regions[name] = {
                "entrance_rules": {k:v for k,v in data['zones'].items()}
            }
            pass
        else:
            region = next(iter(data['zones'].keys()))

        locations.append({
            "name": name,
            "category": ['Bait', "fishsanity"],
            "region": region
        })
    return locations


def generate_bait_list() -> list[dict]:
    bait = json.loads(pkgutil.get_data(__name__, "bait.json"))
    items = []
    for name, data in bait.items():
        if data.get('mooch'):
            continue
        items.append({
            "name": name,
            "progression": True,
            "category": ['Bait', "fishsanity"]
        })
    return items

# called after the items.json file has been loaded, before any item loading or processing has occurred
# if you need access to the items after processing to add ids, etc., you should use the hooks in World.py
def after_load_item_file(item_table: list) -> list:
    item_table.extend(generate_bait_list())
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
    max_blu = 80

    for job in classes:
        n = max_level / 5
        if job == "BLU":
            n = max_blu / 5

        item_table.append({
            "name": f"5 {job} Levels",
            "category": ["Class Level", "DOW/DOM"],
            "count": n,
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
    location_table.extend(generate_fate_list())
    location_table.extend(duty_locations)
    location_table.extend(ocean_fishing())
    location_table.extend(generate_fish_list())

    return location_table

# called after the locations.json file has been loaded, before any location loading or processing has occurred
# if you need access to the locations after processing to add ids, etc., you should use the hooks in World.py
def after_load_region_file(region_table: dict) -> dict:
    region_table.update(bonus_regions)
    return region_table

def create_FATE_location(number: int, key: str, lvl: int):
    location = {
            "name": key + ": FATE #" + str(number),
            "region": key,
            "category": ["FATEs", key],
            "requires": "",
        }
    if lvl > 0:
        location["requires"] = "{anyClassLevel(" + str(lvl) + ")}"
    if lvl > 30 and number > 2:
        location["filler"] = True
    return location

def ocean_fishing():
    indigo_route = ["Rhotano Sea", "Bloodbrine Sea", "Rothlyt Sound", "Northern Strait of Merlthor"]
    ruby_route = ["Ruby Sea", "One River"]

    locations = []
    for route in indigo_route:
        locations.append({
            "name": "Ocean Fishing: " + route,
            "region": "Ocean Fishing",
            "category": ["Ocean Fishing", "Indigo Route"],
            "requires": "|5 FSH Levels:1|"
        })
    for route in ruby_route:
        locations.append({
            "name": "Ocean Fishing: " + route,
            "region": "Ocean Fishing",
            "category": ["Ocean Fishing", "Ruby Route"],
            "requires": "|5 FSH Levels:12| and |Kugane Access:1|"  # Level 60, because why not
        })
    return locations

# called after the categories.json file has been loaded
def after_load_category_file(category_table: dict) -> dict:
    return category_table

# called when an external tool (eg Univeral Tracker) ask for slot data to be read
# use this if you want to restore more data
def hook_interpret_slot_data(world, player: int, slot_data: dict[str, any]):
    prog_classes = slot_data.get("prog_classes", [])
    if not prog_classes:
        prog_classes = TANKS + HEALERS + MELEE + CASTER + RANGED + DOH + ["FSH"]

    for job in prog_classes:
        world.item_name_to_item["5 " + job + " Levels"]["progression"] = True

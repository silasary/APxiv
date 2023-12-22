import csv
import pkgutil

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

    dutyreader = csv.reader(pkgutil.get_data(__name__, "duties.csv").decode().splitlines(), delimiter=',', quotechar='|')

    for row in dutyreader:
        row = [x.strip() for x in row]
        if row[0] not in ["", "Name", "ARR", "HW", "STB", "SHB", "EW"]:
            requires_str = "|$anyClassLevel:" + row[2] + "|"
            requires_str += (" and |" + row[7] + "|") if  (row[7] != "") else ""
            duty_list.append(
                {
                    "name": row[0],
                    "region": row[4],
                    "category": [row[1], row[4]],
                    "requires": requires_str,
                    #"level" : row[3]
                    "party" : row[5],
                    "diff" : row[6],
                }
            )

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
            if "(FATE)" not in name:
                name += " (FATE)"
            fate_list.append(
                {
                    "name": name,
                    "region": row[2],
                    "category": ["FATEsanity", row[2]],
                    "requires": "|$anyClassLevel:" + str(int(row[1]) - 5) + "|",
                    "level" : row[1]
                }
            )
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
        # "CRP",
        # "BSM",
        # "ARM",
        # "GSM",
        # "LTW",
        # "WVR",
        # "ALC",
        # "CUL",
    ]

    # gatherers
    DOL = [
        # "MIN",
        # "BTN",
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
    location_table.extend(generate_fate_list())
    location_table.extend(duty_locations)
    location_table.extend(ocean_fishing())

    return location_table

# called after the locations.json file has been loaded, before any location loading or processing has occurred
# if you need access to the locations after processing to add ids, etc., you should use the hooks in World.py
def after_load_region_file(region_table: dict) -> dict:
    return region_table

def create_FATE_location(number, key, lvl):
    return {
            "name": key + ": FATE #" + str(number),
            "region": key,
            "category": ["FATEs", key],
            "requires": "|$anyClassLevel:" + str(lvl) + "|"
        }

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

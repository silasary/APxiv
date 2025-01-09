"""
Generates static data files from external sources
"""
import csv
import re
import json
import os
import requests
import functools

import yaml
import bs4

NOT_IN_FISHING_GUIDE = [
    "Deep Velodyna Carp",
    "Appleseed",
    "Arrowhead Snail",
    "Astral Algae",
    "Babycray",
    "Chemically Rich Fish",
    "Chromatic Fish",
    "Copperscale",
    "Cosmic Sponge",
    "Crawling Cog",
    "Crimson Copperfish",
    "Dalan's Claw",
    "Dragon's Delight",
    "Dusk Scallop",
    "Elysian Nudibranch",
    "Elysian Stickleback",
    "Fatty Eel",
    "Fish Offering",
    "Fishy Fish",
    "Flagon Clam",
    "Fresh Seaweed",
    "Gigas Catfish",
    "Glowfish",
    "Granite Hardscale",
    "Grass Shrimp",
    "Greasy Strangler",
    "Invisible Catfish",
    "Karellian Fishy Fish",
    "Khaal Crab",
    "Leatherscale",
    "Magma Eel",
    "Meteoric Bonito",
    "Methane Puffer",
    "Moon Oyster",
    "Mossy Tortoise",
    "Nhaama's Claw",
    "Nondescript Fish",
    "Paraichthyoid",
    "Plump Trout",
    "Rainbow Killifish",
    "Saltwater Fish",
    "Seaweed Snapper",
    "Shimmershell",
    "Silky Cosmocoral",
    "Soggy Alien Kelp",
    "Spearhead Snail",
    "Spikefish",
    "Splendid Clawbow",
    "Splendid Cockle",
    "Splendid Diamondtongue",
    "Splendid Egg-bearing Trout",
    "Splendid Eryops",
    "Splendid Larva",
    "Splendid Magmatongue",
    "Splendid Mammoth Shellfish",
    "Splendid Night's Bass",
    "Splendid Pipira",
    "Splendid Piranha",
    "Splendid Pirarucu",
    "Splendid Poison Catfish",
    "Splendid Pondfrond",
    "Splendid Robber Crab",
    "Splendid Shellfish",
    "Splendid Silver Kitten",
    "Splendid Spiralshell",
    "Splendid Sponge",
    "Splendid Treescale",
    "Splendid Trout",
    "Stellar Herring",
    "Steppe Barramundi",
    "Steppe Sweetfish",
    "Sticky Fingers",
    "Sunshell",
    "Supremest Crustacean",
    "Water Fan",
    "Weird Fish",
    "Wildlife Sample",
    "Zagas A'khaal",
]

@functools.lru_cache
def teamcraft_json(filename: str) -> dict | list:
    print(f"Fetching {filename}.json from Teamcraft repo")
    return requests.get(f"https://raw.githubusercontent.com/ffxiv-teamcraft/ffxiv-teamcraft/refs/heads/staging/libs/data/src/lib/json/{filename}.json").json()

@functools.lru_cache
def datamining_csv(filename: str) -> list:
    print(f"Fetching {filename}.csv from datamining repo")
    text = requests.get(f"https://raw.githubusercontent.com/xivapi/ffxiv-datamining/refs/heads/master/csv/{filename}.csv").content.decode('utf-8-sig')
    lines = text.splitlines()
    data = {}
    names = []
    for line in csv.DictReader(lines):
        if line['key'] == '#':
            names = {v: k for k, v in line.items()}
            continue
        data[line['key']] = line
        for k, v in names.items():
            if k:
                data[line['key']][k] = line[v]
    return data

def find_fates(zone: str) -> list[str]:
    print('Finding fates for zone: ' + zone)
    url = f"https://ffxiv.consolegameswiki.com/mediawiki/api.php?action=ask&query=[[Category:Fates]]%20[[Located%20in::{zone}]]%20[[Is%20event%20fate::false]]|?Has%20FATE%20level|?Is retired content|sort%3DHas FATE level,&format=json&api_version=3"
    data = requests.get(url).json()
    fates = []
    for page in data["query"]["results"]:
        name = list(page.keys())[0]
        level = page[name]['printouts']['Has FATE level'][0]
        line = name.replace(',','') + "," + str(level) + ',' + zone
        if page[name]['printouts']['Is retired content']:
            continue
        fates.append(line)
    print(f"Found {len(fates)} fates for zone {zone}")
    offset = data.get('query-continue-offset')
    if offset:
        print("!!! More fates to fetch !!!")
    return fates

# def find_fishing_spots() -> None:
#     baseurl = "https://ffxiv.consolegameswiki.com/mediawiki/api.php?action=ask&query=[[Category:Fishing_log]]|?Gives resource|?Has fishing log level|?Located in|?Bait used|sort=Has fishing log level|offset={0}&format=json&api_version=3"
#     offset = 0
#     fishing_spots = []
#     f = open(os.path.join(os.path.dirname(__file__), 'fishing_spots.csv'), 'w')
#     while offset is not None:
#         url = baseurl.format(offset)
#         print(f'fetching {url}')
#         data = requests.get(url).json()
#         for page in data["query"]["results"]:
#             name = list(page.keys())[0]
#             if name == 'Fishing Log':
#                 continue
#             level = page[name]['printouts']['Has fishing log level'][0]
#             fish = [f['fulltext'] for f in page[name]['printouts']['Gives resource']]
#             bait = [f['fulltext'] for f in page[name]['printouts']['Bait used']]
#             region = [f['fulltext'] for f in page[name]['printouts']['Located in']][0]
#             line = name + ',' + str(level) + ',' + region + ',"' + ','.join(fish) + '","' + ','.join(bait) + '"'
#             f.write(line + '\n')
#             fishing_spots.append(line)
#         offset = data.get('query-continue-offset')
#     f.close()

def load_bait_paths() -> dict[str, dict[str, list[str]]]:
    path = data_path('fish_bait.yaml')
    if os.path.exists(path):
        with open(path, 'r') as f:
            return yaml.safe_load(f)
    return {}

def scrape_bell():
    all_fish = {}
    bait_paths = load_bait_paths()
    url = 'https://www.garlandtools.org/bell/fish.js'
    js = requests.get(url).text.replace('\n', ' ')
    data = re.findall(r'gt.bell.(\w+) = (.*?);', js)
    bait = json.loads(data[0][1])
    fish = json.loads(data[1][1])
    for f in fish:
        if f['zone'] == 'Eulmore - The Buttress':
            f['zone'] = 'Eulmore'
        if f['zone'] == 'The Diadem':
            continue
        name = f["name"]
        if name in NOT_IN_FISHING_GUIDE:
            continue
        if name not in all_fish:
            all_fish[name] = {
                'name': f['name'],
                'id': f['id'],
                'zones': {},
                'lvl': f['lvl'],
                'category': f['category'],
                'bigfish': f['rarity'] > 1,
                'folklore': f.get('folklore'),
                'timed': f.get('weather') or f.get('during'),
                }
        all_fish[name]['zones'][f['zone']] = [c[0] for c in f['baits']]
        bait_paths.setdefault(name, {})
        for c in f['baits']:
            if c[0] not in bait_paths[name].setdefault(f['zone'], []):
                bait_paths[name][f['zone']].append(c[0])

    with open(data_path('fish.json'), 'w', newline='') as h:
        json.dump(all_fish, h, indent=1)
    with open(data_path('bait.json'), 'w', newline='') as h:
        json.dump(bait, h, indent=1)
    with open(data_path('fish_bait.yaml'), 'w', newline='') as h:
        yaml.dump(bait_paths, h)

def lookup_item_name(id: int | str) -> str | bool:
    items = teamcraft_json('items')
    if str(id) not in items:
        return False
    return items[str(id)]['en']

def lookup_rarity(item_id: int) -> int:
    items = teamcraft_json('rarities')
    return items.get(str(item_id), 0)

def lookup_fish(id: int | str) -> dict:
    params = teamcraft_json('fish-parameter')
    fishdata = params[str(id)]
    fish = {
        'name': lookup_item_name(fishdata['itemId']),
        'id': int(id),
        'zones': {},
        'lvl': fishdata['level'],
    }
    # if fishdata.get('recordType'):
    #     fish['category'] = fishdata['recordType']  # I think this is the category
    bigfish = lookup_rarity(fishdata['itemId']) > 1
    if bigfish:
        fish['bigfish'] = True
    if fishdata.get('folklore'):
        fish['folklore'] = fishdata['folklore']
    timed = fishdata.get('timed') or fishdata.get('weathered') or fishdata.get('during')
    if timed:
        fish['timed'] = timed
    if fishdata.get('stars'):
        fishdata['stars'] = fishdata['stars']
    return fish

def scrape_teamcraft():
    all_fish = {}
    fish_ids: list[int] = teamcraft_json('fishes')
    fish_ids.sort()
    for fish_id in fish_ids:
        fish = lookup_fish(fish_id)
        name = fish['name']
        if name in NOT_IN_FISHING_GUIDE or name is False:
            continue
        all_fish.setdefault(name, fish)

    for hole in teamcraft_json('fishing-spots'):
        zone = datamining_csv('PlaceName')[str(hole['placeId'])]
        place_name = zone['Name']
        for fish_id in hole['fishes']:
            name = lookup_item_name(fish_id)
            if name in NOT_IN_FISHING_GUIDE:
                continue
            fish = lookup_fish(fish_id)
            all_fish.setdefault(name, fish)
            fish.setdefault('zones', {})
            all_fish[name]['zones'][place_name] = []  #  [c['name'] for c in fish['bait']]  # TODO:  Determine bait
    with open(data_path('fish.json'), 'w', newline='') as h:
        json.dump(all_fish, h, indent=1)


def tribal_fish():
    with open(data_path('fish.json'), 'r', newline='') as h:
        all_fish = json.load(h)
    offset = 0
    url = "https://ffxiv.consolegameswiki.com/mediawiki/api.php?action=ask&query=[[Category:Seafood]]|?Has%20game%20description|offset={0}&format=json&api_version=3"
    while offset is not None:
        data = requests.get(url.format(offset)).json()
        for page in data["query"]["results"]:
            name = list(page.keys())[0]
            desc = page[name]['printouts']['Has game description'][0]
            if '※Only for use' in desc:
                if name in all_fish:
                    all_fish[name]['tribal'] = True
                # print(name)
            if '※Not included' in desc:
                if name in all_fish:
                    all_fish[name]['tribal'] = True
                    # print(name)
        offset = data.get('query-continue-offset')
    with open(data_path('fish.json'), 'w', newline='') as h:
        json.dump(all_fish, h, indent=1)

def combine_lists(a: list, b: list) -> list:
    return list(set(a + b))

def apply_bait() -> None:
    with open(data_path('bait.json'), 'r', newline='') as h:
        bait_data = json.load(h)
    zoneless = []
    baitless = []
    with open(data_path('fish.json'), 'r', newline='') as h:
        all_fish = json.load(h)
    bait_paths = load_bait_paths()
    for name, fish in all_fish.items():
        if 'Limsa Lominsa Lower Decks' in fish['zones']:
            fish['zones']['Limsa Lominsa'] = combine_lists(fish['zones'].get('Limsa Lominsa', []), fish['zones']['Limsa Lominsa Lower Decks'])
            del fish['zones']['Limsa Lominsa Lower Decks']
        if 'Limsa Lominsa Upper Decks' in fish['zones']:
            fish['zones']['Limsa Lominsa'] = combine_lists(fish['zones'].get('Limsa Lominsa', []), fish['zones']['Limsa Lominsa Upper Decks'])
            del fish['zones']['Limsa Lominsa Upper Decks']

        for zone, baits in bait_paths.get(name, {}).items():
            if len(baits) > 1 and 'Versatile Lure' in baits:
                baits.remove('Versatile Lure')
            if baits:
                fish['zones'][zone] = baits
            else:
                print(f"No bait for {name} in {zone}")
            for bait in baits:
                if bait not in bait_data:
                    bait_data[bait] = {}
        if not fish['zones']:
            # print(f"No zones for {name}")
            zoneless.append(name)
            continue
        all_bait = []
        for zone in fish['zones']:
            all_bait += fish['zones'][zone]
        if not all_bait:
            print(f"No bait for {name}")
            baitless.append(name)

    with open(data_path('fish.json'), 'w', newline='') as h:
        json.dump(all_fish, h, indent=1)
    with open(data_path('zoneless.yaml'), 'w', newline='') as h:
        yaml.dump(zoneless, h, indent=1)
    with open(data_path('baitless.yaml'), 'w', newline='') as h:
        yaml.dump(baitless, h, indent=1)

def fill_missing_bait() -> None:
    with open(data_path('baitless.yaml'), 'r', newline='') as h:
        baitless = yaml.safe_load(h)
    updated = False
    # TODO: Carby Plushy has data for Big Fish, but not the normal fish
    # https://raw.githubusercontent.com/icykoneko/ff14-fish-tracker-app/refs/heads/master/private/fishData.yaml

    # Cat Became Hungry has all fish, but it's HTML and will need to be scraped with bs4
    # https://en.ff14angler.com/

    # Console Games Wiki has everything, but it's inconsistent across pages and is only really useful if I want to hand-enter data

    if updated:
        apply_bait()


def data_path(filename: str) -> str:
    return os.path.join(os.path.dirname(os.path.dirname(__file__)), 'data', filename)

if __name__ == "__main__":
    # scrape_bell()
    scrape_teamcraft()
    tribal_fish()
    apply_bait()


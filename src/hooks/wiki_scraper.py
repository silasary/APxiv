"""
Generates static data files from external sources
"""
import csv
import dataclasses
import functools
import json
import math
import os
import re
from typing import Literal

import bs4
import ratelimit
import requests
import yaml

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
    # Splendid tools
    "Forgiven Melancholy",
    "Ronkan Bullion",
    "Allagan Hunter",
    "Petal Shell",
    "Flintstrike",
    "Pickled Pom",
    "Platinum Seahorse",
    "Clavekeeper",
    "Mirror Image",
    "Spangled Pirarucu",
    "Gold Dustfish",
    "Forgiven Melancholy",
    "Oil Slick",
    "Gonzalo's Grace",
    "Deadwood Shadow",
    "Golding",
    "Ronkan Bullion",
    "Little Bounty",
    "Saint Fathric's Face",

    # Treasure maps
    "Timeworn Kumbhiraskin Map",
]

@functools.lru_cache
def teamcraft_json(filename: str) -> dict | list:
    print(f"Fetching {filename}.json from Teamcraft repo")
    return requests.get(f"https://raw.githubusercontent.com/ffxiv-teamcraft/ffxiv-teamcraft/refs/heads/staging/libs/data/src/lib/json/{filename}.json").json()

@functools.lru_cache
def datamining_csv(filename: str, key = "#") -> dict[str, dict[str, str]]:
    print(f"Fetching {filename}.csv from datamining repo")
    text = requests.get(f"https://raw.githubusercontent.com/xivapi/ffxiv-datamining/refs/heads/master/csv/en/{filename}.csv").content.decode('utf-8-sig')
    lines = text.splitlines()
    data: dict[str, dict[str, str]] = {}
    for line in csv.DictReader(lines):
        data[line[key]] = line
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

def load_all_fish():
    with open(data_path('fish.json'), 'r', newline='') as h:
        all_fish = json.load(h)
    return all_fish

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

@dataclasses.dataclass
class BaitInfo:
    itemId: int
    spot: int
    baitId: int
    aLure: int
    mLure: int
    occurences: int

    @property
    def bait_name(self) -> str:
        return lookup_item_name(self.baitId) or f"Unknown bait {self.baitId}"

    @property
    def spot_info(self) -> dict | None:
        spots = load_fishing_spots()
        spot = next((s for s in spots if s['id'] == self.spot), None)
        return spot

    @property
    def fish_name(self) -> str:
        return lookup_item_name(self.itemId) or f"Unknown fish {self.itemId}"



def load_bait_paths() -> dict[str, dict[str, list[str]]]:
    path = data_path('fish_bait.yaml')
    if os.path.exists(path):
        with open(path, 'r') as f:
            return yaml.safe_load(f)
    return {}

def lookup_item_name(id: int | str) -> str | Literal[False]:
    items = teamcraft_json('items')
    if str(id) not in items:
        return False
    return items[str(id)]['en']

def query_gubal_graphql(operation_name: str, query: str, variables: dict = {}) -> dict:
    url = "https://gubal.ffxivteamcraft.com/graphql"
    response = requests.post(url, json={'operationName': operation_name, 'query': query, 'variables': variables})
    if response.status_code != 200:
        raise Exception(f"GraphQL query failed with status code {response.status_code}: {response.text}")
    return response.json()

@ratelimit.sleep_and_retry
@ratelimit.limits(calls=10, period=5)
def query_baits_per_fish_per_spot(fish_id: int):
    print(f"Querying gubal for baits per fish per spot for fish ID {fish_id}")
    query = """
query BaitsPerFishPerSpotQuery($fishId: Int, $spotId: Int, $misses: Int, $mLureMax: Int, $aLureMax: Int, $mLureMin: Int, $aLureMin: Int) {
  baits: baits_per_fish_per_spot(
    where: {spot: {_eq: $spotId}, itemId: {_eq: $fishId, _gt: $misses}, aLure: {_gte: $aLureMin, _lte: $aLureMax}, mLure: {_gte: $mLureMin, _lte: $mLureMax}, occurences: {_gt: 1}}
  ) {
    itemId
    spot
    baitId
    aLure
    mLure
    occurences
  }
  mooches: baits_per_fish_per_spot(
    where: {spot: {_eq: $spotId}, baitId: {_eq: $fishId, _gt: $misses}, aLure: {_gte: $aLureMin, _lte: $aLureMax}, mLure: {_gte: $mLureMin, _lte: $mLureMax}, occurences: {_gt: 1}}
  ) {
    itemId
    spot
    baitId
    aLure
    mLure
    occurences
  }
}"""
    variables = {
        "fishId": fish_id,
    }
    response = query_gubal_graphql("BaitsPerFishPerSpotQuery", query, variables)
    baits = [BaitInfo(**bait) for bait in response['data']['baits']]
    mooches = [BaitInfo(**bait) for bait in response['data']['mooches']]
    return {
        "baits": baits,
        "mooches": mooches,
    }



@functools.lru_cache
def lookup_item_by_name(name: str) -> dict | None:
    items = datamining_csv('Item')
    for item in items.values():
        if item['Name'] == name:
            return item
        elif item['Singular'].lower() == name.lower():
            return item
    return None

def lookup_item_ui_category(cat_id: int | str) -> str | None:
    categories = datamining_csv('ItemUICategory')
    return categories.get(str(cat_id), {}).get('Name')


def lookup_rarity(item_id: int) -> int:
    items = teamcraft_json('rarities')
    return items.get(str(item_id), 0)

def lookup_fish(id: int | str) -> dict:
    params = teamcraft_json('fish-parameter')
    fishdata = params.get(str(id))
    if not fishdata:
        print(f"Fish with ID {id} not found in Teamcraft data")
        return {}

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
    all_fish = load_all_fish()
    bait_paths = load_bait_paths()
    fish_ids: list[int] = teamcraft_json('fishes')
    fish_ids.sort()
    for fish_id in fish_ids:
        fish = lookup_fish(fish_id)
        name = fish['name']
        if name is False:
            continue
        all_fish.setdefault(name, fish)
        if name in NOT_IN_FISHING_GUIDE:
            all_fish[name]['tribal'] = True

    spots = load_fishing_spots()
    with open(data_path('hole_info.yaml'), 'w', newline='') as h:
        yaml.dump(spots, h, indent=2)

    i = 0

    for hole in spots:
        place_name = hole['zone_name']
        if place_name == "The Diadem":
            continue
        spot_name = hole['hole_name']
        for fish_id in hole['fishes']:
            name = lookup_item_name(fish_id)
            if not name:
                # print(f"Could not find name for fish ID {fish_id}")
                continue
            if name in NOT_IN_FISHING_GUIDE:
                # print(f"Skipping {name} since it's not in the fishing guide")
                continue
            fish = lookup_fish(fish_id)
            if not fish:
                print(f"Could not find fish data for {name} (ID {fish_id})")
                continue

            all_fish.setdefault(name, fish)
            if not bait_paths.setdefault(name, {}) or not bait_paths[name].setdefault(spot_name, []):
                fill_bait_from_teamcraft(all_fish[name], bait_paths)
                i += 1
                if i % 10 == 0:
                    with open(data_path('fish_bait.yaml'), 'w', newline='') as h:
                        yaml.dump(bait_paths, h, indent=1)


    with open(data_path('fish.json'), 'w', newline='') as h:
        json.dump(all_fish, h, indent=1)
    with open(data_path('fish_bait.yaml'), 'w', newline='') as h:
        yaml.dump(bait_paths, h, indent=1)

def fill_bait_from_teamcraft(fish: dict, bait_paths: dict) -> None:
    fish_id = fish['id']
    bait_info = query_baits_per_fish_per_spot(fish_id)
    per_spot: dict[str, list[BaitInfo]] = {}
    # per_zone: dict[str, list[BaitInfo]] = {}
    for bait in bait_info['baits']:
        per_spot.setdefault(bait.spot_info['hole_name'], []).append(bait)
        # per_zone.setdefault(bait.spot_info['zone_name'], []).append(bait)

    for spot, baits in per_spot.items():
        baits = sorted(baits, key=lambda b: b.occurences, reverse=True)
        best_occurences = baits[0].occurences
        # Anything that is less that 5% of the optimal bait is best written off as bad data.
        viable_baits = [b for b in baits if b.occurences >= best_occurences * 0.05]
        if viable_baits[0].bait_name == "Versatile Lure" and len(viable_baits) > 1:
            viable_baits = viable_baits[1:] + [viable_baits[0]]
        # fish['spots'][spot] = [b.bait_name for b in viable_baits]
        bait_paths.setdefault(fish['name'], {})[spot] = [b.bait_name for b in viable_baits]

    # for zone, baits in per_zone.items():
    #     baits = sorted(baits, key=lambda b: b.occurences, reverse=True)
    #     best_occurences = baits[0].occurences
    #     # Anything that is less that 5% of the optimal bait is best written off as bad data.
    #     viable_baits = [b for b in baits if b.occurences >= best_occurences * 0.05]
    #     if viable_baits[0].bait_name == "Versatile Lure" and len(viable_baits) > 1:
    #         viable_baits = viable_baits[1:] + [viable_baits[0]]
    #     fish['logical_bait'][zone] = [b.bait_name for b in viable_baits]

    return


def tribal_fish():
    all_fish = load_all_fish()
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
    all_fish = load_all_fish()
    bait_paths = load_bait_paths()
    spots = {spot['hole_name']: spot for spot in load_fishing_spots()}
    for name, fish in all_fish.items():
        if fish.get('tribal'):
            continue
        if 'The <Emphasis>Endeavor</Emphasis>' in fish['logical_bait']:
            continue

        fish['logical_bait'] = {}
        fish['all_bait'] = {}
        if 'zones' in fish:
            del fish['zones']

        for hole, baits in bait_paths.get(name, {}).copy().items():
            if not baits:
                del bait_paths[name][hole]
                continue
            zone_name = spots[hole]['zone_name']
            # if len(baits) > 1 and 'Versatile Lure' in baits:
            #     baits.remove('Versatile Lure')
            if baits:
                fish['all_bait'][zone_name] = baits
                fish['logical_bait'].setdefault(zone_name, []).append(baits[0])
            else:
                print(f"No bait for {name} in {hole}")
            for bait in baits.copy():
                if isinstance(bait, list):
                    baits.remove(bait)
                    baits.append(bait[0])
                    bait = bait[0]
                info = bait_data.get(bait, {"name": bait})

                if 'id' not in info:
                    item = lookup_item_by_name(bait)
                    if item:
                        info['id'] = int(item['#'])
                    else:
                        print(f"Could not find item ID for bait: {bait}")
                        bait_data[bait] = info
                        continue
                    category = lookup_item_ui_category(item["ItemUICategory"])
                    if category == "Fishing Tackle":
                        pass
                    elif category == "Seafood":
                        info['mooch'] = True
                    else:
                        print(f"!!! Bait {bait} has unexpected category {category} !!!")
                        info['category'] = category
                if info.get('mooch'):
                    found = False
                    mooch = ''
                    for mooch in bait_paths.keys():
                        if mooch.lower() == bait.lower():
                            found = True
                            break
                    if not found:
                        print(f"!!! {name} mooches {bait} but cannot find corresponding fish !!!")
                        bait_data[bait] = info
                        continue
                    mooch_path = bait_paths.setdefault(mooch, {}).setdefault(hole, [])
                    if mooch_path:
                        index = baits.index(bait)
                        # baits.remove(bait)
                        new_bait = mooch_path[0]
                        add = new_bait not in baits
                        if new_bait == "Versatile Lure" and baits:
                            add = False
                        if add:
                            baits[index] = new_bait
                        else:
                            baits.remove(bait)
                if bait not in bait_data and not info.get('mooch'):
                    bait_data[bait] = info


        if not fish['all_bait']:
            # print(f"No zones for {name}")
            zoneless.append(name)
            continue
        all_bait = []
        for zone_name in fish['all_bait']:
            all_bait += fish['all_bait'][zone_name]
        if not all_bait:
            print(f"No bait for {name}")
            baitless.append(name)

    with open(data_path('fish.json'), 'w', newline='') as h:
        json.dump(all_fish, h, indent=1)
    with open(data_path('zoneless.yaml'), 'w', newline='') as h:
        yaml.dump(zoneless, h, indent=1)
    with open(data_path('baitless.yaml'), 'w', newline='') as h:
        yaml.dump(baitless, h, indent=1)
    with open(data_path('bait.json'), 'w', newline='') as h:
        json.dump(bait_data, h, indent=1)
    with open(data_path('fish_bait.yaml'), 'w', newline='') as h:
        yaml.dump(bait_paths, h, indent=1)

def fill_missing_bait() -> None:
    with open(data_path('baitless.yaml'), 'r', newline='') as h:
        baitless = yaml.safe_load(h)
    updated = False
    # TODO: Carby Plushy has data for Big Fish, but not the normal fish
    updated = scrape_carby(baitless) or updated

    # Cat Became Hungry has all fish, but it's HTML and will need to be scraped with bs4
    # https://en.ff14angler.com/
    # updated = scrape_cat(baitless) or updated
    # Console Games Wiki has everything, but it's inconsistent across pages and is only really useful if I want to hand-enter data

    if updated:
        apply_bait()

def scrape_carby(baitless) -> bool:
    if not baitless:
        return False

    updated = False

    all_fish = load_all_fish()
    bait_paths = load_bait_paths()

    fish_parameters = datamining_csv('FishParameter', "Item")
    spots = {spot['id']: spot for spot in load_fishing_spots()}

    carby_text = requests.get('https://raw.githubusercontent.com/icykoneko/ff14-fish-tracker-app/refs/heads/master/private/fishData.yaml').text
    carby_data = yaml.safe_load(carby_text)
    big_fish = {f['name']: f for f in carby_data}

    for fish in baitless:
        if fish not in big_fish:
            continue
        cdata = big_fish[fish]
        item = lookup_item_by_name(fish)
        parameter = fish_parameters.get(item['#'])
        spot = spots.get(int(parameter['FishingSpot']))
        place = datamining_csv('PlaceName')[str(spot['zoneId'])]
        place_name = place['Name']
        if cdata.get('dataMissing', False):
            print(f"Carby is still missing data for {fish}")
            bait_paths.setdefault(fish, {}).setdefault(place_name, [])
            continue
        bait_paths.setdefault(fish, {}).setdefault(place_name, []).extend(cdata['bestCatchPath'])
        baitless.remove(fish)
        updated = True
        pass

    if updated:
        with open(data_path('fish.json'), 'w', newline='') as h:
            json.dump(all_fish, h, indent=1)
        with open(data_path('fish_bait.yaml'), 'w', newline='') as h:
            yaml.dump(bait_paths, h, indent=1)
    return updated

@functools.lru_cache
def load_fishing_spots():
    spots = []
    for spot in teamcraft_json('fishing-spots'):
        spot['hole_name'] = datamining_csv('PlaceName')[str(spot['zoneId'])]['Name']
        spot['zone_name'] = datamining_csv('PlaceName')[str(spot['placeId'])]['Name']
        spots.append(spot)
    return spots

def scrape_cat(baitless) -> bool:
    if not baitless:
        return False
    updated = False
    soup = cat_region_table(10000)
    regions = [o['value'] for o in soup.find(id='select_region').children if isinstance(o, bs4.element.Tag)]

    spots_in_zones, spot_to_id = cat_get_spots(regions)

    all_fish = load_all_fish()
    bait_paths = load_bait_paths()

    for fish in baitless:
        if fish not in all_fish:
            print(f"Fish {fish} not found in fish.json")
            continue
        for zone in all_fish[fish]['zones']:
            # We need to figure out which spots to check, or just check them all
            if zone not in spots_in_zones:
                print(f"Zone '{zone}' not found in Cat Became Hungry")
                continue
            for spot in spots_in_zones[zone]:
                spot_id = spot_to_id[spot]
                data: dict[str, dict[str, float]] = cat_spot_data(spot_id)
                best = (None, 0)
                for bait, stats in data.items():
                    if fish in stats:
                        percent = float(stats[fish])
                        if percent > best[1]:
                            best = (bait, percent)
                if best[0]:
                    name = best[0].title()
                    if name in all_fish:
                        mooch = all_fish[name]['zones'].get(zone, [])
                        if not mooch:
                            print(f"Found mooch for {fish} in {zone}: {name} but no mooch data")
                            continue
                        name = mooch[0]
                    baits = all_fish[fish]['zones'].setdefault(zone, [])
                    if name not in baits:
                        baits.append(name)
                        print(f"Found bait for {fish} in {zone}: {name} ({best[1]}%)")
                        bait_paths.setdefault(fish, {}).setdefault(zone, []).append(name)
                        updated = True
                        with open(data_path('fish_bait.yaml'), 'w', newline='') as h:
                            yaml.dump(bait_paths, h)
    with open(data_path('fish.json'), 'w', newline='') as h:
        json.dump(all_fish, h, indent=1)
    return updated

def cat_get_spots(regions):
    spots_in_zones = {}
    spot_to_id = {}
    for region in regions:
        soup = cat_region_table(region)
        spots = soup.find('div', id='main_contents').find_all('table')[1]
        for spot in spots.find_all('tr'):
            if spot.find('a') is None:
                area_name = spot.string
                continue
            spot_id = spot.find('a')['href'].split('/')[-1]
            spot_name = spot.find('a').text
            spots_in_zones.setdefault(area_name, []).append(spot_name)
            spot_to_id[spot_name] = spot_id

    return spots_in_zones, spot_to_id

@functools.lru_cache(maxsize=None)
@ratelimit.sleep_and_retry
@ratelimit.limits(calls=2, period=5)
def cat_spot_data(spot_id) -> dict[str, dict[str, float]]:
    bait_data = {}
    print(f"Fetching spot {spot_id} from Cat Became Hungry")
    spot = requests.get(f'https://en.ff14angler.com/spot/{spot_id}')
    soup = bs4.BeautifulSoup(spot.text, 'html.parser')
    effective_bait = soup.find(id='effective_bait')
    if effective_bait is None:
        print(f"No effective bait found for spot {spot_id}")
        return bait_data
    rows = effective_bait.find_all('tr')
    headers = [a['title'] for a in rows[0].find_all('a')]
    for row in rows[1:]:
        cells = row.find_all('td')
        bait = (cells[0].find('a') or cells[0].find('span'))['title']  # Bait name
        bait_data[bait] = {}
        for i, cell in enumerate(cells[1:]):
            div = cell.find('div')
            if div is None:
                continue
            percent = float(div.find('canvas')['value'])
            # print(div['title'])
            # print(f"{bait}: {headers[i]} ({percent}%)")
            bait_data[bait][headers[i]] = percent

    return bait_data

@functools.lru_cache
@ratelimit.sleep_and_retry
@ratelimit.limits(calls=2, period=5)
def cat_region_table(spot_id):
    print(f"Fetching spot {spot_id} from Cat Became Hungry")
    spots = requests.get(f'https://en.ff14angler.com/?spot={spot_id}')
    soup = bs4.BeautifulSoup(spots.text, 'html.parser')
    return soup

def data_path(filename: str) -> str:
    return os.path.join(os.path.dirname(os.path.dirname(__file__)), 'data', filename)

def clean_fish():
    all_fish = load_all_fish()
    for fish in all_fish.values():
        to_remove = []
        for zone, baits in fish.get('logical_bait', {}).items():
            if not baits:
                # print(f"Removing {zone} from {fish['name']}")
                to_remove.append(zone)
        for zone in to_remove:
            del fish['logical_bait'][zone]
    with open(data_path('fish.json'), 'w', newline='') as h:
        json.dump(all_fish, h, indent=1)

def sort_fish():
    all_fish = load_all_fish()
    sorted_fish = dict(sorted(all_fish.items(), key=lambda item: item[1].get('id', math.inf)))
    with open(data_path('fish.json'), 'w', newline='') as h:
        json.dump(sorted_fish, h, indent=1)


if __name__ == "__main__":
    scrape_teamcraft()
    tribal_fish()
    apply_bait()
    fill_missing_bait()
    clean_fish()
    sort_fish()


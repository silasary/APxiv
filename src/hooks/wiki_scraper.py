import csv
import re
import json
from dataclasses import dataclass
import os
import requests

def find_fates(zone: str) -> list[str]:
    print('Finding fates for zone: ' + zone)
    url = f"https://ffxiv.consolegameswiki.com/mediawiki/api.php?action=ask&query=[[Category:Fates]]%20[[Located%20in::{zone}]]%20[[Is%20event%20fate::false]]|?Has%20FATE%20level|sort%3DHas FATE level&format=json&api_version=3"
    data = requests.get(url).json()
    fates = []
    for page in data["query"]["results"]:
        name = list(page.keys())[0]
        level = page[name]['printouts']['Has FATE level'][0]
        line = name.replace(',','') + "," + str(level) + ',' + zone
        fates.append(line)
    print(f"Found {len(fates)} fates for zone {zone}")
    return fates

def find_fishing_spots() -> None:
    baseurl = "https://ffxiv.consolegameswiki.com/mediawiki/api.php?action=ask&query=[[Category:Fishing_log]]|?Gives resource|?Has fishing log level|?Located in|?Bait used|sort=Has fishing log level|offset={0}&format=json&api_version=3"
    offset = 0
    fishing_spots = []
    f = open(os.path.join(os.path.dirname(__file__), 'fishing_spots.csv'), 'w')
    while offset is not None:
        url = baseurl.format(offset)
        print(f'fetching {url}')
        data = requests.get(url).json()
        for page in data["query"]["results"]:
            name = list(page.keys())[0]
            if name == 'Fishing Log':
                continue
            level = page[name]['printouts']['Has fishing log level'][0]
            fish = [f['fulltext'] for f in page[name]['printouts']['Gives resource']]
            bait = [f['fulltext'] for f in page[name]['printouts']['Bait used']]
            region = [f['fulltext'] for f in page[name]['printouts']['Located in']][0]
            line = name + ',' + str(level) + ',' + region + ',"' + ','.join(fish) + '","' + ','.join(bait) + '"'
            f.write(line + '\n')
            fishing_spots.append(line)
        offset = data.get('query-continue-offset')
    f.close()

def scrape_bell():
    all_fish = {}
    url = 'https://www.garlandtools.org/bell/fish.js'
    js = requests.get(url).text.replace('\n', ' ')
    data = re.findall(r'gt.bell.(\w+) = (.*?);', js)
    bait = json.loads(data[0][1])
    fish = json.loads(data[1][1])
    for f in fish:
        name = f["name"]
        if name not in all_fish:
            all_fish[name] = {
                'name': f['name'],
                'id': f['id'],
                'zones': {},
                'lvl': f['lvl'],
                'category': f['category'],
                'bigfish': f['rarity'] > 1,
                'folklore': f.get('folklore')
                }
        all_fish[name]['zones'][f['zone']] = [c[0] for c in f['baits']]

    with open(os.path.join(os.path.dirname(__file__), 'fish.json'), 'w', newline='') as h:
        json.dump(all_fish, h)
    with open(os.path.join(os.path.dirname(__file__), 'bait.json'), 'w', newline='') as h:
        json.dump(bait, h)

# @dataclass
# class FishingSpot:


# @dataclass
# class Fish:
#     ItemId: int
#     Name: str
#     FishingSpot: str
#     IsInLog: bool

# def compile_fish() -> None:
#     datamining = r"C:\Users\Clock\projects\ffxiv-datamining"
#     if not os.path.exists(datamining):
#         raise FileNotFoundError()
#     with open(os.path.join(datamining, "FishingSpot.csv")) as f:
#         FishingSpots = csv.reader(f.readlines(), delimiter=',', quotechar='"')

#     with open(os.path.join(datamining, "FishParameter.csv")) as f:
#         FishParameter = csv.reader(f.readlines(), delimiter=',', quotechar='"')


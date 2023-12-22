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

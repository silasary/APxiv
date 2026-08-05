"""
Levequest data scraping: builds src/hooks/leves.csv from datamining CSVs,
cross-checked against consolegameswiki's retired-content property.
"""

import csv
import os
from typing import Callable

import requests

# LeveAssignmentType row ids -> (Type, Class) columns for leves.csv.
# 13-15 are grand company
_LEVE_ASSIGNMENT_TYPES: dict[str, tuple[str, str]] = {
    "1": ("Battlecraft", ""),
    "2": ("Fieldcraft", "MIN"),
    "3": ("Fieldcraft", "BTN"),
    "4": ("Fieldcraft", "FSH"),
    "5": ("Tradecraft", "CRP"),
    "6": ("Tradecraft", "BSM"),
    "7": ("Tradecraft", "ARM"),
    "8": ("Tradecraft", "GSM"),
    "9": ("Tradecraft", "LTW"),
    "10": ("Tradecraft", "WVR"),
    "11": ("Tradecraft", "ALC"),
    "12": ("Tradecraft", "CUL"),
}


def _is_untranslated(name: str) -> bool:
    # Check for CJK character to skip untranslated leves
    # like "508  獲得任務：オーレリアのバラスト袋"
    return ord(name[0]) >= 0x3000


def _normalize_leve_name(name: str) -> str:
    # See for example https://ffxiv.consolegameswiki.com/wiki/And_My_Axe_(Levequest)
    if name.endswith(" (Levequest)"):
        name = name[: -len(" (Levequest)")]
    return name.casefold()


def find_retired_leves() -> set[str]:
    """Fetch the levequests marked 'Is retired content' on consolegameswiki."""
    retired: set[str] = set()
    offset: int | None = 0

    while offset is not None:
        print(f"Fetching retired levequests from wiki (offset {offset})")

        data = requests.get(
            "https://ffxiv.consolegameswiki.com/mediawiki/api.php",
            params={
                "action": "ask",
                "format": "json",
                "api_version": "3",
                "query": f"[[Category:Levequests]]|?Is retired content|limit=500|offset={offset}",
            },
        ).json()

        for page in data["query"]["results"]:
            name = list(page.keys())[0]

            if page[name]["printouts"]["Is retired content"]:
                retired.add(_normalize_leve_name(name))

        offset = data.get("query-continue-offset")

    return retired


def _build_place_expansion_map(
    territory_type: dict[str, dict[str, str]],
) -> dict[str, str]:
    # PlaceName id -> ExVersion. A zone's PlaceName id can appear on multiple
    # TerritoryType rows (e.g. PlaceName 32 "Eastern La Noscea" has 9 rows on
    # the identical Bg path, tagged ExVersion 0/1/3)
    # Keep the lowest ExVersion so leve zones resolve to the original zone's expansion
    result: dict[str, str] = {}

    for tt in territory_type.values():
        place = tt.get("PlaceName", "0")

        if not place or place == "0":
            continue

        ex = tt.get("ExVersion", "0")

        if place not in result or int(ex) < int(result[place]):
            result[place] = ex

    return result


def _write_leves_csv(rows: list[dict[str, str]]) -> None:
    # src/hooks/scrapers/leves.py -> src/hooks/leves.csv
    out_path = os.path.join(os.path.dirname(os.path.dirname(__file__)), "leves.csv")

    with open(out_path, "w", newline="", encoding="utf-8") as h:
        writer = csv.DictWriter(
            h,
            fieldnames=[
                "LeveId",
                "Name",
                "Type",
                "Class",
                "Level",
                "Location",
                "Cost",
                "Expansion",
                "Retired",
            ],
        )

        writer.writeheader()
        writer.writerows(rows)

    by_zone: dict[str, int] = {}

    for row in rows:
        by_zone[row["Location"]] = by_zone.get(row["Location"], 0) + 1

    print(f"Wrote {len(rows)} leves to {out_path} across {len(by_zone)} zones")


def build_leves_csv(
    datamining_csv: Callable[[str], dict[str, dict[str, str]]],
    ex_version_data: dict[str, tuple[str, int, int]],
) -> list[dict[str, str]]:
    """Build levequest data (leves.csv) from datamining CSVs.

    Sheet relationships used here:
      Leve          - LeveAssignmentType picks battle/craft/gather,
                      PlaceNameStartZone is the field zone the leve is undertaken in,
                      AllowanceCost distinguishes normal (1) from large-scale "(L)" leves (10)
      PlaceName     - PlaceName id -> zone display name
      TerritoryType - PlaceName id -> ExVersion (expansion tag)

    leves.csv marks retired leves and does not delete them, to keep ids stable
    """
    print("Building leves.csv from datamining CSVs")

    # Relevant columns: Name, LeveAssignmentType, ClassJobLevel, AllowanceCost, PlaceNameStartZone
    leves = datamining_csv("Leve")
    # Relevant column: Name (zone name)
    place_name = datamining_csv("PlaceName")
    # Relevant columns: PlaceName, ExVersion
    territory_type = datamining_csv("TerritoryType")

    place_to_ex_version = _build_place_expansion_map(territory_type)
    retired_leves = find_retired_leves()

    rows: list[dict[str, str]] = []
    skipped_untranslated = 0
    marked_retired = 0

    for leve in leves.values():
        name = leve["Name"]

        if not name:
            continue

        assignment = _LEVE_ASSIGNMENT_TYPES.get(leve["LeveAssignmentType"])

        if assignment is None:
            continue

        leve_type, leve_class = assignment

        if _is_untranslated(name):
            skipped_untranslated += 1
            continue

        zone_name = place_name.get(leve["PlaceNameStartZone"], {}).get("Name", "")

        if not zone_name:
            continue

        expansion_info = ex_version_data.get(
            place_to_ex_version.get(leve["PlaceNameStartZone"], "0")
        )

        if expansion_info is None:
            continue

        is_retired = _normalize_leve_name(name) in retired_leves
        if is_retired:
            marked_retired += 1

        rows.append(
            {
                "LeveId": leve["#"],
                "Name": name,
                "Type": leve_type,
                "Class": leve_class,
                "Level": leve["ClassJobLevel"],
                "Location": zone_name,
                "Cost": leve["AllowanceCost"],
                "Expansion": expansion_info[0],
                "Retired": "1" if is_retired else "",
            }
        )

    if skipped_untranslated:
        print(f"Skipped {skipped_untranslated} untranslated leves")
    if marked_retired:
        print(f"Marked {marked_retired} leves as retired (per wiki)")

    _write_leves_csv(rows)
    return rows

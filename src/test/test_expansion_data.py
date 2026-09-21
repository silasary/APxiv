import unittest

from ..Data import location_table
from ..hooks.Data import EXPANSION_ORDER


class LocationDataTest(unittest.TestCase):
    def test_location_expansions(self):
        """Make sure that locations have proper expansion data"""
        for location in location_table:
            expansion = location.get("expansion")
            assert expansion is None or expansion in EXPANSION_ORDER, (
                f"Location '{location['name']}' has expansion tag '{expansion}', "
                f"which is not one of {list(EXPANSION_ORDER)}"
            )

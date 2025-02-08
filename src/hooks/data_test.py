from test.bases import WorldTestBase

class FatesanityTest(WorldTestBase):
    game = "Manual_FFXIV_Silasary"

    fatesanity = True

class FishsanityTest(WorldTestBase):
    game = "Manual_FFXIV_Silasary"

    fishsanity = 1

class BigFishsanityTest(WorldTestBase):
    game = "Manual_FFXIV_Silasary"

    fishsanity = 3


class ShortTest(WorldTestBase):
    game = "Manual_FFXIV_Silasary"

    include_dungeons = False
    duty_difficulty = "Normal"
    max_party_size = "Light Party"

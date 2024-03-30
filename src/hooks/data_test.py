from test.bases import WorldTestBase

class FatesanityTest(WorldTestBase):
    game = "Manual_FFXIV_Silasary"

    fatesanity = True

class FishsanityTest(WorldTestBase):
    game = "Manual_FFXIV_Silasary"

    fishsanity = True
    fishsanity_big_fish = True
    fishsanity_timed_fish = True

class ShortTest(WorldTestBase):
    game = "Manual_FFXIV_Silasary"

    include_dungeons = False
    duty_difficulty = "Normal"
    max_party_size = "Light Party"

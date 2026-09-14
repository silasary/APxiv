from worlds.LauncherComponents import Component, Type, components, launch_subprocess


def launch_client(*args):
    from .client import launch as Main
    launch_subprocess(Main, name="Manual FFXIV client")

def add_client_to_launcher() -> None:
    components.append(Component("Final Fantasy XIV Manual Client", func=launch_client, description="Launches the Manual Client for Final Fantasy XIV.",
                                game_name="Final Fantasy XIV", supports_uri=True))
    components.append(Component("Final Fantasy XIV Manual Client", func=launch_client, description="Launches the Manual Client for Final Fantasy XIV.",
                                game_name="Manual_FFXIV_Silasary", supports_uri=True, component_type=Type.HIDDEN))

add_client_to_launcher()

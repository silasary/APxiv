using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using Lumina.Excel.GeneratedSheets;

namespace ArchipelagoXIV
{
    public class DalamudApi
    {
        internal static IDtrBarEntry? logicBar = null;
        internal static IDtrBarEntry? jobBar = null;

        public static void Initialize(IDalamudPluginInterface pluginInterface) => pluginInterface.Create<DalamudApi>();

        [PluginService] public static IAddonLifecycle AddonLifecycle { get; private set; } = null;
        [PluginService] public static IAetheryteList AetheryteList { get; private set; } = null;
        [PluginService] public static IAddonEventManager EventManager { get; private set; } = null;
        [PluginService] public static IBuddyList BuddyList { get; private set; } = null;
        [PluginService] public static IChatGui ChatGui { get; private set; } = null;
        //[PluginService] public static ChatHandlers ChatHandlers { get; private set; } = null;
        [PluginService] public static IClientState ClientState { get; private set; } = null;
        [PluginService] public static ICommandManager CommandManager { get; private set; } = null;
        [PluginService] public static ICondition Condition { get; private set; } = null;
        //[PluginService] public static IConsole Console { get; private set; } = null;
        [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null;
        [PluginService] public static IDataManager DataManager { get; private set; } = null;
        [PluginService] public static IDtrBar DtrBar { get; private set; } = null;
        [PluginService] public static IDutyState DutyState { get; private set; } = null;
        [PluginService] public static IFateTable FateTable { get; private set; } = null;
        [PluginService] public static IFlyTextGui FlyTextGui { get; private set; } = null;
        [PluginService] public static IFramework Framework { get; private set; } = null;
        [PluginService] public static IGameGui GameGui { get; private set; } = null;
        [PluginService] public static IGameInventory GameInventory { get; private set; } = null;
        [PluginService] public static IGameNetwork GameNetwork { get; private set; } = null;
        [PluginService] public static IGamepadState GamePadState { get; private set; } = null;
        [PluginService] public static IJobGauges JobGauges { get; private set; } = null;
        [PluginService] public static IKeyState KeyState { get; private set; } = null;
        [PluginService] public static IObjectTable ObjectTable { get; private set; } = null;
        [PluginService] public static IPartyFinderGui PartyFinderGui { get; private set; } = null;
        [PluginService] public static IPartyList PartyList { get; private set; } = null;
        [PluginService] public static IPluginLog PluginLog { get; private set; } = null;
        [PluginService] public static ISigScanner SigScanner { get; private set; } = null;
        [PluginService] public static ITargetManager TargetManager { get; private set; } = null;
        [PluginService] public static IToastGui ToastGui { get; private set; } = null;


        internal static void Echo(string Text)
        {
            ChatGui.Print(new XivChatEntry
            {
                Message = new SeStringBuilder().AddUiGlow("[AP]", 32).AddText(Text).Build(),
                Type = XivChatType.Echo,
            });
        }

        internal static void PvPTeam(string Text, string sender)
        {
            ChatGui.Print(new XivChatEntry
            {
                Message = new SeStringBuilder().AddText(Text).Build(),
                Type = XivChatType.PvPTeam,
                Name = sender,
            });
        }

        internal static void SetStatusBar(string text)
        {
            logicBar ??= DtrBar.Get("Archipelago");
            logicBar.Text = "" + SeIconChar.EurekaLevel.ToIconChar() + " " + text;
        }
        internal static void SetStatusTooltop(string text)
        {
            logicBar.Tooltip = text;
        }
        internal static void SetJobStatusBar(string text)
        {
            if (text == null)
                return;
            jobBar ??= DtrBar.Get("APJob");
            jobBar.Text = "" + SeIconChar.EurekaLevel.ToIconChar() + " " + text;
        }
        internal static void SetJobTooltop(string text)
        {
            jobBar ??= DtrBar.Get("APJob");
            jobBar.Tooltip = text;
        }
        internal static void ShowToast(string text)
        {
            ToastGui.ShowQuest(text);
        }
        internal static void ShowError(string text)
        {
            ToastGui.ShowError(text);
        }

        public static ClassJob? CurrentClass() => ClientState.LocalPlayer?.ClassJob.GameData;
    }
}

using Dalamud.Data;
using Dalamud.Game.ClientState.Aetherytes;
using Dalamud.Game.ClientState.Buddy;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Gui.PartyFinder;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Gui;
using Dalamud.Game.Libc;
using Dalamud.Game.Network;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.DutyState;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;

namespace ArchipelagoXIV
{
    public class DalamudApi
    {
        private static DtrBarEntry? logicBar = null;
        private static DtrBarEntry? jobBar = null;

        public static void Initialize(DalamudPluginInterface pluginInterface) => pluginInterface.Create<DalamudApi>();

        [PluginService][RequiredVersion("1.0")] public static IAddonLifecycle AddonLifecycle { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IAetheryteList AetheryteList { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IBuddyList BuddyList { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IChatGui ChatGui { get; private set; } = null;
        //[PluginService][RequiredVersion("1.0")] public static ChatHandlers ChatHandlers { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IClientState ClientState { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static ICommandManager CommandManager { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static ICondition Condition { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static DalamudPluginInterface PluginInterface { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IDataManager DataManager { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IDtrBar DtrBar { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IDutyState DutyState { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IAddonEventManager EventManager { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IFateTable FateTable { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IFlyTextGui FlyTextGui { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IFramework Framework { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IGameGui GameGui { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IGameInventory GameInventory { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IGameNetwork GameNetwork { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IGamepadState GamePadState { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IJobGauges JobGauges { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IKeyState KeyState { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static ILibcFunction LibcFunction { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IObjectTable ObjectTable { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IPartyFinderGui PartyFinderGui { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IPartyList PartyList { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static ISigScanner SigScanner { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static ITargetManager TargetManager { get; private set; } = null;
        [PluginService][RequiredVersion("1.0")] public static IToastGui ToastGui { get; private set; } = null;


        internal static void Echo(string Text)
        {
            ChatGui.Print(new XivChatEntry
            {
                Message = new SeStringBuilder().AddText(Text).Build(),
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
    }
}

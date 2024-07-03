using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using ArchipelagoXIV.Windows;
using ArchipelagoXIV.Hooks;
using Dalamud.Plugin.Services;
using Archipelago.MultiClient.Net.Packets;

namespace ArchipelagoXIV
{
    public sealed class Plugin : IDalamudPlugin
    {
        private IDalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }

        private ApState apState { get; init; }

        internal UnlockHooks Hooks { get; }
        internal Events Events { get; }
        internal UIHooks UiHooks { get; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("ArchipelagoXIV");

        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }

        public Plugin(
            IDalamudPluginInterface pluginInterface,
            ICommandManager commandManager
        )
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            DalamudApi.Initialize(pluginInterface);
            Data.Initialize();

            this.apState = new ApState();

            this.Hooks = new UnlockHooks(apState);
            this.Events = new Events(apState);
            this.UiHooks = new UIHooks(apState);

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            ConfigWindow = new ConfigWindow(this, this.apState);
            MainWindow = new MainWindow(this, this.apState);

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);

            this.CommandManager.AddHandler("/ap-connect", new CommandInfo(Connect)
            {
                HelpMessage = "Show Archipelago main window, and connect if we're not connected"
            });


            this.CommandManager.AddHandler("/ap-config", new CommandInfo(ShowConfig)
            {
                HelpMessage = "Archipelago Config"
            });

            this.CommandManager.AddHandler("/ap-disconnect", new CommandInfo(Disconnect)
            {
                HelpMessage = "Disconnect from Archipelago"
            });

            this.CommandManager.AddHandler("/ap", new CommandInfo(Chat)
            {
                HelpMessage = "Send chat messages to your multiworld"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            this.PluginInterface.UiBuilder.OpenMainUi += () => MainWindow.IsOpen = true;
            this.Hooks.Enable();
            this.Events.Enable();
            UiHooks.Enable();
            DalamudApi.Framework.Update += Framework_Update;
            DalamudApi.SetStatusBar("AP Ready");
            DalamudApi.logicBar.OnClick += () => { MainWindow.IsOpen = !MainWindow.IsOpen; };
        }

        private void Framework_Update(IFramework framework)
        {
            if (!apState.Connected)
                return;
            var localPlayer = DalamudApi.ClientState.LocalPlayer;
            if (localPlayer == null)
                return;
            var job = localPlayer.ClassJob.GameData;
            if (apState.lastJob != job)
                apState.UpdateBars();

            Events.CheckAmnesty();
        }

        private void Chat(string command, string arguments)
        {
            if (!apState.Connected)
            {
                DalamudApi.Echo("Connect with /ap-connect first");
                return;
            }
            if (string.IsNullOrEmpty(arguments))
            {
                MainWindow.IsOpen = true;
                return;
            }
            apState.session.Socket.SendPacket(new SayPacket() { Text = arguments });
        }

        private void ShowConfig(string command, string arguments)
        {
            this.ConfigWindow.IsOpen = true;
        }

        public void Dispose()
        {
            apState?.Disconnect();
            WindowSystem.RemoveAllWindows();
            ConfigWindow.Dispose();
            Hooks.Dispose();
            Events.Disable();
            UiHooks.Disable();
            CommandManager.RemoveHandler("/ap");
            CommandManager.RemoveHandler("/ap-connect");
            CommandManager.RemoveHandler("/ap-config");
            CommandManager.RemoveHandler("/ap-disconnect");
        }

        private void Connect(string command, string args)
        {
            // in response to the slash command, just display our main ui
            this.MainWindow.IsOpen = true;
            if (string.IsNullOrEmpty(Configuration.Connection))
            {
                this.ConfigWindow.IsOpen = true;
                return;
            }
            if (!apState.Connected)
            {
                apState.Connect(Configuration.Connection, Configuration.SlotName);
                Events.RefreshTerritory();
            }
        }

        private void Disconnect(string command, string args)
        {
            apState.Disconnect();
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }


    }
}

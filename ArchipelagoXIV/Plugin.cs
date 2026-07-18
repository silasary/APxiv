using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using ArchipelagoXIV.Windows;
using ArchipelagoXIV.Hooks;
using Dalamud.Plugin.Services;
using Archipelago.MultiClient.Net.Packets;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace ArchipelagoXIV
{
    public sealed class Plugin : IAsyncDalamudPlugin
    {
        [PluginService]
        private IDalamudPluginInterface PluginInterface { get; init; }
        [PluginService]
        private ICommandManager CommandManager { get; init; }

        private ApState apState { get; set; }

        internal UnlockHooks Hooks { get; private set; }
        internal Events Events { get; private set; }
        internal UIHooks UiHooks { get; private set; }
        internal DeathLinkHooks DLHooks { get; private set; }
        internal HuntHooks HuntHooks { get; private set; }
        public Configuration Configuration { get; private set; }
        public WindowSystem WindowSystem = new("ArchipelagoXIV");

        private ConfigWindow ConfigWindow { get; set; }
        private MainWindow MainWindow { get; set; }

        public async Task LoadAsync(CancellationToken cancellationToken)
        {
            DalamudApi.Initialize(PluginInterface);
            Data.Initialize();

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            this.apState = new ApState(Configuration);

            this.Hooks = new UnlockHooks(apState);
            this.Events = new Events(apState);
            this.UiHooks = new UIHooks(apState);
            this.DLHooks = new DeathLinkHooks(apState);
            this.HuntHooks = new HuntHooks(apState);


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

            await DalamudApi.Framework.RunOnFrameworkThread(() =>
            {
                this.Hooks.Enable();
                this.Events.Enable();
                UiHooks.Enable();
                DLHooks.Enable();
                DalamudApi.Framework.Update += Framework_Update;
                DalamudApi.SetStatusBar("AP Ready");
                DalamudApi.logicBar!.OnClick += (e) => { MainWindow.IsOpen = !MainWindow.IsOpen; };
            });
            await LogicUpdate(cancellationToken);
        }


        public async ValueTask DisposeAsync()
        {
            await DalamudApi.Framework.RunOnFrameworkThread(() =>
            {
                Dispose();
            });
        }

        private async Task LogicUpdate(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (apState.RefreshBars)
                {
                    await apState.UpdateBars();
                    apState.RefreshBars = false;
                }

                await Task.Delay(1000, cancellationToken);
            }
        }

        private void Framework_Update(IFramework framework)
        {
            if (!apState.Connected)
                return;
            var localPlayer = DalamudApi.PlayerState;
            if (!localPlayer.IsLoaded)
                return;
            var refresh = false;
            var job = localPlayer.ClassJob.Value;
            var fates = DalamudApi.FateTable.Length;

            if (apState.lastJob.RowId != job.RowId)
            {
                apState.lastJob = job;
                refresh = true;
            }

            if (apState.lastFateCount != fates)
            {
                apState.lastFateCount = fates;
                refresh = true;
            }
            if (refresh)
                apState.RefreshBars = true;

            Events.CheckAmnesty();
            HuntHooks.OnFrameworkUpdate();
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
            apState.session!.Socket.SendPacket(new SayPacket() { Text = arguments });
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
            DLHooks.Dispose();
            DalamudApi.Framework.Update -= Framework_Update;
            CommandManager.RemoveHandler("/ap");
            CommandManager.RemoveHandler("/ap-connect");
            CommandManager.RemoveHandler("/ap-config");
            CommandManager.RemoveHandler("/ap-disconnect");
            DalamudApi.DtrBar.Remove("Archipelago");
            DalamudApi.DtrBar.Remove("APJob");
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
                apState.Connect(Configuration.Connection, Configuration.SlotName, Configuration.Password);
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

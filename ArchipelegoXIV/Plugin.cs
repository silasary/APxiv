using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using SamplePlugin.Windows;
using Dalamud.Game.DutyState;
using Dalamud.Game.ClientState;
using ArchipelegoXIV;
using System;
using Dalamud.Data;
using Lumina.Excel.GeneratedSheets;
using System.Linq;
using Dalamud.Game;
using ArchipelegoXIV.Hooks;
using Dalamud.Plugin.Services;

namespace SamplePlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        private const string CommandName = "/ap";

        private DalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }

        private ApState apState { get; init; }

        internal UnlockHooks Hooks { get; }
        internal Events Events { get; }
        internal UIHooks UiHooks { get; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("ArchipelegoXIV");

        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager
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

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Show Archipelago main window, and connect if we're not connected"
            });

            this.CommandManager.AddHandler("/apconfig", new CommandInfo(ShowConfig)
            {
                HelpMessage = "Archipelago Config"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            this.Hooks.Enable();
            this.Events.Enable();
            UiHooks.Enable();
            DalamudApi.SetStatusBar("AP Disconnected");
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
            CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
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

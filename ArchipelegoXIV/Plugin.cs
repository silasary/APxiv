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

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            ConfigWindow = new ConfigWindow(this, this.apState);
            MainWindow = new MainWindow(this, this.apState);

            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);

            MainWindow.IsOpen = true;

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            this.Hooks.Enable();
            this.Events.Enable();
            DalamudApi.SetStatusBar("AP Disconnected");
        }

        public void Dispose()
        {
            if (apState.Connected && (apState.session?.Socket?.Connected ?? false))
                apState.session?.Socket?.DisconnectAsync()?.Wait();
            WindowSystem.RemoveAllWindows();
            ConfigWindow.Dispose();
            Hooks.Dispose();
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
            if (!this.apState.Connected)
                this.apState.Connect(Configuration.Connection, Configuration.SlotName);
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

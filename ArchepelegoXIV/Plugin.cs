using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using SamplePlugin.Windows;
using Dalamud.Game.DutyState;
using Dalamud.Game.ClientState;
using ArchepelegoXIV;
using System;
using Dalamud.Data;
using Lumina.Excel.GeneratedSheets;
using System.Linq;
using Dalamud.Game;
using ArchepelegoXIV.Hooks;

namespace SamplePlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "ArchepelegoXIV";
        private const string CommandName = "/ap";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private DutyState dutyState { get; init; }
        private ClientState clientState { get; init; }

        private ApState apState { get; init; }
        internal UnlockHooks Hooks { get; }
        public TerritoryType[] Territories { get; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("ArchepelegoXIV");

        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] DutyState dutyState,
            [RequiredVersion("1.0")] ClientState clientState,
            [RequiredVersion("1.0")] DataManager dataManager,
            [RequiredVersion("1.0")] SigScanner sigScanner
        )
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.dutyState = dutyState;
            this.clientState = clientState;

            this.apState = new ApState();

            this.Hooks = new UnlockHooks(apState);

            var territoryTypes = dataManager.GetExcelSheet<TerritoryType>();
            Territories = territoryTypes.ToArray();

            DalamudApi.Initialize(pluginInterface);

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
            this.clientState.TerritoryChanged += State_TerritoryChanged;
            if (clientState.IsLoggedIn)
                State_TerritoryChanged(this, clientState.TerritoryType);
            this.Hooks.Enable();
        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            
            ConfigWindow.Dispose();
            MainWindow.Dispose();
            Hooks.Dispose();
            this.CommandManager.RemoveHandler(CommandName);
            clientState.TerritoryChanged -= State_TerritoryChanged;
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            this.MainWindow.IsOpen = true;
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }

        private void State_TerritoryChanged(object? sender, ushort e)
        {
            var territory = apState.territory = Territories.First(row => row.RowId == e);
            apState.territoryName = territory.PlaceName.Value?.Name;
            apState.territoryRegion = territory.PlaceNameRegion.Value?.Name;
        }
    }
}

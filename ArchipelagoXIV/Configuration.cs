using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace ArchipelagoXIV
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private IDalamudPluginInterface? pluginInterface;
        public string Connection { get; set; } = "";
        public string SlotName { get; set; } = "";
        public string GameName { get; set; } = "";
        public bool ForceDeathlink { get; set; } = false;
        public bool IgnoreClassRestrictions { get; set; } = false;
        public bool RequireSyncedDuties { get; set; } = false;
        public string Password { get; internal set; } = "";

        public List<string> ConnectionHistory { get; set; } = [];

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void AddToConnectionHistory(string? slotName = null, string? password = null, string? connection = null)
        {
            slotName ??= this.SlotName;
            password ??= this.Password;
            connection ??= this.Connection;

            var quickstring = $"{slotName}:{password}@{connection}";

            if (!ConnectionHistory.Contains(quickstring))
                ConnectionHistory.Add(quickstring);
            if (ConnectionHistory.Count > 5)
            {
                ConnectionHistory.RemoveAt(0);
            }
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}

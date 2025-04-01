using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

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
        public string Password { get; internal set; } = "";

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}

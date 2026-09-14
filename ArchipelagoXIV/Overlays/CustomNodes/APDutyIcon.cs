using System.Numerics;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Simplified;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ArchipelagoXIV.Overlays.CustomNodes
{
    internal unsafe class APDutyIcon : ResNode
    {
        //private readonly ImageNode APIcon;
        //private readonly object _icon;

        public APDutyIcon()
        {
            //var assemblyLocation = DalamudApi.PluginInterface.AssemblyLocation.DirectoryName!;
            //var icon_path = Path.Combine(assemblyLocation, "color-icon.png");
            //var _icon = DalamudApi.TextureProvider.GetFromFile(icon_path);

            var APIcon = new SimpleImageNode()
            {
                Size = new Vector2(18, 18),
                Position = Vector2.Zero,
                TextureCoordinates = new Vector2(0.0f, 0.0f),
                TextureSize = new Vector2(22.0f, 22.0f),
                //TexturePath = icon_path,
                IsVisible = true,
                WrapMode = WrapMode.Tile,
                ImageNodeFlags = FFXIVClientStructs.FFXIV.Component.GUI.ImageNodeFlags.AutoFit,
            };
            APIcon.LoadIcon(61826);
            APIcon.AttachNode(this);
        }
    }
}

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArchipelagoXIV.Hooks
{
    internal class HuntHooks(ApState apState) : IDisposable
    {
        // Maps GameObjectId → last known HP so we can detect the alive→dead transition
        private readonly Dictionary<ulong, uint> _trackedHp = [];
        // Mobs the local player has interacted with
        private readonly HashSet<ulong> _playerAttacked = [];

        public void Enable()
        {
            DalamudApi.Framework.Update += OnFrameworkUpdate;
        }

        public void Disable()
        {
            DalamudApi.Framework.Update -= OnFrameworkUpdate;
        }

        private void OnFrameworkUpdate(Dalamud.Plugin.Services.IFramework _)
        {
            var localPlayer = DalamudApi.ObjectTable.LocalPlayer;
            var localPlayerId = localPlayer?.GameObjectId ?? ulong.MaxValue;
            var playerTargetId = localPlayer?.TargetObjectId ?? ulong.MaxValue;

            var toRemove = new HashSet<ulong>(_trackedHp.Keys);

            foreach (var obj in DalamudApi.ObjectTable)
            {
                if (obj is not IBattleNpc bnpc)
                    continue;
                if (bnpc.BattleNpcKind != BattleNpcSubKind.Combatant)
                    continue;

                var id = bnpc.GameObjectId;
                toRemove.Remove(id);

                // Tag as player-involved if the player is targeting this mob,
                // or if the mob is targeting the player
                if (id == playerTargetId || bnpc.TargetObjectId == localPlayerId)
                    _playerAttacked.Add(id);

                if (_trackedHp.TryGetValue(id, out var lastHp) && lastHp > 0 && bnpc.CurrentHp == 0)
                {
                    // Transitioned alive → dead this tick
                    if (_playerAttacked.Contains(id))
                        OnHuntKill(bnpc);

                    _trackedHp.Remove(id);
                    _playerAttacked.Remove(id);
                    continue;
                }

                _trackedHp[id] = bnpc.CurrentHp;
            }

            // remove objects that left the object table without a confirmed HP=0 kill
            // (like fled out of range, was already dead)
            foreach (var gone in toRemove)
            {
                _trackedHp.Remove(gone);
                _playerAttacked.Remove(gone);
            }
        }

        private void OnHuntKill(IBattleNpc bnpc)
        {
            if (!Data.HuntTable.TryGetValue(bnpc.NameId, out var huntInfo))
            {
                DalamudApi.PluginLog.Debug($"[Hunt] {bnpc.Name} (NameId={bnpc.NameId}) not in HuntTable, ignoring.");
                return;
            }

            DalamudApi.PluginLog.Information($"[Hunt] Killed {huntInfo.Rank}-rank {huntInfo.Name}");

            var loc = apState.MissingLocations?.FirstOrDefault(l => string.Equals(l.Name, huntInfo.LocationName, StringComparison.OrdinalIgnoreCase));
            
            if (loc == null)
            {
                DalamudApi.PluginLog.Information($"[Hunt] {huntInfo.LocationName} not in missing locations (already done or not in this seed).");
                return;
            }

            if (!loc.IsAccessible())
            {
                DalamudApi.PluginLog.Information($"[Hunt] {huntInfo.LocationName} is not in logic (level or region requirement not met), skipping.");
                return;
            }

            loc.Complete();
        }

        public void Dispose()
        {
            Disable();
        }
    }
}

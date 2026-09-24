using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace CS2_Poor_MapAdvertisements.Managers;

public class EventManager(CS2_Poor_MapAdvertisements plugin)
{
    private readonly CS2_Poor_MapAdvertisements _plugin = plugin;
    private string? _savedPingTokenCooldown;
    private bool _pingCooldownDisabled;
    public void RegisterEvents()
    {
        //Events:
        _plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        _plugin.RegisterEventHandler<EventPlayerPing>(OnPlayerPing);

        //Listeners:
        _plugin.RegisterListener<Listeners.OnServerPrecacheResources>((ResourceManifest manifest) =>
        {
            foreach (var material in (_plugin.Config.Props ?? [])
                .Concat(_plugin.Config.MapIntegration?.Materials ?? [])
                .Concat(_plugin.Config.SolidMaterialVariants.Values)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                manifest.AddResource(material);
            }
        });
        _plugin.RegisterListener<Listeners.OnMapStart>(OnMapStart);
        _plugin.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
        _plugin.RegisterListener<Listeners.OnTick>(OnTick);
        _plugin.AddCommandListener("say", OnPlayerChatListener);
        _plugin.AddCommandListener("say_team", OnPlayerChatListener);
    }

    private void OnTick()
    {
        var placingViaPing = _plugin.MapIntegration?.IsPlacing == true;
        foreach (var player in _plugin.MapIntegration!.Placing)
            if (player.IsValid)
                player.PrintToCenterHtml("Map Integration: ping a wall to save a slot.<br>Stop: !mapadverts_matchzy");
        foreach(var player in _plugin.MenuManager!._selectedMaterial)
        {
            if(player.Key.IsValid && player.Value.onPing)
            {
                placingViaPing = true;
                player.Key.PrintToCenterHtml($"{_plugin.Localizer["OnTickNotification", player.Value.material!]}");
            }
        }

        UpdatePingCooldown(placingViaPing);
    }

    public void RestorePingCooldown()
    {
        if (!_pingCooldownDisabled || _savedPingTokenCooldown == null) return;

        ConVar.Find("player_ping_token_cooldown")?.SetValue(_savedPingTokenCooldown);
        _savedPingTokenCooldown = null;
        _pingCooldownDisabled = false;
    }

    private void UpdatePingCooldown(bool placingViaPing)
    {
        if (placingViaPing == _pingCooldownDisabled) return;

        var pingTokenCooldown = ConVar.Find("player_ping_token_cooldown");
        if (pingTokenCooldown == null) return;

        if (placingViaPing)
        {
            _savedPingTokenCooldown = pingTokenCooldown.StringValue;
            pingTokenCooldown.SetValue(0.0f);
            _pingCooldownDisabled = true;
            return;
        }

        RestorePingCooldown();
    }

    private HookResult OnPlayerChatListener(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null) return HookResult.Continue;
        if (!AdminManager.PlayerHasPermissions(player, _plugin.Config.AdminFlag) || !_plugin.MenuManager!._listenForChat.ContainsKey(player))
        {
            return HookResult.Continue;
        }
        var msg = commandInfo.GetArg(1);
        if(string.IsNullOrWhiteSpace(msg)) return HookResult.Continue;
        if(!int.TryParse(msg, out int value))
        {
            player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["NoArg"]}");
            return HookResult.Continue;
        }

        _plugin.MenuManager._listenForChat[player].ModelGroupIndex = value;
        
        player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer[$"PlayerSelectedSkin", value]}");

        _plugin.MenuManager._listenForChat[player].EntityProp!.AcceptInput("Skin", _plugin.MenuManager._listenForChat[player].EntityProp, _plugin.MenuManager._listenForChat[player].EntityProp, value.ToString());

        Server.NextFrame(() =>
        {
            _plugin.MenuManager._listenForChat.Remove(player);
        });

        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        _plugin.PropManager!.SpawnProps();
        return HookResult.Continue;
    }

    private HookResult OnPlayerPing(EventPlayerPing @event, GameEventInfo info)
    {
        var ping = @event;
        var player = ping.Userid;
        if (player == null) return HookResult.Continue;

        if (AdminManager.PlayerHasPermissions(player, _plugin.Config.AdminFlag)
            && _plugin.MapIntegration!.Placing.Contains(player))
        {
            try { _plugin.MapIntegration.Place(player, new Vector(ping.X, ping.Y, ping.Z)); }
            catch (Exception ex)
            {
                player.PrintToChat("[Map Integration] Could not save slot. Check server logs.");
                _plugin.DebugMode(ex.ToString());
            }
            return HookResult.Continue;
        }

        if (!AdminManager.PlayerHasPermissions(player, _plugin.Config.AdminFlag) || !_plugin.MenuManager!._selectedMaterial.TryGetValue(player, out var selected))
        {
            return HookResult.Continue;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return HookResult.Continue;

        if (!selected!.onPing) return HookResult.Continue;
        if(_plugin.PluginUtils!.CheckMaterial(selected.material!))
        {
            _plugin.PluginUtils!.CreatePropModelOnClick(new Vector(ping.X, ping.Y, ping.Z), new QAngle(pawn.EyeAngles.X, pawn.EyeAngles.Y, pawn.EyeAngles.Z), selected.material!, selected.isVip, selected.isOnGround, selected.materialIndex);
        }
        else
        {
            _plugin.PluginUtils!.CreateDecalOnClick(player, new Vector(ping.X, ping.Y, ping.Z));
        }
        
        return HookResult.Continue;
    }

    public void RemoveAdvertisementEntities()
    {
        foreach (var ad in GetAdvertisementEntities())
            if (ad.IsValid) ad.Remove();
    }

    private static IEnumerable<CBaseEntity> GetAdvertisementEntities()
    {
        var decals = Utilities.FindAllEntitiesByDesignerName<CEnvDecal>("env_decal") ?? [];
        var props = Utilities.FindAllEntitiesByDesignerName<CPhysicsPropOverride>("prop_physics_override") ?? [];
        return decals.Cast<CBaseEntity>().Concat(props)
            .Where(entity => entity.Entity?.Name?.StartsWith("advert", StringComparison.Ordinal) == true);
    }

    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        _plugin.MapIntegration!.CheckTransmit(infoList);
        var allAdvs = GetAdvertisementEntities().ToList();

        if(!allAdvs.Any()) return;

        try
        {
            foreach (var entry in infoList)
            {
                CCheckTransmitInfo info;
                CCSPlayerController? player;

                try
                {
                    (info, player) = ((CCheckTransmitInfo, CCSPlayerController))entry;
                }
                catch
                {
                    continue;
                }

                if (player == null) continue;
                bool isVip = AdminManager.PlayerHasPermissions(player, _plugin.Config.VipFlag);
                bool isActivePlayer = player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist;
                var preference = _plugin.GetAdvertisementPreference(player);
                foreach (var ad in allAdvs)
                {
                    if (AdvertisementVisibilityRules.ShouldHide(
                        _plugin.AdvertisementsVisible,
                        _plugin.AdvertisementAudience,
                        preference,
                        isActivePlayer,
                        isVip,
                        ad.Entity?.Name))
                        info.TransmitEntities.Remove(ad);
                }
            }
        }
        catch (Exception error)
        {
            _plugin.DebugMode($"CheckTransmit: ${error}");
        }

    }


    private void OnMapStart(string mapName)
    {
        _plugin.PropManager!.InitializeMap(mapName);
        Server.NextFrame(() => _plugin.PropManager.SpawnProps());
    }

}

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace CS2_Poor_MapAdvertisements.Managers;

public class CommandsManager(CS2_Poor_MapAdvertisements plugin)
{
    private readonly CS2_Poor_MapAdvertisements _plugin = plugin;

    public void RegisterCommands()
    {
        if (_plugin.Config.EnableCMD)
        {
            _plugin.AddCommand("css_mapadverts", "Map advertisements menu", OnMapAdvert);
            _plugin.AddCommand("css_mapadverts_matchzy", "Optional MatchZy map integration", (player, _) =>
            {
                if (player != null && AdminManager.PlayerHasPermissions(player, _plugin.Config.AdminFlag))
                    _plugin.MenuManager!.ShowMapIntegrationMenu(player);
            });
            _plugin.AddCommand("css_mapadverts_undo", "Undo last map advert placement", OnUndoLastAdvert);
            _plugin.AddCommand("css_mapadverts_toggle", "Emergency advertisement visibility toggle", OnToggleAdvertisements);
            _plugin.AddCommand("css_mapadverts_audience", "Set advertisement audience: all or spectators", OnSetAdvertisementAudience);
            _plugin.AddCommand("css_mapadverts_self", "Set your personal advertisement visibility", OnTogglePersonalVisibility);
        }
    }
    private void OnMapAdvert(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if(player == null) return;
        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        if (!AdminManager.PlayerHasPermissions(player, _plugin.Config.AdminFlag))
        {
            player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["NoAccess"]}");
            return;
        }

        _plugin.MenuManager!.ShowMapAdvertMenu(player);

        return;
    }

    private void OnUndoLastAdvert(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !AdminManager.PlayerHasPermissions(player, _plugin.Config.AdminFlag))
        {
            if (player != null)
            {
                player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["NoAccess"]}");
            }

            return;
        }

        var removedId = _plugin.PropManager!.UndoLastPlacement();
        player.PrintToChat($"{_plugin.Localizer["Prefix"]}{(removedId.HasValue ? _plugin.Localizer["SuccessUndo", removedId.Value] : _plugin.Localizer["NothingToUndo"])}");
    }

    private void OnToggleAdvertisements(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player != null && !AdminManager.PlayerHasPermissions(player, _plugin.Config.AdminFlag))
        {
            player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["NoAccess"]}");
            return;
        }

        _plugin.SetAdvertisementsVisible(!_plugin.AdvertisementsVisible);
        var state = _plugin.AdvertisementsVisible ? "VISIBLE" : "HIDDEN";
        if (player != null)
            player.PrintToChat($"{_plugin.Localizer["Prefix"]}All advertisements are now {state}.");
        else
            _plugin.Logger.LogInformation("All advertisements are now {State}.", state);
    }

    private void OnSetAdvertisementAudience(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player != null && !AdminManager.PlayerHasPermissions(player, _plugin.Config.AdminFlag))
        {
            player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["NoAccess"]}");
            return;
        }

        var argument = commandInfo.GetArg(1).Trim().ToLowerInvariant();
        var audience = argument switch
        {
            "all" or "everyone" => AdvertisementAudience.Everyone,
            "spectators" or "observers" => AdvertisementAudience.Spectators,
            _ => (AdvertisementAudience?)null
        };

        if (audience == null)
        {
            Reply(player, $"Advertisement audience: {_plugin.AdvertisementAudience}. Usage: css_mapadverts_audience <all|spectators>.");
            return;
        }

        if (_plugin.AdvertisementAudience != audience.Value)
        {
            _plugin.AdvertisementAudience = audience.Value;
            _plugin.RefreshAdvertisementVisibility();
        }
        Reply(player, audience == AdvertisementAudience.Everyone
            ? "Advertisement audience: ALL players and spectators."
            : "Advertisement audience: SPECTATORS only. Active T/CT players are hidden.");
    }

    private void OnTogglePersonalVisibility(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null) return;

        var argument = commandInfo.GetArg(1).Trim().ToLowerInvariant();
        var preference = argument switch
        {
            "auto" => AdvertisementPreference.Auto,
            "hide" or "hidden" or "off" => AdvertisementPreference.Hidden,
            "show" or "visible" or "on" => AdvertisementPreference.Visible,
            _ => (AdvertisementPreference?)null
        };

        if (preference == null)
        {
            player.PrintToChat($"{_plugin.Localizer["Prefix"]}Your advertisement visibility: {_plugin.GetAdvertisementPreference(player).ToString().ToUpperInvariant()}. Usage: css_mapadverts_self <auto|hide|show>.");
            return;
        }

        if (_plugin.GetAdvertisementPreference(player) != preference.Value)
        {
            if (preference == AdvertisementPreference.Auto)
                _plugin.AdvertisementPreferences.Remove(player.SteamID);
            else
                _plugin.AdvertisementPreferences[player.SteamID] = preference.Value;
            _plugin.RefreshAdvertisementVisibility();
        }

        player.PrintToChat($"{_plugin.Localizer["Prefix"]}Your advertisement visibility: {preference.Value.ToString().ToUpperInvariant()}. Global emergency OFF always wins.");
    }

    private void Reply(CCSPlayerController? player, string message)
    {
        if (player != null)
            player.PrintToChat($"{_plugin.Localizer["Prefix"]}{message}");
        else
            _plugin.Logger.LogInformation("{Message}", message);
    }

}

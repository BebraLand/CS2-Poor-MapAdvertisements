using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

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

}

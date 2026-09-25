using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_Poor_MapAdvertisements.Managers;
using CS2_Poor_MapAdvertisements.Models;
using CS2MenuManager.API.Enum;
using CS2MenuManager.API.Menu;

namespace CS2_Poor_MapAdvertisements.Menu;

public partial class PluginMenu(CS2_Poor_MapAdvertisements plugin)
{
    private readonly CS2_Poor_MapAdvertisements _plugin = plugin;

    public Dictionary<CCSPlayerController, SelectedMaterialModel> _selectedMaterial = new();
    public Dictionary<CCSPlayerController, PropModel> _listenForChat = new();
    private string[] _retardedWayCords = ["X+", "X-", "Y+", "Y-", "Z+", "Z-"];
    private int[] _decalSize = [16, 32, 64, 128, 256, 512, 1024];

    public void ShowAdvertisementPreferenceMenu(CCSPlayerController player)
    {
        if (player?.IsValid != true) return;

        var preference = _plugin.GetAdvertisementPreference(player);
        var mode = _plugin.Localizer.ForPlayer(player, $"AdsMode_{preference}");
        var audience = !_plugin.AdvertisementsVisible ? "AdsAudience_Off"
            : _plugin.AdvertisementAudience == AdvertisementAudience.Spectators ? "AdsAudience_Spectators"
            : "AdsAudience_Everyone";
        var menu = new WasdMenu(_plugin.Localizer.ForPlayer(player, "AdsMenu_Header"), _plugin);
        menu.AddItem(_plugin.Localizer.ForPlayer(player, "AdsMenu_Current", mode), DisableOption.DisableHideNumber);
        menu.AddItem(_plugin.Localizer.ForPlayer(player, "AdsMenu_Audience", _plugin.Localizer.ForPlayer(player, audience)), DisableOption.DisableHideNumber);

        AddAdvertisementPreferenceOption(menu, player, AdvertisementPreference.Auto, "AdsMenu_Auto");
        AddAdvertisementPreferenceOption(menu, player, AdvertisementPreference.Hidden, "AdsMenu_Hide");
        AddAdvertisementPreferenceOption(menu, player, AdvertisementPreference.Visible, "AdsMenu_Show");
        menu.Display(player, 0);
    }

    private void AddAdvertisementPreferenceOption(WasdMenu menu, CCSPlayerController player,
        AdvertisementPreference preference, string label)
    {
        menu.AddItem(_plugin.Localizer.ForPlayer(player, label), (p, option) =>
        {
            if (!p.IsValid) return;
            _plugin.CommandsManager!.ApplyPersonalAdvertisementPreference(p, preference);
            option.PostSelectAction = PostSelectAction.Close;
            Server.NextFrame(() => ShowAdvertisementPreferenceMenu(p));
        });
    }

    public void ShowMapAdvertMenu(CCSPlayerController player)
    {
        if (player == null) return;
        WasdMenu menu = new($"{_plugin.Localizer["MapAdvertMenu_Header"]}", _plugin);
        menu.AddItem($"{_plugin.Localizer["CreatePropMenu"]}", (p, o) =>
        {
            CreatePropMenu(player, menu);
        });
        menu.AddItem($"{_plugin.Localizer["CreateDecalMenu"]}", (p, o) =>
        {
            CreateDecalMenu(player, menu);
        });

        menu.AddItem($"{_plugin.Localizer["EditPropsMenu"]}", (p, o) =>
        {
            EditPropsMenu(player, menu);
        });

        menu.AddItem($"{_plugin.Localizer["EditDecalsMenu"]}", (p, o) =>
        {
            EditDecalMenu(player, menu);
        });

        menu.AddItem($"{_plugin.Localizer["RemoveAdvertsMenu"]}", (p, o) =>
        {
            RemoveAdvertsMenu(player, menu);
        });

        if (_plugin.PropManager!.HasUndoablePlacement)
        {
            menu.AddItem($"{_plugin.Localizer["UndoLastAdvertMenu"]}", (p, o) =>
            {
                var removedId = _plugin.PropManager.UndoLastPlacement();
                p.PrintToChat($"{_plugin.ChatPrefix}{(removedId.HasValue ? _plugin.Localizer["SuccessUndo", removedId.Value] : _plugin.Localizer["NothingToUndo"])}");
                o.PostSelectAction = PostSelectAction.Close;
                Server.NextFrame(() => ShowMapAdvertMenu(p));
            });
        }

        if (_plugin.PropManager!._props.Count > 0)
        {
            menu.AddItem($"{_plugin.Localizer["RemoveAllAdvertsMenu"]}", (p, o) =>
            {
                RemoveAllAdvertsConfirmation(player, menu);
            });
        }

        menu.AddItem($"{_plugin.Localizer["SaveAdvertsMenu"]}", (p, o) =>
        {
            try
            {
                _plugin.PropManager!.SaveAllAdverts();
                p.PrintToChat($"{_plugin.ChatPrefix}{_plugin.Localizer["SavedAdverts"]}");
            }
            catch (Exception error)
            {
                p.PrintToChat($"{_plugin.ChatPrefix}{_plugin.Localizer["SavedAdvertsError"]}");
                _plugin.DebugMode($"{error}");
            }

        });

        menu.AddItem($"{_plugin.Localizer["ClearCacheMenu"]}", (p, o) =>
        {
            if (!_plugin.MenuManager!._selectedMaterial.ContainsKey(p)) return;
            _plugin.MenuManager._selectedMaterial.Remove(p);

        });

        menu.Display(player, 0);
    }

    private IEnumerable<string> DecalMaterials()
        => (_plugin.Config.Props ?? [])
            .Concat(_plugin.Config.MapIntegration?.Materials ?? [])
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase);

    private void RemoveAdvertsMenu(CCSPlayerController player, WasdMenu prevMenu)
    {
        if (player == null) return;
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return;

        WasdMenu menu = new($"{_plugin.Localizer["RemoveAdvert_Header"]}", _plugin);

        foreach (var adv in _plugin.PropManager!._props)
        {
            menu.AddItem($"{adv.Id}", (p, o) =>
            {
                _plugin.PropManager.RemovePropFromFile(adv.Id);
                player.PrintToChat($"{_plugin.Localizer["SuccessRemove", adv.Id]}");
                o.PostSelectAction = PostSelectAction.Close;
                Server.NextFrame(() =>
                {
                    ShowMapAdvertMenu(player);
                });
            });
        }

        menu.PrevMenu = prevMenu;
        menu.Display(player, 0);
    }

    private void RemoveAllAdvertsConfirmation(CCSPlayerController player, WasdMenu prevMenu)
    {
        var advertCount = _plugin.PropManager!._props.Count;
        if (advertCount == 0)
        {
            ShowMapAdvertMenu(player);
            return;
        }

        WasdMenu menu = new($"{_plugin.Localizer["RemoveAllAdvertsConfirmHeader", advertCount]}", _plugin);
        menu.AddItem($"{_plugin.Localizer["RemoveAllAdvertsConfirm"]}", (p, o) =>
        {
            var removedCount = _plugin.PropManager.RemoveAllProps();
            p.PrintToChat($"{_plugin.ChatPrefix}{_plugin.Localizer["SuccessRemoveAll", removedCount]}");
            o.PostSelectAction = PostSelectAction.Close;
            Server.NextFrame(() => ShowMapAdvertMenu(p));
        });
        menu.AddItem($"{_plugin.Localizer["Cancel"]}", (p, o) =>
        {
            o.PostSelectAction = PostSelectAction.Close;
            Server.NextFrame(() => ShowMapAdvertMenu(p));
        });

        menu.PrevMenu = prevMenu;
        menu.Display(player, 0);
    }

    private void CordsMenu(CCSPlayerController player, WasdMenu prevMenu, PropModel prop, int propId, int _type)
    {
        if (player == null) return;
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return;

        var entity = prop.EntityProp;
        if (entity == null) return;

        WasdMenu menu = new($"{_plugin.Localizer[$"CordsFor_{_type}", propId]} ", _plugin);

        // _type - 0 Position, 1 - Angles, 2 - Config angles values, 3 - Config position values

        if (_type == 0)
        {
            foreach (var v in _plugin.Config.customPositionValues)
            {
                foreach (var i in _retardedWayCords)
                {
                    menu.AddItem($"{i} {v}", (p, o) =>
                    {
                        var pos = entity!.AbsOrigin!;
                        var newPos = new Vector(pos.X, pos.Y, pos.Z);

                        if (i == "X+") newPos = new Vector(pos.X + v, pos.Y, pos.Z);
                        else if (i == "X-") newPos = new Vector(pos.X - v, pos.Y, pos.Z);
                        else if (i == "Y+") newPos = new Vector(pos.X, pos.Y + v, pos.Z);
                        else if (i == "Y-") newPos = new Vector(pos.X, pos.Y - v, pos.Z);
                        else if (i == "Z+") newPos = new Vector(pos.X, pos.Y, pos.Z + v);
                        else if (i == "Z-") newPos = new Vector(pos.X, pos.Y, pos.Z - v);

                        entity.Teleport(newPos, entity.AbsRotation);
                        o.PostSelectAction = PostSelectAction.Nothing;
                    });
                }
            }

        }
        else if (_type == 1)
        {
            foreach (var v in _plugin.Config.customAngleValues)
            {
                foreach (var i in _retardedWayCords)
                {
                    menu.AddItem($"{i} {v}", (p, o) =>
                    {
                        var angles = entity!.AbsRotation!;
                        var newQangle = new QAngle(angles.X, angles.Y, angles.Z);

                        if (i == "X+") newQangle = new QAngle(angles.X + v, angles.Y, angles.Z);
                        else if (i == "X-") newQangle = new QAngle(angles.X - v, angles.Y, angles.Z);
                        else if (i == "Y+") newQangle = new QAngle(angles.X, angles.Y + v, angles.Z);
                        else if (i == "Y-") newQangle = new QAngle(angles.X, angles.Y - v, angles.Z);
                        else if (i == "Z+") newQangle = new QAngle(angles.X, angles.Y, angles.Z + v);
                        else if (i == "Z-") newQangle = new QAngle(angles.X, angles.Y, angles.Z - v);

                        entity.Teleport(entity.AbsOrigin, newQangle);
                        o.PostSelectAction = PostSelectAction.Nothing;

                    });
                }
            }
        }
        menu.PrevMenu = prevMenu;
        menu.Display(player, 0);
    }

}

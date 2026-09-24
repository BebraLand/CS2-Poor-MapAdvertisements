using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CS2_Poor_MapAdvertisements.Models;
using CS2MenuManager.API.Enum;
using CS2MenuManager.API.Menu;

namespace CS2_Poor_MapAdvertisements.Menu;

public partial class PluginMenu
{
    public void ShowMapIntegrationMenu(CCSPlayerController player, WasdMenu? previous = null)
    {
        if (!player.IsValid || !AdminManager.PlayerHasPermissions(player, _plugin.Config.AdminFlag)) return;
        var integration = _plugin.MapIntegration!;
        var menu = new WasdMenu("MatchZy map slots", _plugin) { PrevMenu = previous };
        if (!_plugin.Config.MapIntegration.Enabled)
        {
            menu.AddItem("Disabled: enable MatchZy Map Integration in config", DisableOption.DisableHideNumber);
            menu.Display(player, 0);
            return;
        }
        menu.AddItem(integration.CurrentMapNumber > 0 ? $"Match: MAP {integration.CurrentMapNumber}" : "No active series (slots hidden)", DisableOption.DisableHideNumber);
        var settings = integration.GetPlacementSettings(player);
        var selectedMap = settings.MapNumber > 0 ? integration.MaterialLabel(settings.MapNumber) : "material not selected";
        var selectedSize = settings.Width > 0 && settings.Height > 0 && settings.Depth > 0
            ? $"{settings.Width} x {settings.Height}, depth {settings.Depth}, {(settings.Solid ? "solid" : "current")}, {settings.Opacity}%" : "size not selected";
        menu.AddItem($"New slot: {selectedMap}, {selectedSize}",
            (p, _) => ConfigurePlacementMenu(p, menu));
        AddIntegrationAction(menu, $"Creating map slots on ping: {integration.Placing.Contains(player)}", () => integration.TogglePlacement(player));
        AddIntegrationAction(menu, "Clear preview and stop placement", () => integration.SetPreview(player, 0));
        if (integration.HasUndo) AddIntegrationAction(menu, "Undo last new slot", integration.Undo);
        if (integration.Slots.Count > 0)
            menu.AddItem($"Delete all {integration.Slots.Count} slots...", (p, _) =>
            {
                var confirm = new WasdMenu($"Delete all {integration.Slots.Count} slots on this map?", _plugin) { PrevMenu = menu };
                AddIntegrationAction(confirm, "Yes, delete all slots", integration.RemoveAll);
                confirm.AddItem("Cancel", (who, _) => ShowMapIntegrationMenu(who));
                confirm.Display(p, 0);
            });
        foreach (var slot in integration.Slots)
            menu.AddItem($"Edit slot #{slot.Id} ({slot.width} x {slot.height})", (p, _) => EditIntegrationSlot(p, menu, slot));
        menu.Display(player, 0);
    }

    private void ConfigurePlacementMenu(CCSPlayerController player, WasdMenu previous)
    {
        var integration = _plugin.MapIntegration!;
        var settings = integration.GetPlacementSettings(player);
        var menu = new WasdMenu("New MatchZy slot", _plugin) { PrevMenu = previous };
        menu.AddItem($"Material / preview: {(settings.MapNumber > 0 ? integration.MaterialLabel(settings.MapNumber) : "not selected")}", (p, _) =>
        {
            var maps = new WasdMenu("Choose material / preview", _plugin) { PrevMenu = menu };
            for (int number = 1; number <= 5; number++)
            {
                int selectedNumber = number;
                if (integration.Material(number) != null)
                    AddIntegrationAction(maps, integration.MaterialLabel(selectedNumber), () => integration.SetPlacementMap(player, selectedNumber));
            }
            maps.Display(p, 0);
        });
        menu.AddItem($"Width: {(settings.Width > 0 ? settings.Width : "not selected")}", (p, _) =>
        {
            var sizes = new WasdMenu("New slot width", _plugin) { PrevMenu = menu };
            foreach (var size in _decalSize)
                AddIntegrationAction(sizes, $"{size}", () => integration.SetPlacementWidth(player, size));
            sizes.Display(p, 0);
        });
        menu.AddItem($"Height: {(settings.Height > 0 ? settings.Height : "not selected")}", (p, _) =>
        {
            var sizes = new WasdMenu("New slot height", _plugin) { PrevMenu = menu };
            foreach (var size in _decalSize)
                AddIntegrationAction(sizes, $"{size}", () => integration.SetPlacementHeight(player, size));
            sizes.Display(p, 0);
        });
        menu.AddItem($"Depth: {(settings.Depth > 0 ? settings.Depth : "not selected")}", (p, _) =>
        {
            var depth = new WasdMenu("New slot depth", _plugin) { PrevMenu = menu };
            AddIntegrationAction(depth, "+1", () => integration.SetPlacementDepth(player, Math.Min(256, settings.Depth + 1)));
            AddIntegrationAction(depth, "-1", () => integration.SetPlacementDepth(player, Math.Max(1, settings.Depth - 1)));
            depth.Display(p, 0);
        });
        menu.AddItem($"Blend: {(settings.Solid ? "Solid" : "Current")}", (p, _) =>
            integration.SetPlacementSolid(p, !settings.Solid),
            disableOption: settings.MapNumber > 0 && integration.HasSolidVariant(settings.MapNumber)
                ? DisableOption.None : DisableOption.DisableHideNumber);
        menu.AddItem($"Opacity: {settings.Opacity}%", (p, _) =>
        {
            var opacity = new WasdMenu($"New slot opacity: {settings.Opacity}%", _plugin) { PrevMenu = menu };
            AddIntegrationAction(opacity, "+10%", () => integration.SetPlacementOpacity(player, settings.Opacity + 10));
            AddIntegrationAction(opacity, "-10%", () => integration.SetPlacementOpacity(player, settings.Opacity - 10));
            opacity.Display(p, 0);
        });
        menu.Display(player, 0);
    }

    // Guard callbacks too: permission/config/map may change while a menu is open.
    private void AddIntegrationAction(WasdMenu menu, string label, Action action, PropModel? slot = null)
    {
        menu.AddItem(label, (player, option) =>
        {
            if (!_plugin.Config.MapIntegration.Enabled || !AdminManager.PlayerHasPermissions(player, _plugin.Config.AdminFlag)
                || (slot != null && !_plugin.MapIntegration!.Slots.Contains(slot))) return;
            try { action(); }
            catch (Exception ex)
            {
                player.PrintToChat("[Map Integration] Could not save changes. Check server logs.");
                _plugin.DebugMode(ex.ToString());
            }
            option.PostSelectAction = PostSelectAction.Close;
            Server.NextFrame(() =>
            {
                if (!player.IsValid) return;
                if (slot != null && _plugin.MapIntegration!.Slots.Contains(slot))
                    menu.Display(player, 0);
                else ShowMapIntegrationMenu(player);
            });
        });
    }

    private void EditIntegrationSlot(CCSPlayerController player, WasdMenu? previous, PropModel slot)
    {
        var integration = _plugin.MapIntegration!;
        if (!integration.Slots.Contains(slot)) return;
        var menu = new WasdMenu($"Map Integration slot #{slot.Id}", _plugin) { PrevMenu = previous };
        menu.AddItem("Back to Map Integration", (p, _) => ShowMapIntegrationMenu(p));
        menu.AddItem($"Size: {slot.width} x {slot.height}; depth: {slot.depth}", (p, _) =>
        {
            var sizes = new WasdMenu("Slot size", _plugin) { PrevMenu = menu };
            foreach (int size in _decalSize)
            {
                AddIntegrationAction(sizes, $"Width: {size}", () => ChangeSlot(slot, () => slot.width = size), slot);
                AddIntegrationAction(sizes, $"Height: {size}", () => ChangeSlot(slot, () => slot.height = size), slot);
            }
            AddIntegrationAction(sizes, "Depth +1", () => ChangeSlot(slot, () => slot.depth = Math.Min(256, slot.depth + 1)), slot);
            AddIntegrationAction(sizes, "Depth -1", () => ChangeSlot(slot, () => slot.depth = Math.Max(1, slot.depth - 1)), slot);
            sizes.Display(p, 0);
        });
        AddIntegrationAction(menu, $"Blend: {(slot.solid ? "Solid" : "Current")}",
            () => ChangeSlot(slot, () => slot.solid = !slot.solid), slot);
        menu.AddItem($"Opacity: {slot.opacity}%", (p, _) =>
        {
            var opacity = new WasdMenu($"Slot opacity: {slot.opacity}%", _plugin) { PrevMenu = menu };
            AddIntegrationAction(opacity, "+10%", () => ChangeSlot(slot, () => slot.opacity = Math.Min(100, slot.opacity + 10)), slot);
            AddIntegrationAction(opacity, "-10%", () => ChangeSlot(slot, () => slot.opacity = Math.Max(10, slot.opacity - 10)), slot);
            opacity.Display(p, 0);
        });
        menu.AddItem("Position", (p, _) =>
        {
            var positions = new WasdMenu("Slot position", _plugin) { PrevMenu = menu };
            foreach (int step in (_plugin.Config.customPositionValues.Length > 0 ? _plugin.Config.customPositionValues : [1, 5, 10]))
            {
                foreach (int direction in new[] { -1, 1 })
                {
                    int delta = step * direction;
                    AddIntegrationAction(positions, $"X {delta:+#;-#;0}", () => ChangeSlot(slot, () => slot.posX += delta), slot);
                    AddIntegrationAction(positions, $"Y {delta:+#;-#;0}", () => ChangeSlot(slot, () => slot.posY += delta), slot);
                    AddIntegrationAction(positions, $"Z {delta:+#;-#;0}", () => ChangeSlot(slot, () => slot.posZ += delta), slot);
                }
            }
            positions.Display(p, 0);
        });
        menu.AddItem("Rotation", (p, _) =>
        {
            var angles = new WasdMenu("Slot rotation", _plugin) { PrevMenu = menu };
            foreach (int step in (_plugin.Config.customAngleValues.Length > 0 ? _plugin.Config.customAngleValues : [1, 5, 10]))
            {
                foreach (int direction in new[] { -1, 1 })
                {
                    int delta = step * direction;
                    AddIntegrationAction(angles, $"X {delta:+#;-#;0}", () => ChangeSlot(slot, () => slot.angleX += delta), slot);
                    AddIntegrationAction(angles, $"Y {delta:+#;-#;0}", () => ChangeSlot(slot, () => slot.angleY += delta), slot);
                    AddIntegrationAction(angles, $"Z {delta:+#;-#;0}", () => ChangeSlot(slot, () => slot.angleZ += delta), slot);
                }
            }
            angles.Display(p, 0);
        });
        menu.AddItem("Delete slot...", (p, _) =>
        {
            var confirm = new WasdMenu($"Delete slot #{slot.Id}?", _plugin) { PrevMenu = menu };
            AddIntegrationAction(confirm, "Yes, delete", () => integration.Remove(slot), slot);
            confirm.AddItem("Cancel", (who, _) => EditIntegrationSlot(who, previous, slot));
            confirm.Display(p, 0);
        });
        menu.Display(player, 0);
    }

    private void ChangeSlot(PropModel slot, Action change)
    {
        var old = (slot.posX, slot.posY, slot.posZ, slot.angleX, slot.angleY, slot.angleZ, slot.width, slot.height, slot.depth, slot.solid, slot.opacity);
        change();
        try { _plugin.MapIntegration!.Save(); }
        catch
        {
            (slot.posX, slot.posY, slot.posZ, slot.angleX, slot.angleY, slot.angleZ, slot.width, slot.height, slot.depth, slot.solid, slot.opacity) = old;
            throw;
        }
        _plugin.MapIntegration!.ClearEntities();
        _plugin.MapIntegration.Refresh();
    }
}

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_Poor_MapAdvertisements.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CS2_Poor_MapAdvertisements.Managers;

public sealed class MapIntegration(CS2_Poor_MapAdvertisements plugin)
{
    public List<PropModel> Slots { get; private set; } = [];
    public HashSet<CCSPlayerController> Placing { get; } = [];
    private readonly Dictionary<CCSPlayerController, int> previews = [];
    private readonly Dictionary<CCSPlayerController, PlacementSettings> placementSettings = [];
    private readonly Stack<PropModel> undo = [];
    private string map = "";
    private bool storageHealthy;
    private bool running = true;
    private string? renderedMaterial;
    private string Root => Path.GetFullPath(Path.Combine(plugin.ModuleDirectory, "..", "..", "configs", "plugins"));
    private string SlotPath => Path.Combine(Root, plugin.ModuleName, "map-integration", Path.GetFileName(map) + ".json");
    private static readonly PluginCapability<int> CurrentMapNumberCapability =
        new("matchzy:current_map_number:v1");
    private static readonly PluginCapability<int> SeriesLengthCapability =
        new("matchzy:series_length:v1");
    private static readonly PluginCapability<string> ChatPrefixCapability =
        new("matchzy:chat_prefix:v1");
    public int CurrentMapNumber { get; private set; }
    public string? MatchZyChatPrefix
    {
        get
        {
            try
            {
                var prefix = ChatPrefixCapability.Get();
                return string.IsNullOrWhiteSpace(prefix) ? null : prefix;
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }
    }
    public bool IsPlacing => Placing.Any();
    public bool HasUndo => undo.Any(Slots.Contains);

    private sealed class PlacementSettings
    {
        public int MapNumber { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public int Depth { get; set; } = 14;
        public bool Solid { get; set; }
        public int Opacity { get; set; } = 100;
    }

    private PlacementSettings Settings(CCSPlayerController player)
        => placementSettings.TryGetValue(player, out var settings)
            ? settings
            : placementSettings[player] = new();

    public (int MapNumber, float Width, float Height, int Depth, bool Solid, int Opacity) GetPlacementSettings(CCSPlayerController player)
    {
        var settings = Settings(player);
        return (settings.MapNumber, settings.Width, settings.Height, settings.Depth, settings.Solid, settings.Opacity);
    }

    public void SetPlacementMap(CCSPlayerController player, int number)
    {
        if (Material(number) == null) throw new InvalidOperationException($"MAP {number} material is not configured");
        Settings(player).MapNumber = number;
        previews[player] = number;
        Refresh();
    }

    public void SetPlacementWidth(CCSPlayerController player, float width)
    {
        Settings(player).Width = width;
        ClearEntities();
        Refresh();
    }

    public void SetPlacementHeight(CCSPlayerController player, float height)
    {
        Settings(player).Height = height;
        ClearEntities();
        Refresh();
    }

    public void SetPlacementDepth(CCSPlayerController player, int depth)
    {
        Settings(player).Depth = depth;
        ClearEntities();
        Refresh();
    }

    public void SetPlacementSolid(CCSPlayerController player, bool solid)
    {
        Settings(player).Solid = solid;
        ClearEntities();
        Refresh();
    }

    public void SetPlacementOpacity(CCSPlayerController player, int opacity)
    {
        Settings(player).Opacity = DecalAppearanceRules.NormalizeOpacity(opacity);
        ClearEntities();
        Refresh();
    }

    public bool HasSolidVariant(int number)
        => Material(number) is { } material && plugin.Config.SolidMaterialVariants.ContainsKey(material);

    public string? Material(int number)
    {
        var materials = plugin.Config.MapIntegration.Materials;
        if (materials == null || number < 1 || number > 5 || number > materials.Length) return null;
        var material = materials[number - 1];
        return !string.IsNullOrWhiteSpace(material) && material.StartsWith("materials/", StringComparison.Ordinal)
            && material.EndsWith(".vmat", StringComparison.OrdinalIgnoreCase) && !material.Contains("..")
            ? material : null;
    }

    public string MaterialLabel(int number)
        => $"MAP {number}";

    public void Start()
    {
        plugin.RegisterListener<Listeners.OnMapStart>(LoadMap);
        plugin.RegisterListener<Listeners.OnMapEnd>(() =>
        {
            ClearEntities();
            Slots.Clear();
            Placing.Clear();
            previews.Clear();
            placementSettings.Clear();
            undo.Clear();
            map = "";
            CurrentMapNumber = 0;
        });
        plugin.RegisterListener<Listeners.OnServerPrecacheResources>(manifest =>
        {
            if (!plugin.Config.MapIntegration.Enabled) return;
            for (int i = 1; i <= 5; i++)
                if (Material(i) is { } material) manifest.AddResource(material);
        });
        plugin.RegisterEventHandler<EventRoundStart>((_, _) => { Refresh(); return HookResult.Continue; });
        plugin.RegisterEventHandler<EventPlayerDisconnect>((ev, _) =>
        {
            if (ev.Userid is { } player)
            {
                Placing.Remove(player);
                previews.Remove(player);
                placementSettings.Remove(player);
                plugin.MenuManager!._selectedMaterial.Remove(player);
                plugin.MenuManager._listenForChat.Remove(player);
            }
            return HookResult.Continue;
        });
        plugin.AddTimer(1f, Refresh, TimerFlags.REPEAT);
        Server.NextFrame(() => { if (running && !string.IsNullOrEmpty(Server.MapName)) LoadMap(Server.MapName); });
    }

    private void LoadMap(string name)
    {
        ClearEntities();
        map = name;
        Slots = [];
        undo.Clear();
        Placing.Clear();
        previews.Clear();
        placementSettings.Clear();
        storageHealthy = false;
        try
        {
            Slots = File.Exists(SlotPath)
                ? JsonSerializer.Deserialize<List<PropModel>>(File.ReadAllText(SlotPath)) ?? [] : [];
            if (Slots.Any(s => s == null || !Valid(s))) throw new JsonException("Invalid slot coordinates or size");
            if (Slots.Any(s => s.Id < 1) || Slots.Select(s => s.Id).Distinct().Count() != Slots.Count)
                throw new JsonException("Invalid or duplicate slot IDs");
            storageHealthy = true;
        }
        catch (Exception ex)
        {
            Slots = [];
            plugin.Logger.LogError(ex, "Cannot load Map Integration slots; file will not be overwritten");
        }
        Refresh();
    }

    private static bool Valid(PropModel s) => new[] { s.posX, s.posY, s.posZ, s.angleX, s.angleY, s.angleZ,
        s.width, s.height }.All(float.IsFinite) && s.width is > 0 and <= 4096 && s.height is > 0 and <= 4096
        && s.depth is > 0 and <= 256 && s.opacity is >= 10 and <= 100;

    public void Save()
    {
        if (!storageHealthy || string.IsNullOrEmpty(map)) throw new IOException("Slot storage is not available");
        if (Slots.Any(s => !Valid(s))) throw new ArgumentException("Invalid slot size/coordinates");
        Directory.CreateDirectory(Path.GetDirectoryName(SlotPath)!);
        File.WriteAllText(SlotPath + ".tmp", JsonSerializer.Serialize(Slots, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(SlotPath + ".tmp", SlotPath, true);
    }

    public void Place(CCSPlayerController player, Vector position)
    {
        if (!storageHealthy || !plugin.Config.MapIntegration.Enabled || player.PlayerPawn.Value is not { } pawn) return;
        var backward = -plugin.PluginUtils!.Normalize(plugin.PluginUtils.GetForwardVector(pawn.EyeAngles));
        var offset = position + backward * 2f;
        bool ground = -Math.Sin(pawn.EyeAngles.X * Math.PI / 180) < -0.90;
        var settings = Settings(player);
        var slot = new PropModel
        {
            Id = Slots.Count == 0 ? 1 : Slots.Max(s => s.Id) + 1,
            posX = offset.X, posY = offset.Y, posZ = offset.Z + (ground ? 1 : 0),
            angleX = ground ? 0 : 90, angleY = (pawn.EyeAngles.Y + 180) % 360,
            width = settings.Width, height = settings.Height, depth = settings.Depth,
            solid = settings.Solid, opacity = settings.Opacity
        };
        Slots.Add(slot);
        try { Save(); }
        catch { Slots.Remove(slot); throw; }
        undo.Push(slot);
        Refresh();
        player.PrintToChat($"[Map Integration] Saved slot #{slot.Id} ({map}) MAP {settings.MapNumber} {settings.Width}x{settings.Height} depth {settings.Depth}, {(settings.Solid ? "solid" : "current")}, opacity {settings.Opacity}%.");
        if (CS2MenuManager.API.Class.MenuManager.GetActiveMenu(player)?.Menu.Title == "MatchZy map slots")
            plugin.MenuManager!.ShowMapIntegrationMenu(player);
    }

    public void Remove(PropModel slot)
    {
        int index = Slots.IndexOf(slot);
        if (index < 0) return;
        Slots.RemoveAt(index);
        try { Save(); }
        catch { Slots.Insert(index, slot); throw; }
        if (slot.EntityProp?.IsValid == true) slot.EntityProp.Remove();
        slot.EntityProp = null;
    }

    public void RemoveAll()
    {
        var removed = Slots.ToArray();
        if (removed.Length == 0) return;
        Slots.Clear();
        try { Save(); }
        catch { Slots.AddRange(removed); throw; }
        foreach (var slot in removed)
        {
            if (slot.EntityProp?.IsValid == true) slot.EntityProp.Remove();
            slot.EntityProp = null;
        }
        undo.Clear();
        Placing.Clear();
        previews.Clear();
        renderedMaterial = null;
    }

    public void Undo()
    {
        while (undo.TryPeek(out var slot))
        {
            if (Slots.Contains(slot)) { Remove(slot); undo.Pop(); return; }
            undo.Pop();
        }
    }

    public void TogglePlacement(CCSPlayerController player)
    {
        if (!storageHealthy || string.IsNullOrEmpty(map)) throw new IOException("Slot storage is unavailable");
        if (!Placing.Remove(player))
        {
            var settings = Settings(player);
            if (Material(settings.MapNumber) == null || settings.Width <= 0 || settings.Height <= 0 || settings.Depth <= 0)
                throw new InvalidOperationException("Choose the preview, width, height and depth first");
            Placing.Add(player);
            plugin.MenuManager!._selectedMaterial.Remove(player);
            previews[player] = settings.MapNumber;
            player.PrintToChat($"[Map Integration] Ping to save slots. Preview MAP {settings.MapNumber} is visible to admins only.");
        }
        Refresh();
    }

    public void SetPreview(CCSPlayerController player, int number)
    {
        if (number == 0)
        {
            previews.Remove(player);
            Placing.Remove(player);
            if (CurrentMapNumber == 0 && previews.Count == 0) ClearEntities();
        }
        else
        {
            Settings(player).MapNumber = number;
            previews[player] = number;
        }
        Refresh();
    }

    public void ClearEntities()
    {
        foreach (var slot in Slots)
        {
            if (slot.EntityProp?.IsValid == true) slot.EntityProp.Remove();
            slot.EntityProp = null;
        }
        renderedMaterial = null;
    }

    public void Stop() { running = false; ClearEntities(); Placing.Clear(); previews.Clear(); }

    public void Refresh()
    {
        if (!running || string.IsNullOrEmpty(map)) return;
        if (!plugin.AdvertisementsVisible) { ClearEntities(); return; }
        if (!plugin.Config.MapIntegration.Enabled) { Placing.Clear(); previews.Clear(); }
        foreach (var player in Placing.Concat(previews.Keys).Distinct().ToArray())
            if (!player.IsValid || !AdminManager.PlayerHasPermissions(player, plugin.Config.AdminFlag))
            { Placing.Remove(player); previews.Remove(player); }
        CurrentMapNumber = 0;
        if (plugin.Config.MapIntegration.Enabled)
        {
            CurrentMapNumber = ReadCurrentMapNumber();
            if (!MapIntegrationRules.ShouldShowSeries(ReadSeriesLength(), plugin.Config.MapIntegration.ShowInBestOfOne))
                CurrentMapNumber = 0;
        }
        int number = CurrentMapNumber > 0 ? CurrentMapNumber : previews.Values.LastOrDefault();
        var material = plugin.Config.MapIntegration.Enabled ? Material(number) : null;
        if (material != renderedMaterial) { ClearEntities(); renderedMaterial = material; }
        if (material == null) return;
        foreach (var slot in Slots)
        {
            if (slot.EntityProp?.IsValid == true) continue;
            slot.EntityProp = plugin.PluginUtils!.CreateDecal(new Vector(slot.posX, slot.posY, slot.posZ),
                new QAngle(slot.angleX, slot.angleY, slot.angleZ), material, slot.width, slot.height, false, slot.depth, slot.opacity, slot.solid);
        }
    }

    private int ReadCurrentMapNumber()
    {
        try { return CurrentMapNumberCapability.Get(); }
        catch (KeyNotFoundException) { return 0; }
    }

    private int ReadSeriesLength()
    {
        try { return SeriesLengthCapability.Get(); }
        catch (KeyNotFoundException) { return 0; }
    }

    public void CheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (CurrentMapNumber > 0) return;
        foreach (var (info, player) in infoList)
        {
            if (player != null && player.IsValid && AdminManager.PlayerHasPermissions(player, plugin.Config.AdminFlag)) continue;
            foreach (var slot in Slots)
                if (slot.EntityProp?.IsValid == true) info.TransmitEntities.Remove(slot.EntityProp);
        }
    }
}

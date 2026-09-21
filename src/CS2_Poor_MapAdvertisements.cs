using CounterStrikeSharp.API.Core;
using CS2_Poor_MapAdvertisements.Config;
using CS2_Poor_MapAdvertisements.Managers;
using CS2_Poor_MapAdvertisements.Menu;
using CS2_Poor_MapAdvertisements.Utils;
using Microsoft.Extensions.Logging;

namespace CS2_Poor_MapAdvertisements;
public class CS2_Poor_MapAdvertisements : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "CS2_Poor_MapAdvertisements";

    public override string ModuleVersion => "1.0";

    public override string ModuleAuthor => "Letaryat | github.com/letaryat";

    public override string ModuleDescription => "Creates map advertisements.";

    public required PluginConfig Config { get; set; }

    public static CS2_Poor_MapAdvertisements? Instance { get; private set; }

    public EventManager? EventManager { get; private set; }
    public PropManager? PropManager { get; private set; }
    public MapIntegration? MapIntegration { get; private set; }
    public bool AdvertisementsVisible { get; set; } = true;

    public PluginUtils? PluginUtils { get; private set; }
    public CommandsManager? CommandsManager { get; private set; }

    public PluginMenu? MenuManager {get; private set;}
    public override void Load(bool hotReload)
    {
        Console.WriteLine("Loaded CS2_Poor_MapAdvertisements");
        Instance = this;

        EventManager = new EventManager(this);
        PluginUtils = new PluginUtils(this);
        CommandsManager = new CommandsManager(this);
        PropManager = new PropManager(this);
        MenuManager = new PluginMenu(this);
        MapIntegration = new MapIntegration(this);
        MapIntegration.Start();

        PropManager.MigrateLegacyMapFiles();

        EventManager.RegisterEvents();
        CommandsManager.RegisterCommands();

    }

    public void OnConfigParsed(PluginConfig config)
    {
        config.MapIntegration ??= new();
        Config = config;
    }
    public override void Unload(bool hotReload)
    {
        EventManager?.RestorePingCooldown();
        MapIntegration?.Stop();
        Console.WriteLine("Unloaded CS2_Poor_MapAdvertisements");
    }

    public void DebugMode(string message)
    {
        if (Config.Debug)
        {
            Logger.LogInformation(message);
        }
    }

    public void SetAdvertisementsVisible(bool visible)
    {
        if (AdvertisementsVisible == visible) return;
        AdvertisementsVisible = visible;

        if (!visible)
        {
            EventManager?.RemoveAdvertisementEntities();
            MapIntegration?.ClearEntities();
            return;
        }

        PropManager?.SpawnProps();
        MapIntegration?.Refresh();
    }

}

using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;


namespace CS2_Poor_MapAdvertisements.Config
{
    public class PluginConfig : BasePluginConfig
    {
        [JsonPropertyName("Admin Flag")]
        public string AdminFlag { get; set; } = "@css/root";

        [JsonPropertyName("Vip Flag")]
        public string VipFlag { get; set; } = "@vip/noadv";

        [JsonPropertyName("Props Path")]
        public string[] Props { get; set; } = [];

        [JsonPropertyName("Solid Material Variants")]
        public Dictionary<string, string> SolidMaterialVariants { get; set; } = [];

        [JsonPropertyName("MatchZy Map Integration")]
        public MapIntegrationConfig MapIntegration { get; set; } = new();

        [JsonPropertyName("Custom Position Values")]
        public int[] customPositionValues { get; set; } = [];

        [JsonPropertyName("Custom Angle Values")]
        public int[] customAngleValues { get; set; } = [];

        [JsonPropertyName("Enable commands")]
        public bool EnableCMD { get; set; } = true;

        [JsonPropertyName("Debug Mode")]
        public bool Debug { get; set; } = true;

    }

    public class MapIntegrationConfig
    {
        public bool Enabled { get; set; } = false;
        [JsonPropertyName("Show In Best Of One")]
        public bool ShowInBestOfOne { get; set; } = true;
        public string[] Materials { get; set; } = [];
    }
}

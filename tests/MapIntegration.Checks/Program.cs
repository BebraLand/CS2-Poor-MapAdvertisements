using MatchZy;
using CS2_Poor_MapAdvertisements.Managers;

int checks = 0;
void Check(bool condition, string name)
{
    checks++;
    if (!condition) throw new Exception(name);
}
foreach (int total in new[] { 1, 3, 5 })
{
    for (int index = 0; index < total; index++)
        Check(MapAdvertisementRules.Select(true, index, total, Enumerable.Repeat("de_mirage", total).ToArray(), "de_mirage") == index + 1, $"BO{total} MAP {index + 1}");
    Check(MapAdvertisementRules.Select(true, total, total, Enumerable.Repeat("de_mirage", total).ToArray(), "de_mirage") == 0, "Series exhausted");
    Check(MapAdvertisementRules.Select(true, -1, total, Enumerable.Repeat("de_mirage", total).ToArray(), "de_mirage") == 0, "Invalid negative index");
    Check(MapAdvertisementRules.Select(false, 0, total, Enumerable.Repeat("de_mirage", total).ToArray(), "de_mirage") == 0, "Inactive/practice/finished series");
}
Check(MapAdvertisementRules.Select(true, 2, 5, ["de_mirage", "de_nuke", "de_ancient", "de_anubis", "de_inferno"], "de_ancient") == 3, "Current authoritative index");
Check(MapAdvertisementRules.Select(true, 1, 3, ["de_mirage", "de_nuke", "de_ancient"], "de_mirage") == 0, "No next-map label on previous map");
Check(MapAdvertisementRules.Select(false, 1, 3, ["de_mirage", "de_mirage", "de_mirage"], "de_mirage") == 0, "Transition to same map stays hidden");
Check(MapAdvertisementRules.Select(true, 1, 3, ["de_mirage"], "de_mirage") == 0, "Incomplete veto list");
Check(MapAdvertisementRules.Select(true, 0, 1, ["workshop/123/de_mirage"], "de_mirage") == 1, "Workshop map path");
Check(MapAdvertisementRules.Select(true, 0, 1, ["12345678"], "custom_map") == 1, "Workshop numeric ID after transition");
Check(MapIntegrationRules.ShouldShowSeries(1, true), "BO1 enabled");
Check(!MapIntegrationRules.ShouldShowSeries(1, false), "BO1 disabled");
Check(MapIntegrationRules.ShouldShowSeries(3, false), "BO3 unaffected");
Check(MapIntegrationRules.ShouldShowSeries(0, false), "Older MatchZy fails open");
Check(AdvertisementVisibilityRules.ShouldHide(false, AdvertisementAudience.Everyone, AdvertisementPreference.Visible, false, false, "advert_decals_"), "Emergency overrides personal visible");
Check(AdvertisementVisibilityRules.ShouldHide(false, AdvertisementAudience.Everyone, AdvertisementPreference.Auto, false, false, "advert_prop1"), "Emergency hides props");
Check(!AdvertisementVisibilityRules.ShouldHide(false, AdvertisementAudience.Everyone, AdvertisementPreference.Auto, false, false, "map_decoration"), "Emergency keeps unrelated entities");
Check(!AdvertisementVisibilityRules.ShouldHide(true, AdvertisementAudience.Everyone, AdvertisementPreference.Auto, true, false, "advert_decals_"), "Everyone mode shows active players");
Check(AdvertisementVisibilityRules.ShouldHide(true, AdvertisementAudience.Spectators, AdvertisementPreference.Auto, true, false, "advert_decals_"), "Spectator mode hides active players");
Check(!AdvertisementVisibilityRules.ShouldHide(true, AdvertisementAudience.Spectators, AdvertisementPreference.Auto, false, false, "advert_decals_"), "Spectator mode shows observers");
Check(AdvertisementVisibilityRules.ShouldHide(true, AdvertisementAudience.Everyone, AdvertisementPreference.Hidden, false, false, "advert_decals_"), "Personal hidden wins");
Check(!AdvertisementVisibilityRules.ShouldHide(true, AdvertisementAudience.Spectators, AdvertisementPreference.Visible, true, false, "advert_decals_"), "Personal visible overrides audience");
Check(AdvertisementVisibilityRules.ShouldHide(true, AdvertisementAudience.Everyone, AdvertisementPreference.Visible, false, true, "advert_decals__force"), "VIP filtering preserved");
var variants = new Dictionary<string, string> { ["materials/logo.vmat"] = "materials/logo_solid.vmat" };
Check(DecalAppearanceRules.ResolveMaterial("materials/logo.vmat", false, variants) == "materials/logo.vmat", "Current blend stays default");
Check(DecalAppearanceRules.ResolveMaterial("materials/logo.vmat", true, variants) == "materials/logo_solid.vmat", "Solid blend resolves variant");
Check(DecalAppearanceRules.NormalizeOpacity(0) == 10 && DecalAppearanceRules.NormalizeOpacity(120) == 100, "Opacity is bounded");
Console.WriteLine($"PASS: {checks} Map Integration checks.");

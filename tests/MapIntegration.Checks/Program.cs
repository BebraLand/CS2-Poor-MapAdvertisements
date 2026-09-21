using MatchZy;

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
Console.WriteLine($"PASS: {checks} Map Integration checks.");

namespace CS2_Poor_MapAdvertisements.Managers;

public static class AdvertisementVisibilityRules
{
    public static bool ShouldHide(bool advertisementsVisible, bool isVip, string? entityName)
        => entityName?.StartsWith("advert", StringComparison.Ordinal) == true
            && (!advertisementsVisible || (isVip && entityName.Contains("force", StringComparison.Ordinal)));
}

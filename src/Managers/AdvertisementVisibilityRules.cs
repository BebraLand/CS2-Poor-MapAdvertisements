namespace CS2_Poor_MapAdvertisements.Managers;

public enum AdvertisementAudience
{
    Everyone,
    Spectators
}

public enum AdvertisementPreference
{
    Auto,
    Hidden,
    Visible
}

public static class AdvertisementVisibilityRules
{
    public static bool ShouldHide(
        bool advertisementsVisible,
        AdvertisementAudience audience,
        AdvertisementPreference preference,
        bool isActivePlayer,
        bool isVip,
        string? entityName)
        => entityName?.StartsWith("advert", StringComparison.Ordinal) == true
            && (!advertisementsVisible
                || preference == AdvertisementPreference.Hidden
                || (preference == AdvertisementPreference.Auto
                    && ((isVip && entityName.Contains("force", StringComparison.Ordinal))
                        || (audience == AdvertisementAudience.Spectators && isActivePlayer))));
}

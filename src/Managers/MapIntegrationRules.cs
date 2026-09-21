namespace CS2_Poor_MapAdvertisements.Managers;

public static class MapIntegrationRules
{
    public static bool ShouldShowSeries(int seriesLength, bool showInBestOfOne)
        => seriesLength != 1 || showInBestOfOne;
}

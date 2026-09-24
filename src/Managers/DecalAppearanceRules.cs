namespace CS2_Poor_MapAdvertisements.Managers;

public static class DecalAppearanceRules
{
    public static int NormalizeOpacity(int opacity) => Math.Clamp(opacity, 10, 100);

    public static string ResolveMaterial(string material, bool solid, IReadOnlyDictionary<string, string> variants)
        => solid && variants.TryGetValue(material, out var variant) && !string.IsNullOrWhiteSpace(variant)
            ? variant
            : material;
}

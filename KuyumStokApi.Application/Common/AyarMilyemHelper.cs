namespace KuyumStokApi.Application.Common;

/// <summary>
/// Ayar değerlerini milyem'e çeviren helper sınıf.
/// Doğrudan ayar-milyem eşleştirmesi kullanılır (hesaplama yapılmaz).
/// </summary>
public static class AyarMilyemHelper
{
    /// <summary>
    /// Ayar değerlerinin milyem karşılıkları (standart altın ayarları ve özel durumlar).
    /// </summary>
    private static readonly Dictionary<string, int> AyarMilyemMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Standart altın ayarları
        { "1", 42 },      // 41,66 → 42
        { "2", 83 },      // 83,33 → 83
        { "3", 125 },     // 125 → 125
        { "4", 167 },     // 166,66 → 167
        { "5", 208 },     // 208,33 → 208
        { "6", 250 },     // 250 → 250
        { "7", 292 },     // 291,66 → 292
        { "8", 333 },     // 333,28 → 333
        { "9", 375 },     // 374,94 → 375
        { "10", 417 },    // 416,6 → 417
        { "11", 458 },    // 458,33 → 458
        { "12", 500 },    // 500 → 500
        { "13", 542 },    // 541,66 → 542
        { "14", 585 },    // 585 → 585
        { "15", 625 },    // 625 → 625
        { "16", 667 },    // 666,66 → 667
        { "17", 708 },    // 708,33 → 708
        { "18", 750 },    // 750 → 750
        { "19", 792 },    // 791,66 → 792
        { "20", 833 },    // 833,33 → 833
        { "21", 875 },    // 874,86 → 875
        { "22", 916 },    // 916 → 916
        { "23", 958 },    // 958,33 → 958
        { "24", 1000 },   // 1000 → 1000
        
        // Özel durumlar
        { "925", 925 },           // Gümüş
        { "STERLING", 925 },      // Gümüş
        { "PT950", 950 },         // Platin
        { "950", 950 },           // Platin
        { "PT900", 900 },         // Platin
        { "900", 900 }            // Platin
    };

    /// <summary>
    /// Ayar string'ini milyem değerine çevirir.
    /// Doğrudan eşleştirme kullanılır, hesaplama yapılmaz.
    /// </summary>
    /// <param name="ayar">Ayar değeri (örn: "14", "18", "22", "24", "925", "Pt950")</param>
    /// <returns>Milyem değeri (0-1000 arası), ayar geçersizse null</returns>
    public static int? GetMilyemFromAyar(string? ayar)
    {
        if (string.IsNullOrWhiteSpace(ayar))
            return null;

        var normalizedAyar = ayar.Trim();
        
        if (AyarMilyemMap.TryGetValue(normalizedAyar, out int milyem))
            return milyem;

        return null;
    }
}


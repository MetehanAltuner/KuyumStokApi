using System.Collections.Generic;

namespace KuyumStokApi.Application.DTOs.Dashboard
{
    /// <summary>
    /// Risk skor aralığı için DTO
    /// </summary>
    public sealed class RiskScoreRangeDto
    {
        public int Min { get; set; }
        public int Max { get; set; }
        public string Level { get; set; } = string.Empty; // "Düşük", "Orta", "Yüksek", "Kritik"
        public string Description { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty; // Frontend için hex color
    }

    /// <summary>
    /// Risk skor sözlüğü için DTO
    /// </summary>
    public sealed class RiskScoreLegendDto
    {
        public List<RiskScoreRangeDto> Ranges { get; set; } = new();
    }
}


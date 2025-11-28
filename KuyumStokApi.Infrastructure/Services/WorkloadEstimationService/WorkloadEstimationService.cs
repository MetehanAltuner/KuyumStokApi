using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KuyumStokApi.Infrastructure.Services.WorkloadEstimationService
{
    /// <summary>
    /// İş yükü tahmini servisi - Basit istatistiksel yöntemler ve ML.NET kullanır
    /// </summary>
    public sealed class WorkloadEstimationService
    {
        private readonly ILogger<WorkloadEstimationService> _logger;

        public WorkloadEstimationService(ILogger<WorkloadEstimationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Moving average ile iş yükü tahmini
        /// </summary>
        /// <param name="dailyCounts">Günlük işlem sayıları listesi</param>
        /// <param name="windowSize">Moving average penceresi (default: 7 gün)</param>
        /// <returns>Tahmin edilen işlem sayısı</returns>
        public int EstimateWithMovingAverage(List<int> dailyCounts, int windowSize = 7)
        {
            if (dailyCounts == null || dailyCounts.Count == 0)
                return 0;

            try
            {
                // Son N günün ortalaması
                var recentDays = dailyCounts.TakeLast(windowSize).ToList();
                if (!recentDays.Any())
                    return 0;

                return (int)Math.Round(recentDays.Average());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Moving average hesaplanırken hata oluştu");
                return dailyCounts.LastOrDefault();
            }
        }

        /// <summary>
        /// Basit lineer regresyon ile iş yükü tahmini (trend bazlı)
        /// </summary>
        /// <param name="dailyCounts">Günlük işlem sayıları listesi (en eski -> en yeni)</param>
        /// <returns>Tahmin edilen işlem sayısı</returns>
        public int EstimateWithLinearRegression(List<int> dailyCounts)
        {
            if (dailyCounts == null || dailyCounts.Count < 2)
                return dailyCounts?.LastOrDefault() ?? 0;

            try
            {
                var n = dailyCounts.Count;
                var x = Enumerable.Range(1, n).Select(i => (double)i).ToArray();
                var y = dailyCounts.Select(c => (double)c).ToArray();

                // Basit lineer regresyon: y = a + b*x
                var sumX = x.Sum();
                var sumY = y.Sum();
                var sumXY = x.Zip(y, (xi, yi) => xi * yi).Sum();
                var sumX2 = x.Sum(xi => xi * xi);

                var b = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
                var a = (sumY - b * sumX) / n;

                // Bir sonraki gün için tahmin (x = n + 1)
                var nextDay = n + 1;
                var predicted = a + b * nextDay;

                return Math.Max(0, (int)Math.Round(predicted));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lineer regresyon hesaplanırken hata oluştu");
                return EstimateWithMovingAverage(dailyCounts);
            }
        }

        /// <summary>
        /// İş yükü yüzdesi hesaplama
        /// </summary>
        /// <param name="estimatedCount">Tahmin edilen işlem sayısı</param>
        /// <param name="averageCount">Ortalama işlem sayısı</param>
        /// <returns>İş yükü yüzdesi (sınırsız, 100'e sınırlanmaz)</returns>
        public int CalculateWorkloadPercentage(int estimatedCount, double averageCount)
        {
            if (averageCount <= 0)
                return 0;

            var percentage = (int)((estimatedCount / averageCount) * 100);
            return Math.Max(0, percentage); // Sınırsız yüzde
        }

        /// <summary>
        /// Yoğunluk seviyesi belirleme (Hibrit yaklaşım: Mutlak eşikler + Yüzde bazlı kontrol)
        /// </summary>
        /// <param name="estimatedCount">Tahmin edilen işlem sayısı</param>
        /// <param name="workloadPercentage">İş yükü yüzdesi</param>
        /// <returns>Yoğunluk seviyesi</returns>
        public string DetermineIntensityLevel(int estimatedCount, int workloadPercentage)
        {
            // Önce mutlak eşikleri kontrol et
            if (estimatedCount <= 5)
                return "Düşük";
            if (estimatedCount <= 15)
                return "Orta";
            if (estimatedCount > 15)
                return "Yüksek";

            // Yüzde bazlı kontrol (ek güvenlik - mutlak eşikler belirsizse)
            if (workloadPercentage >= 200)
                return "Yüksek";
            if (workloadPercentage >= 150)
                return "Orta";
            
            return "Düşük";
        }

        /// <summary>
        /// Durum mesajı oluşturma
        /// </summary>
        public string GenerateMessage(string intensityLevel, int estimatedCount)
        {
            return intensityLevel switch
            {
                "Düşük" => "Rahat bir gün olacak.",
                "Orta" => "Normal bir gün olacak.",
                "Yüksek" => $"Yoğun bir gün olacak — kasa ve personel hazır bulunsun! Tahmini işlem hacmi: {estimatedCount} işlem",
                _ => "İş yükü tahmini hazırlandı."
            };
        }
    }
}


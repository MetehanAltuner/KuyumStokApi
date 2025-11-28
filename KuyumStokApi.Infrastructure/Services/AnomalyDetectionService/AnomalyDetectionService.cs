using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KuyumStokApi.Infrastructure.Services.AnomalyDetectionService
{
    /// <summary>
    /// Anomali algılama servisi - Z-score tabanlı istatistiksel yöntem kullanır
    /// </summary>
    public sealed class AnomalyDetectionService
    {
        private readonly ILogger<AnomalyDetectionService> _logger;

        public AnomalyDetectionService(ILogger<AnomalyDetectionService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Z-score tabanlı anomali tespiti yapar
        /// </summary>
        /// <param name="values">Analiz edilecek değerler listesi</param>
        /// <param name="currentValue">Mevcut değer</param>
        /// <param name="threshold">Z-score eşik değeri (default: 2.0)</param>
        /// <returns>Anomali durumu ve Z-score değeri</returns>
        public AnomalyResult DetectAnomaly(List<decimal> values, decimal currentValue, double threshold = 2.0)
        {
            if (values == null || values.Count == 0)
            {
                return new AnomalyResult
                {
                    IsAnomaly = false,
                    ZScore = 0,
                    Mean = 0,
                    StandardDeviation = 0,
                    Message = "Yeterli veri yok"
                };
            }

            try
            {
                // Ortalama hesapla
                var mean = (double)values.Average();

                // Standart sapma hesapla
                var variance = values.Select(v => Math.Pow((double)v - mean, 2)).Average();
                var stdDev = Math.Sqrt(variance);

                if (stdDev == 0)
                {
                    return new AnomalyResult
                    {
                        IsAnomaly = false,
                        ZScore = 0,
                        Mean = mean,
                        StandardDeviation = 0,
                        Message = "Standart sapma sıfır - tüm değerler aynı"
                    };
                }

                // Z-score hesapla
                var zScore = ((double)currentValue - mean) / stdDev;

                // Anomali kontrolü
                var isAnomaly = Math.Abs(zScore) > threshold;

                string message;
                if (zScore < -threshold)
                {
                    message = $"Düşüş anomali tespit edildi (Z-score: {zScore:F2})";
                }
                else if (zScore > threshold)
                {
                    message = $"Artış anomali tespit edildi (Z-score: {zScore:F2})";
                }
                else
                {
                    message = "Normal değer aralığında";
                }

                return new AnomalyResult
                {
                    IsAnomaly = isAnomaly,
                    ZScore = zScore,
                    Mean = mean,
                    StandardDeviation = stdDev,
                    Message = message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Anomali tespiti yapılırken hata oluştu");
                return new AnomalyResult
                {
                    IsAnomaly = false,
                    ZScore = 0,
                    Mean = 0,
                    StandardDeviation = 0,
                    Message = "Hata: " + ex.Message
                };
            }
        }

        /// <summary>
        /// Satış verileri için anomali tespiti
        /// </summary>
        public AnomalyResult DetectSalesAnomaly(List<decimal> dailySales, decimal todaySales)
        {
            return DetectAnomaly(dailySales, todaySales, threshold: 2.0);
        }

        /// <summary>
        /// Stok seviyesi için anomali tespiti (kritik seviye kontrolü)
        /// </summary>
        public AnomalyResult DetectStockAnomaly(int currentQuantity, int minThreshold)
        {
            if (minThreshold <= 0)
            {
                return new AnomalyResult
                {
                    IsAnomaly = false,
                    ZScore = 0,
                    Mean = 0,
                    StandardDeviation = 0,
                    Message = "Minimum eşik değeri tanımlı değil"
                };
            }

            // Kritik stok seviyesi kontrolü
            var isAnomaly = currentQuantity < minThreshold;
            var riskScore = isAnomaly 
                ? (int)Math.Min(100, (1 - ((double)currentQuantity / (double)minThreshold)) * 100)
                : 0;

            return new AnomalyResult
            {
                IsAnomaly = isAnomaly,
                ZScore = isAnomaly ? -2.0 : 0,
                Mean = minThreshold,
                StandardDeviation = 0,
                Message = isAnomaly 
                    ? $"Kritik stok seviyesi: {currentQuantity} < {minThreshold} (Risk: %{riskScore})"
                    : "Stok seviyesi normal"
            };
        }
    }

    /// <summary>
    /// Anomali tespiti sonucu
    /// </summary>
    public sealed class AnomalyResult
    {
        public bool IsAnomaly { get; set; }
        public double ZScore { get; set; }
        public double Mean { get; set; }
        public double StandardDeviation { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}


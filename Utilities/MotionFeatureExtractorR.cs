using Microsoft.Maui.Devices.Sensors;

namespace CortriumBLE.Utilities;

public static class MotionFeatureExtractor
{
    public static double CalculateMotionScore(IReadOnlyList<AccelerometerData> batch)
    {
        if (batch == null || batch.Count < 2)
            return 0;

       var magnitudes = batch
    .Select(x => Math.Sqrt(
        x.X * x.X +
        x.Y * x.Y +
        x.Z * x.Z))
    .ToList();

        double mean = magnitudes.Average();

        double variance = magnitudes
            .Select(x => Math.Pow(x - mean, 2))
            .Average();

        return Math.Sqrt(variance);
    }
}
using Microsoft.Maui.Devices.Sensors;

namespace CortriumBLE.Services;

public enum ContextActivityState
{
    Rest,
    LightMovement,
    HighMovement
}

public class ContextAwarenessResult
{
    public ContextActivityState ActivityState { get; set; }
    public double MotionScore { get; set; }
    public string Explanation { get; set; } = "";
}

public class ContextAwarenessTestingService
{
    private readonly MotionFilteringService motionFilter = new();
    public ContextAwarenessResult Analyze(
        IReadOnlyList<AccelerometerData> accelerometerBatch)
    {
        double rawMotionScore = CalculateMotionScore(accelerometerBatch);
        double motionScore = motionFilter.AddAndFilter(rawMotionScore);
        var activity = ClassifyActivity(motionScore);

        return new ContextAwarenessResult
        {
            ActivityState = activity,
            MotionScore = motionScore,
            Explanation = $"Activity={activity}, Filtered={motionScore:F3}, Raw={rawMotionScore:F3}"
        };
    }

    private static double CalculateMotionScore(IReadOnlyList<AccelerometerData> batch)
    {
        if (batch == null || batch.Count < 2)
            return 0;

        var magnitudes = batch
            .Select(x =>
            {
                double m = Math.Sqrt(x.X * x.X + x.Y * x.Y + x.Z * x.Z);
                return Math.Abs(m - 1.0);
            })
            .ToList();

        double mean = magnitudes.Average();

        double variance = magnitudes
            .Select(x => Math.Pow(x - mean, 2))
            .Average();

        return Math.Sqrt(variance);
    }

    private static ContextActivityState ClassifyActivity(double motionScore)
    {
        if (motionScore < 0.08)
            return ContextActivityState.Rest;

        if (motionScore < 0.35)
            return ContextActivityState.LightMovement;

        return ContextActivityState.HighMovement;
    }
}
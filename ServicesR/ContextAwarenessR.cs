using CortriumBLE.Utilities;
using Microsoft.Maui.Devices.Sensors;

namespace CortriumBLE.Services;

public class ContextAwarenessService
{
    private readonly ContextAwareSeizureDetector _detector = new();

    public ContextDecision Analyze(
        double csi,
        double heartRate,
        IReadOnlyList<AccelerometerData> accelerometerBatch)
    {
        double motionScore = MotionFeatureExtractor.CalculateMotionScore(accelerometerBatch);

        return _detector.Evaluate(
            csi: csi,
            heartRate: heartRate,
            motionScore: motionScore
        );
    }
}
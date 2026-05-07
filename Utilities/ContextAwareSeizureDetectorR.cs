namespace CortriumBLE.Utilities;

public enum ActivityState
{
    Rest,
    LightMovement,
    HighMovement
}

public class ContextDecision
{
    public bool EcgCandidate { get; set; }
    public bool AlarmAllowed { get; set; }
    public bool AlarmSuppressed { get; set; }
    public ActivityState Activity { get; set; }
    public double MotionScore { get; set; }
    public double DynamicCsiThreshold { get; set; }
    public string Reason { get; set; } = "";
}

public class ContextAwareSeizureDetector
{
    private readonly double _baseCsiThreshold;
    private readonly double _restMotionThreshold;
    private readonly double _highMotionThreshold;

    private int _candidateWindows = 0;
    private int _clearWindows = 0;

    public ContextAwareSeizureDetector(
        double baseCsiThreshold = 500,
        double restMotionThreshold = 0.08,
        double highMotionThreshold = 0.35)
    {
        _baseCsiThreshold = baseCsiThreshold;
        _restMotionThreshold = restMotionThreshold;
        _highMotionThreshold = highMotionThreshold;
    }

    public ContextDecision Evaluate(
        double csi,
        double heartRate,
        double motionScore)
    {
        var activity = ClassifyActivity(motionScore);

        double dynamicThreshold = _baseCsiThreshold;

        if (activity == ActivityState.LightMovement)
            dynamicThreshold *= 1.25;

        if (activity == ActivityState.HighMovement)
            dynamicThreshold *= 1.75;

        bool ecgCandidate =
            csi > dynamicThreshold &&
            heartRate > 90;

        if (ecgCandidate)
        {
            _candidateWindows++;
            _clearWindows = 0;
        }
        else
        {
            _clearWindows++;

            if (_clearWindows >= 2)
                _candidateWindows = 0;
        }

        bool persistentCandidate = _candidateWindows >= 3;

        bool suppress =
            persistentCandidate &&
            activity == ActivityState.HighMovement;

        bool allowAlarm =
            persistentCandidate &&
            !suppress;

        return new ContextDecision
        {
            EcgCandidate = ecgCandidate,
            AlarmAllowed = allowAlarm,
            AlarmSuppressed = suppress,
            Activity = activity,
            MotionScore = motionScore,
            DynamicCsiThreshold = dynamicThreshold,
            Reason = BuildReason(ecgCandidate, allowAlarm, suppress, activity)
        };
    }

    private ActivityState ClassifyActivity(double motionScore)
    {
        if (motionScore < _restMotionThreshold)
            return ActivityState.Rest;

        if (motionScore < _highMotionThreshold)
            return ActivityState.LightMovement;

        return ActivityState.HighMovement;
    }

    private static string BuildReason(
        bool ecgCandidate,
        bool alarmAllowed,
        bool suppressed,
        ActivityState activity)
    {
        if (!ecgCandidate)
            return $"No seizure candidate. Activity: {activity}.";

        if (suppressed)
            return $"ECG candidate suppressed because motion is high. Activity: {activity}.";

        if (alarmAllowed)
            return $"Alarm allowed. ECG candidate persisted and activity is {activity}.";

        return $"ECG candidate detected, waiting for persistence. Activity: {activity}.";
    }
}
using UnityEngine;

public class RunManager : MonoBehaviour
{
    public enum DistanceAxisMode
    {
        WorldX,
        WorldZ,
        CustomAxis
    }

    public struct RunSummary
    {
        public float CurrentRunDistance;
        public float BestDistance;
        public int LastRunReward;

        public RunSummary(float currentRunDistance, float bestDistance, int lastRunReward)
        {
            CurrentRunDistance = currentRunDistance;
            BestDistance = bestDistance;
            LastRunReward = lastRunReward;
        }
    }

    [Header("REFERENCES")]
    [SerializeField] Transform trackedTransform;

    [Header("DISTANCE")]
    [SerializeField] DistanceAxisMode distanceAxisMode = DistanceAxisMode.WorldZ;
    [SerializeField] Vector3 customUphillAxis = Vector3.forward;

    [Header("REWARDS")]
    [SerializeField] float rewardPerDistanceUnit = 1f;
    [SerializeField] bool awardDodgeSurvivalBonus = true;
    [SerializeField] int dodgeSurvivalBonus = 50;

    public float CurrentRunDistance { get; private set; }
    public float BestDistance { get; private set; }
    public int LastRunReward { get; private set; }

    Vector3 runStartPosition;
    bool isRunActive;

    void Awake()
    {
        if (trackedTransform == null)
            trackedTransform = transform;
    }

    void Update()
    {
        if (!isRunActive || trackedTransform == null)
            return;

        CurrentRunDistance = CalculateProjectedDistance(trackedTransform.position);
    }

    public void BeginRun()
    {
        if (trackedTransform == null)
            return;

        runStartPosition = trackedTransform.position;
        CurrentRunDistance = 0f;
        isRunActive = true;
    }

    public void EndRun(bool dodgedFallingBike)
    {
        if (!isRunActive)
            return;

        CurrentRunDistance = CalculateProjectedDistance(trackedTransform.position);
        BestDistance = Mathf.Max(BestDistance, CurrentRunDistance);

        int distanceReward = Mathf.RoundToInt(CurrentRunDistance * rewardPerDistanceUnit);
        int dodgeReward = (awardDodgeSurvivalBonus && dodgedFallingBike) ? dodgeSurvivalBonus : 0;

        LastRunReward = distanceReward + dodgeReward;
        isRunActive = false;
    }

    public float GetCurrentDistance()
    {
        return CurrentRunDistance;
    }

    public RunSummary GetRunSummary()
    {
        return new RunSummary(CurrentRunDistance, BestDistance, LastRunReward);
    }

    float CalculateProjectedDistance(Vector3 currentPosition)
    {
        Vector3 axis = GetDistanceAxis();
        if (axis.sqrMagnitude <= Mathf.Epsilon)
            return 0f;

        Vector3 offset = currentPosition - runStartPosition;
        return Mathf.Max(0f, Vector3.Dot(offset, axis.normalized));
    }

    Vector3 GetDistanceAxis()
    {
        switch (distanceAxisMode)
        {
            case DistanceAxisMode.WorldX:
                return Vector3.right;
            case DistanceAxisMode.WorldZ:
                return Vector3.forward;
            case DistanceAxisMode.CustomAxis:
                return customUphillAxis;
            default:
                return Vector3.forward;
        }
    }
}

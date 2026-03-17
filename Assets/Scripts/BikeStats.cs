using UnityEngine;

[CreateAssetMenu(fileName = "BikeStats", menuName = "Motorcycle/Bike Stats")]
public class BikeStats : ScriptableObject
{
    [Header("MOVEMENT")]
    public float movePower = 120f;
    public float brakePower = 3000f;
    public float inNeutralBrakePower = 40f;

    [Header("HANDLING")]
    public float maxSteerRotateAngle = 30f;
    public float tiltingSpeed = 5f;
}

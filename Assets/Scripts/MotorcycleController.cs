using System.Collections;
using TMPro;
using UnityEngine;

public class MotorcycleController : MonoBehaviour
{
    [System.Serializable]
    public struct RuntimeBikeStats
    {
        public float movePower;
        public float brakePower;
        public float inNeutralBrakePower;
        public float maxSteerRotateAngle;
        public float tiltingSpeed;

        public static RuntimeBikeStats FromBase(BikeStats bikeStats)
        {
            RuntimeBikeStats runtimeStats = default;
            if (bikeStats == null)
                return runtimeStats;

            runtimeStats.movePower = bikeStats.movePower;
            runtimeStats.brakePower = bikeStats.brakePower;
            runtimeStats.inNeutralBrakePower = bikeStats.inNeutralBrakePower;
            runtimeStats.maxSteerRotateAngle = bikeStats.maxSteerRotateAngle;
            runtimeStats.tiltingSpeed = bikeStats.tiltingSpeed;
            return runtimeStats;
        }
    }

    Rigidbody motorcycleRigidbody;

    [Header("MOTORCYCLE VALUES")]
    [SerializeField] WheelCollider frontWheelCollider;
    [SerializeField] Transform[] SteeringPiecesTransforms;
    [SerializeField] BikeStats defaultStats;

    float movePower;
    float brakePower;
    float inNeutralBrakePower;
    float maxSteerRotateAngle;
    float tiltingSpeed;

    float currentSteerRotateAngle;
    bool isBraking;
    float currentBrakePower;

    [Header("SPEED TEXT")]
    [SerializeField] TMP_Text speedText;
    int speed = 0;

    [Header("GAME FLOW")]
    [SerializeField] GameManager gameManager;

    float speedDetector;
    float slideValue;
    bool canMoveToFront = true;

    [Header("BIKER")]
    [SerializeField] RiderFallController riderFallController;
    [SerializeField] float followArmSpeed;
    [SerializeField] Transform rightArmTransform;
    [SerializeField] Transform leftArmTransform;

    bool isMovingSoundPlaying = false;
    bool isInNeutralSoundPlaying = false;
    bool isbrakingSoundPlaying = false;

    void Start()
    {
        motorcycleRigidbody = GetComponent<Rigidbody>();
        if (gameManager == null)
            gameManager = GameManager.Instance;
        if (riderFallController == null)
            riderFallController = GetComponentInChildren<RiderFallController>();

        ApplyStats(RuntimeBikeStats.FromBase(defaultStats));
    }

    void Update()
    {
        DisplayMotorcycleSpeedText();
    }

    void FixedUpdate()
    {
        VerticalMove();
        HorizontalMove();
        CheckPutBrake();
        FollowAllSteeringPieceToWheelRotation();
        TiltingToMotorcycle();
    }

    public void ApplyStats(RuntimeBikeStats stats)
    {
        movePower = Mathf.Max(0f, stats.movePower);
        brakePower = Mathf.Max(0f, stats.brakePower);
        inNeutralBrakePower = Mathf.Max(0f, stats.inNeutralBrakePower);
        maxSteerRotateAngle = Mathf.Max(0f, stats.maxSteerRotateAngle);
        tiltingSpeed = Mathf.Max(0f, stats.tiltingSpeed);
    }

    void VerticalMove()
    {
        if (!IsInRidingState())
        {
            frontWheelCollider.motorTorque = 0f;
            return;
        }

        float verticalInput = Input.GetAxis("Vertical");
        frontWheelCollider.motorTorque = verticalInput * movePower;
        if (verticalInput > 0 && canMoveToFront)
        {
            SetMotorcycleMovingSound();
            MakeSlideOnMotorcycle();
            isInNeutralSoundPlaying = false;
        }
        else if (verticalInput == 0)
        {
            SetMotorcycleInNeutralSound();
            frontWheelCollider.brakeTorque = inNeutralBrakePower;
            isMovingSoundPlaying = false;
        }
        else
        {
            SetMotorcycleInNeutralSound();
            frontWheelCollider.brakeTorque = 0;
            isMovingSoundPlaying = false;
        }
    }

    void HorizontalMove()
    {
        if (!IsInRidingState())
        {
            currentSteerRotateAngle = 0f;
            frontWheelCollider.steerAngle = 0f;
            return;
        }

        float horizontalInput = Input.GetAxis("Horizontal");
        currentSteerRotateAngle = maxSteerRotateAngle * horizontalInput;
        frontWheelCollider.steerAngle = currentSteerRotateAngle;
        if (horizontalInput > 0.3f)
            FollowBikerArmsToSteeringWhenTurnRight();
        else if (horizontalInput < -0.3f)
            FollowBikerArmsToSteeringWhenTurnLeft();
        else
            FollowBikerArmsToSteeringWhenNoTurn();
    }

    void TiltingToMotorcycle()
    {
        float zPosition = frontWheelCollider.steerAngle;
        zPosition = Mathf.Clamp(zPosition, -25, 25);
        Quaternion newMotorcycleRotation = Quaternion.Euler(new Vector3(0f, transform.eulerAngles.y, -zPosition));
        transform.rotation = Quaternion.Lerp(transform.rotation, newMotorcycleRotation, tiltingSpeed * Time.fixedDeltaTime);
    }

    void CheckPutBrake()
    {
        if (!IsInRidingState())
        {
            isBraking = false;
            currentBrakePower = 0f;
            frontWheelCollider.brakeTorque = 0f;
            return;
        }

        isBraking = Input.GetKey(KeyCode.Space);
        currentBrakePower = isBraking ? brakePower : 0f;
        frontWheelCollider.brakeTorque = currentBrakePower;
        if (isBraking)
            SetMotorcycleBrakingSound();
        else
            isbrakingSoundPlaying = false;
    }

    void MakeSlideOnMotorcycle()
    {
        speedDetector = motorcycleRigidbody.velocity.sqrMagnitude;
        slideValue = Vector3.Dot(motorcycleRigidbody.velocity.normalized, transform.forward);
        if (slideValue > 0 && slideValue < 0.7f && speedDetector > 20f)
        {
            motorcycleRigidbody.velocity = Vector3.zero;
            SetMotorcycleBrakingSound();
            canMoveToFront = false;
            speedDetector = 0;
            StartCoroutine(ResetCanMoveToFrontBoolean());
        }
    }

    IEnumerator ResetCanMoveToFrontBoolean()
    {
        yield return new WaitForSeconds(2f);
        canMoveToFront = true;
    }

    void FollowAllSteeringPieceToWheelRotation()
    {
        foreach (Transform piece in SteeringPiecesTransforms)
            piece.localEulerAngles = new Vector3(piece.localEulerAngles.x, frontWheelCollider.steerAngle, piece.localEulerAngles.z);
    }

    void FollowBikerArmsToSteeringWhenTurnRight()
    {
        Quaternion newRightArmRotation = Quaternion.Euler(new Vector3(138f, 91f, 60f));
        rightArmTransform.localRotation = Quaternion.Lerp(rightArmTransform.localRotation, newRightArmRotation, followArmSpeed * Time.fixedDeltaTime);
        Quaternion newLeftArmRotation = Quaternion.Euler(new Vector3(90f, -51f, 0f));
        leftArmTransform.localRotation = Quaternion.Lerp(leftArmTransform.localRotation, newLeftArmRotation, followArmSpeed * Time.fixedDeltaTime);
    }

    void FollowBikerArmsToSteeringWhenTurnLeft()
    {
        Quaternion newRightArmRotation = Quaternion.Euler(new Vector3(78f, 91f, 60f));
        rightArmTransform.localRotation = Quaternion.Lerp(rightArmTransform.localRotation, newRightArmRotation, followArmSpeed * Time.fixedDeltaTime);
        Quaternion newLeftArmRotation = Quaternion.Euler(new Vector3(106f, -51f, 0f));
        leftArmTransform.localRotation = Quaternion.Lerp(leftArmTransform.localRotation, newLeftArmRotation, followArmSpeed * Time.fixedDeltaTime);
    }

    void FollowBikerArmsToSteeringWhenNoTurn()
    {
        Quaternion newRightArmRotation = Quaternion.Euler(new Vector3(103f, 91f, 60f));
        rightArmTransform.localRotation = Quaternion.Lerp(rightArmTransform.localRotation, newRightArmRotation, followArmSpeed * Time.fixedDeltaTime);
        Quaternion newLeftArmRotation = Quaternion.Euler(new Vector3(96f, -51f, 0f));
        leftArmTransform.localRotation = Quaternion.Lerp(leftArmTransform.localRotation, newLeftArmRotation, followArmSpeed * Time.fixedDeltaTime);
    }

    void DisplayMotorcycleSpeedText()
    {
        speed = Mathf.RoundToInt(motorcycleRigidbody.velocity.magnitude * 3.6f);
        speedText.text = speed.ToString();
    }

    void SetMotorcycleMovingSound()
    {
        if (!isMovingSoundPlaying)
        {
            AudioManager.Instance.StopMotorcycleEngineSound();
            AudioManager.Instance.PlayMotorcycleSpeedUpSound();
            isMovingSoundPlaying = true;
        }
    }

    void SetMotorcycleInNeutralSound()
    {
        if (!isInNeutralSoundPlaying)
        {
            AudioManager.Instance.StopMotorcycleSpeedUpSound();
            AudioManager.Instance.PlayMotorcycleEngineSound();
            isInNeutralSoundPlaying = true;
        }
    }

    void SetMotorcycleBrakingSound()
    {
        if (!isbrakingSoundPlaying)
        {
            AudioManager.Instance.StopMotorcycleEngineSound();
            AudioManager.Instance.StopMotorcycleSpeedUpSound();
            AudioManager.Instance.PlayMotorcycleBrakingSound();
            isbrakingSoundPlaying = true;
            isMovingSoundPlaying = false;
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Obstacle"))
        {
            if (riderFallController != null)
                riderFallController.BeginEjection();
        }
    }

    bool IsInRidingState()
    {
        return gameManager == null || gameManager.CurrentState == GameState.Riding;
    }
}

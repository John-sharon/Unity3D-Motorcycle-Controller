using System;
using UnityEngine;

public class RiderFallController : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] GameObject bikerParentGameObject;
    [SerializeField] Rigidbody motorcycleRigidbody;
    [SerializeField] GameManager gameManager;

    [Header("EJECTION IMPULSES")]
    [SerializeField] float riderForwardImpulse = 6f;
    [SerializeField] float riderUpImpulse = 3f;
    [SerializeField] float bikeBackwardImpulse = 2f;
    [SerializeField] float bikeSpinTorque = 8f;

    [Header("DODGE")]
    [SerializeField, Range(2f, 4f)] float dodgeWindowSeconds = 3f;
    [SerializeField] float lateralDodgeImpulse = 5f;
    [SerializeField] float hopImpulse = 2f;
    [SerializeField] float dodgeInputThreshold = 0.25f;

    public event Action OnDodgeSucceeded;
    public event Action OnDodgeFailed;

    Rigidbody[] bikerRigidbodies;
    Collider[] bikerColliders;
    Rigidbody riderRootRigidbody;

    bool isDodgeWindowActive;
    float dodgeWindowTimer;

    void Awake()
    {
        if (motorcycleRigidbody == null)
            motorcycleRigidbody = GetComponent<Rigidbody>();

        if (gameManager == null)
            gameManager = GameManager.Instance;

        CacheRiderParts();
        SetRagdollActive(false);
    }

    void Update()
    {
        if (!isDodgeWindowActive)
            return;

        dodgeWindowTimer -= Time.deltaTime;
        TryHandleDodgeInput();

        if (dodgeWindowTimer > 0f)
            return;

        isDodgeWindowActive = false;
        OnDodgeFailed?.Invoke();

        if (gameManager != null)
            gameManager.OnBikeHitRider();
    }

    public void BeginEjection()
    {
        if (isDodgeWindowActive)
            return;

        SetRagdollActive(true);
        ApplyEjectionImpulses();

        dodgeWindowTimer = dodgeWindowSeconds;
        isDodgeWindowActive = true;
    }

    public void SetRagdollActive(bool isActive)
    {
        if (bikerRigidbodies == null || bikerColliders == null)
            CacheRiderParts();

        bool shouldBeKinematic = !isActive;
        for (int i = 0; i < bikerRigidbodies.Length; i++)
        {
            bikerRigidbodies[i].isKinematic = shouldBeKinematic;
        }

        for (int i = 0; i < bikerColliders.Length; i++)
        {
            bikerColliders[i].enabled = isActive;
        }
    }

    void CacheRiderParts()
    {
        if (bikerParentGameObject == null)
            return;

        bikerRigidbodies = bikerParentGameObject.GetComponentsInChildren<Rigidbody>();
        bikerColliders = bikerParentGameObject.GetComponentsInChildren<Collider>();

        if (bikerRigidbodies.Length > 0)
            riderRootRigidbody = bikerRigidbodies[0];
    }

    void ApplyEjectionImpulses()
    {
        Vector3 riderImpulse = (transform.forward * riderForwardImpulse) + (transform.up * riderUpImpulse);

        for (int i = 0; i < bikerRigidbodies.Length; i++)
        {
            bikerRigidbodies[i].AddForce(riderImpulse, ForceMode.Impulse);
        }

        if (motorcycleRigidbody == null)
            return;

        Vector3 bikeImpulse = (-transform.forward * bikeBackwardImpulse) + (transform.up * 0.5f);
        motorcycleRigidbody.AddForce(bikeImpulse, ForceMode.Impulse);
        motorcycleRigidbody.AddTorque(transform.right * bikeSpinTorque, ForceMode.Impulse);
    }

    void TryHandleDodgeInput()
    {
        if (riderRootRigidbody == null)
            return;

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        bool hasLateralInput = Mathf.Abs(horizontalInput) >= dodgeInputThreshold;
        bool hasHopInput = Input.GetKeyDown(KeyCode.Space);

        if (!hasLateralInput && !hasHopInput)
            return;

        Vector3 dodgeImpulse = Vector3.zero;
        if (hasLateralInput)
            dodgeImpulse += transform.right * Mathf.Sign(horizontalInput) * lateralDodgeImpulse;

        if (hasHopInput)
            dodgeImpulse += transform.up * hopImpulse;

        riderRootRigidbody.AddForce(dodgeImpulse, ForceMode.Impulse);

        isDodgeWindowActive = false;
        OnDodgeSucceeded?.Invoke();

        if (gameManager != null)
            gameManager.OnDodgeSurvived();
    }
}

using UnityEngine;

/// <summary>
/// Dedicated bike-body danger volume used after the rider is ejected.
///
/// Required setup (scene/prefab):
/// 1) Attach this component to a dedicated bike-body collider (not the main chassis collider).
/// 2) Choose detection mode:
///    - TriggerBased (recommended): set this collider's <see cref="Collider.isTrigger"/> = true.
///    - CollisionBased: keep this collider non-trigger and allow physical collisions.
/// 3) Keep a non-kinematic Rigidbody on the falling bike root so Unity produces trigger/collision callbacks.
/// 4) Configure consistent identifiers:
///    - Rider root object: tag/layer = Rider.
///    - Danger collider object: tag/layer = BikeDanger.
/// 5) Rider ragdoll limb colliders may live on child bones; this script accepts contacts from any child under rider root.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BikeDangerCollision : MonoBehaviour
{
    enum DetectionMode
    {
        TriggerBased,
        CollisionBased,
        Both
    }

    [Header("REFERENCES")]
    [SerializeField] Transform riderRoot;
    [SerializeField] Collider riderRootCollider;
    [SerializeField] GameManager gameManager;

    [Header("DETECTION")]
    [SerializeField] DetectionMode detectionMode = DetectionMode.TriggerBased;

    [Header("IDENTIFICATION")]
    [SerializeField] string riderTag = "Rider";
    [SerializeField] string bikeDangerTag = "BikeDanger";
    [SerializeField] string riderLayerName = "Rider";
    [SerializeField] string bikeDangerLayerName = "BikeDanger";

    bool hasRegisteredBikeHit;
    int riderLayer = -1;
    int bikeDangerLayer = -1;

    void Reset()
    {
        CacheLayerIds();
        gameManager = GameManager.Instance;

        Collider dangerCollider = GetComponent<Collider>();
        if (dangerCollider != null)
            dangerCollider.isTrigger = detectionMode != DetectionMode.CollisionBased;
    }

    void Awake()
    {
        CacheLayerIds();

        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (riderRootCollider != null && riderRoot == null)
            riderRoot = riderRootCollider.transform;

        ValidateConfiguredSetup();
    }

    void OnTriggerEnter(Collider other)
    {
        if (detectionMode == DetectionMode.CollisionBased)
            return;

        TryHandleRiderContact(other);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (detectionMode == DetectionMode.TriggerBased)
            return;

        TryHandleRiderContact(collision.collider);
    }

    void TryHandleRiderContact(Collider other)
    {
        if (hasRegisteredBikeHit || other == null)
            return;

        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (gameManager == null || gameManager.CurrentState != GameState.DodgingFallingBike)
            return;

        if (!IsRiderContact(other))
            return;

        hasRegisteredBikeHit = true;

        // Requirement: call the bike-hit callback and ensure transition to GameOver.
        gameManager.OnBikeHitRider();
        if (gameManager.CurrentState != GameState.GameOver)
            gameManager.SetState(GameState.GameOver);
    }

    bool IsRiderContact(Collider other)
    {
        if (riderRootCollider != null)
        {
            if (other == riderRootCollider)
                return true;

            if (other.transform.IsChildOf(riderRootCollider.transform))
                return true;
        }

        if (riderRoot != null && (other.transform == riderRoot || other.transform.IsChildOf(riderRoot)))
            return true;

        if (HasTagInHierarchy(other.transform, riderTag))
            return true;

        return IsLayerInHierarchy(other.transform, riderLayer);
    }

    bool HasTagInHierarchy(Transform current, string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            return false;

        while (current != null)
        {
            if (current.CompareTag(tagName))
                return true;

            current = current.parent;
        }

        return false;
    }

    bool IsLayerInHierarchy(Transform current, int layerId)
    {
        if (layerId < 0)
            return false;

        while (current != null)
        {
            if (current.gameObject.layer == layerId)
                return true;

            current = current.parent;
        }

        return false;
    }

    void CacheLayerIds()
    {
        riderLayer = LayerMask.NameToLayer(riderLayerName);
        bikeDangerLayer = LayerMask.NameToLayer(bikeDangerLayerName);
    }

    void ValidateConfiguredSetup()
    {
        Collider dangerCollider = GetComponent<Collider>();
        if (dangerCollider == null)
            return;

        bool expectsTrigger = detectionMode != DetectionMode.CollisionBased;
        if (dangerCollider.isTrigger != expectsTrigger)
        {
            string expected = expectsTrigger ? "true" : "false";
            Debug.LogWarning($"{nameof(BikeDangerCollision)} on '{name}' expects Collider.isTrigger = {expected} for {detectionMode} mode.", this);
        }

        if (bikeDangerLayer >= 0 && gameObject.layer != bikeDangerLayer)
        {
            Debug.LogWarning($"{nameof(BikeDangerCollision)} on '{name}' expects layer '{bikeDangerLayerName}' for the dedicated bike danger volume.", this);
        }

        if (!string.IsNullOrWhiteSpace(bikeDangerTag) && !CompareTag(bikeDangerTag))
        {
            Debug.LogWarning($"{nameof(BikeDangerCollision)} on '{name}' expects tag '{bikeDangerTag}' for the dedicated bike danger volume.", this);
        }
    }
}

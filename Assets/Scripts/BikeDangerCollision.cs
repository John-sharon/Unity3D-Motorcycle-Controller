using UnityEngine;

/// <summary>
/// Dedicated bike-body danger volume used after the rider is ejected.
/// 
/// Scene/prefab setup expected by this component:
/// 1. Attach this script to a dedicated collider on the bike body instead of the main impact collider.
/// 2. The dedicated collider should normally be configured as a trigger so it can overlap the ragdoll while
///    the bike keeps using its regular colliders for world collisions. Collision callbacks are also supported,
///    but trigger-based detection is the intended setup for this project.
/// 3. Keep the motorcycle rigidbody on the bike root so Unity can generate trigger/collision callbacks while the
///    falling bike is dynamic.
/// 4. Tag/layer the rider root consistently (for example tag = Rider, layer = Rider) and tag/layer this danger
///    volume consistently (for example tag = BikeDanger, layer = BikeDanger).
/// 5. Rider body-part colliders may live on child bones; this component treats any collider under the configured
///    rider root as a valid rider hit so you do not need to tag every ragdoll limb individually.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BikeDangerCollision : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] Transform riderRoot;
    [SerializeField] Collider riderRootCollider;
    [SerializeField] GameManager gameManager;

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
            dangerCollider.isTrigger = true;
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
        TryHandleRiderContact(other);
    }

    void OnCollisionEnter(Collision collision)
    {
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
        gameManager.OnBikeHitRider();

        if (gameManager.CurrentState != GameState.GameOver)
            gameManager.SetState(GameState.GameOver);
    }

    bool IsRiderContact(Collider other)
    {
        if (riderRootCollider != null && other == riderRootCollider)
            return true;

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
        if (dangerCollider != null && !dangerCollider.isTrigger)
        {
            Debug.LogWarning($"{nameof(BikeDangerCollision)} on '{name}' is intended to use a trigger collider. Collision callbacks remain supported if you keep it non-trigger.", this);
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

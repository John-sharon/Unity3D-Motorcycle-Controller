using UnityEngine;

public class BikeHealth : MonoBehaviour
{
    public const string DurabilityKey = "Save.Bike.Durability";

    [Header("REFERENCES")]
    [SerializeField] RunManager runManager;

    [Header("DURABILITY")]
    [SerializeField] float maxDurability = 100f;
    [SerializeField] float crashDamage = 25f;
    [SerializeField] float damagePerDistanceUnit = 0.05f;

    [Header("REPAIR")]
    [SerializeField] int repairCostPerPoint = 2;

    public float MaxDurability => maxDurability;
    public float CurrentDurability { get; private set; }
    public int RepairCost => Mathf.CeilToInt(GetMissingDurability() * repairCostPerPoint);

    bool damageAppliedThisRun;

    void Awake()
    {
        if (runManager == null)
            runManager = FindObjectOfType<RunManager>();

        LoadDurability();
    }

    void OnEnable()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
            gameManager.OnStateChanged += HandleStateChanged;
    }

    void OnDisable()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
            gameManager.OnStateChanged -= HandleStateChanged;
    }

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f)
            return;

        CurrentDurability = Mathf.Clamp(CurrentDurability - amount, 0f, maxDurability);
        SaveDurability();
    }

    public bool RepairFully()
    {
        int repairCost = RepairCost;
        if (repairCost <= 0)
            return true;

        EconomyManager economyManager = EconomyManager.Instance;
        if (economyManager == null || !economyManager.SpendCurrency(repairCost))
            return false;

        CurrentDurability = maxDurability;
        SaveDurability();
        economyManager.SaveToPrefs();
        return true;
    }

    float GetMissingDurability()
    {
        return Mathf.Max(0f, maxDurability - CurrentDurability);
    }

    void HandleStateChanged(GameState previousState, GameState nextState)
    {
        if (nextState == GameState.Riding)
        {
            damageAppliedThisRun = false;
            return;
        }

        if (damageAppliedThisRun)
            return;

        if (nextState != GameState.RunComplete && nextState != GameState.GameOver)
            return;

        float distanceDamage = runManager != null ? runManager.CurrentRunDistance * damagePerDistanceUnit : 0f;
        float crashPenalty = nextState == GameState.GameOver ? crashDamage : 0f;
        ApplyDamage(distanceDamage + crashPenalty);

        damageAppliedThisRun = true;
    }

    void LoadDurability()
    {
        float savedDurability = PlayerPrefs.GetFloat(DurabilityKey, maxDurability);
        CurrentDurability = Mathf.Clamp(savedDurability, 0f, maxDurability);
    }

    void SaveDurability()
    {
        PlayerPrefs.SetFloat(DurabilityKey, CurrentDurability);
        PlayerPrefs.Save();
    }
}

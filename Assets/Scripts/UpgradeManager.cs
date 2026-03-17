using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public const string EngineLevelKey = "Save.Upgrade.EngineLevel";
    public const string BrakesLevelKey = "Save.Upgrade.BrakesLevel";
    public const string HandlingLevelKey = "Save.Upgrade.HandlingLevel";

    [Header("REFERENCES")]
    [SerializeField] MotorcycleController motorcycleController;
    [SerializeField] BikeStats baseStats;

    [Header("UPGRADE COSTS")]
    [SerializeField] int baseEngineUpgradeCost = 100;
    [SerializeField] int baseBrakesUpgradeCost = 100;
    [SerializeField] int baseHandlingUpgradeCost = 100;
    [SerializeField] int costIncreasePerLevel = 50;

    [Header("UPGRADE EFFECTS")]
    [SerializeField] float movePowerPerLevel = 10f;
    [SerializeField] float brakePowerPerLevel = 200f;
    [SerializeField] float neutralBrakePerLevel = 5f;
    [SerializeField] float steerAnglePerLevel = 1.25f;
    [SerializeField] float tiltSpeedPerLevel = 0.35f;

    public int EngineLevel { get; private set; }
    public int BrakesLevel { get; private set; }
    public int HandlingLevel { get; private set; }

    void Awake()
    {
        if (motorcycleController == null)
            motorcycleController = FindObjectOfType<MotorcycleController>();

        LoadUpgrades();
    }

    void Start()
    {
        ApplyCurrentStats();

        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
            gameManager.OnStateChanged += HandleStateChanged;
    }

    void OnDestroy()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
            gameManager.OnStateChanged -= HandleStateChanged;
    }

    public void ApplyCurrentStats()
    {
        if (motorcycleController == null || baseStats == null)
            return;

        motorcycleController.ApplyStats(GetEffectiveStats());
    }

    public MotorcycleController.RuntimeBikeStats GetEffectiveStats()
    {
        MotorcycleController.RuntimeBikeStats runtimeStats = MotorcycleController.RuntimeBikeStats.FromBase(baseStats);

        runtimeStats.movePower += EngineLevel * movePowerPerLevel;

        runtimeStats.brakePower += BrakesLevel * brakePowerPerLevel;
        runtimeStats.inNeutralBrakePower += BrakesLevel * neutralBrakePerLevel;

        runtimeStats.maxSteerRotateAngle += HandlingLevel * steerAnglePerLevel;
        runtimeStats.tiltingSpeed += HandlingLevel * tiltSpeedPerLevel;

        return runtimeStats;
    }

    public int GetEngineUpgradeCost()
    {
        return Mathf.Max(0, baseEngineUpgradeCost + EngineLevel * costIncreasePerLevel);
    }

    public int GetBrakesUpgradeCost()
    {
        return Mathf.Max(0, baseBrakesUpgradeCost + BrakesLevel * costIncreasePerLevel);
    }

    public int GetHandlingUpgradeCost()
    {
        return Mathf.Max(0, baseHandlingUpgradeCost + HandlingLevel * costIncreasePerLevel);
    }

    public bool PurchaseEngineUpgrade()
    {
        return TryPurchaseUpgrade(GetEngineUpgradeCost(), () => EngineLevel++);
    }

    public bool PurchaseBrakesUpgrade()
    {
        return TryPurchaseUpgrade(GetBrakesUpgradeCost(), () => BrakesLevel++);
    }

    public bool PurchaseHandlingUpgrade()
    {
        return TryPurchaseUpgrade(GetHandlingUpgradeCost(), () => HandlingLevel++);
    }

    bool TryPurchaseUpgrade(int cost, System.Action onSuccess)
    {
        EconomyManager economyManager = EconomyManager.Instance;
        if (economyManager == null || !economyManager.SpendCurrency(cost))
            return false;

        onSuccess?.Invoke();
        SaveUpgrades();
        economyManager.SaveToPrefs();
        ApplyCurrentStats();
        return true;
    }

    void HandleStateChanged(GameState previousState, GameState nextState)
    {
        if (nextState == GameState.Riding)
            ApplyCurrentStats();
    }

    void LoadUpgrades()
    {
        EngineLevel = Mathf.Max(0, PlayerPrefs.GetInt(EngineLevelKey, 0));
        BrakesLevel = Mathf.Max(0, PlayerPrefs.GetInt(BrakesLevelKey, 0));
        HandlingLevel = Mathf.Max(0, PlayerPrefs.GetInt(HandlingLevelKey, 0));
    }

    void SaveUpgrades()
    {
        PlayerPrefs.SetInt(EngineLevelKey, EngineLevel);
        PlayerPrefs.SetInt(BrakesLevelKey, BrakesLevel);
        PlayerPrefs.SetInt(HandlingLevelKey, HandlingLevel);
        PlayerPrefs.Save();
    }
}

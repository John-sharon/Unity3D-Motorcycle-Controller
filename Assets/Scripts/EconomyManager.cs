using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    public int CurrencyBalance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadFromSave();
    }

    public void LoadFromSave()
    {
        CurrencyBalance = Mathf.Max(0, SaveSystem.LoadCurrencyBalance());
    }

    public void SaveToPrefs()
    {
        SaveSystem.SaveCurrencyBalance(CurrencyBalance);
    }

    public void AddCurrency(int amount)
    {
        if (amount <= 0)
            return;

        CurrencyBalance += amount;
    }

    public bool CanAfford(int amount)
    {
        return amount <= 0 || CurrencyBalance >= amount;
    }

    public bool SpendCurrency(int amount)
    {
        if (amount < 0 || !CanAfford(amount))
            return false;

        if (amount == 0)
            return true;

        CurrencyBalance -= amount;
        return true;
    }
}

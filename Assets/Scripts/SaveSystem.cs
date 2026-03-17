using UnityEngine;

public static class SaveSystem
{
    public const string CurrencyBalanceKey = "Save.CurrencyBalance";
    public const string BestDistanceKey = "Save.BestDistance";

    public static int LoadCurrencyBalance()
    {
        return PlayerPrefs.GetInt(CurrencyBalanceKey, 0);
    }

    public static float LoadBestDistance()
    {
        return PlayerPrefs.GetFloat(BestDistanceKey, 0f);
    }

    public static void SaveCurrencyBalance(int balance)
    {
        PlayerPrefs.SetInt(CurrencyBalanceKey, Mathf.Max(0, balance));
        PlayerPrefs.Save();
    }

    public static void SaveBestDistance(float bestDistance)
    {
        PlayerPrefs.SetFloat(BestDistanceKey, Mathf.Max(0f, bestDistance));
        PlayerPrefs.Save();
    }

    public static void SaveProgress(int currencyBalance, float bestDistance)
    {
        PlayerPrefs.SetInt(CurrencyBalanceKey, Mathf.Max(0, currencyBalance));
        PlayerPrefs.SetFloat(BestDistanceKey, Mathf.Max(0f, bestDistance));
        PlayerPrefs.Save();
    }
}

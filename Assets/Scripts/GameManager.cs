using System;
using UnityEngine;

public enum GameState
{
    PreRun,
    Riding,
    RiderEjected,
    DodgingFallingBike,
    RunComplete,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action<GameState, GameState> OnStateChanged;

    [SerializeField] RunManager runManager;
    [SerializeField] EconomyManager economyManager;

    public GameState CurrentState { get; private set; } = GameState.PreRun;

    bool hasPendingRunRewards;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (runManager == null)
            runManager = FindObjectOfType<RunManager>();

        if (economyManager == null)
            economyManager = FindObjectOfType<EconomyManager>();
    }

    public bool SetState(GameState nextState)
    {
        if (CurrentState == nextState || !CanTransition(CurrentState, nextState))
            return false;

        GameState previousState = CurrentState;
        CurrentState = nextState;
        HandleRunStateTransition(previousState, CurrentState);
        OnStateChanged?.Invoke(previousState, CurrentState);
        return true;
    }

    public void StartRun()
    {
        SetState(GameState.Riding);
    }

    public void OnCrashTriggered()
    {
        if (!SetState(GameState.RiderEjected))
            return;

        SetState(GameState.DodgingFallingBike);
    }

    public void OnDodgeSurvived()
    {
        SetState(GameState.RunComplete);
    }

    public void OnBikeHitRider()
    {
        SetState(GameState.GameOver);
    }

    public void EndRun()
    {
        if (CurrentState != GameState.RunComplete && CurrentState != GameState.GameOver)
            SetState(GameState.RunComplete);

        if (!hasPendingRunRewards || runManager == null)
            return;

        if (economyManager == null)
            economyManager = EconomyManager.Instance;

        if (economyManager != null)
            economyManager.AddCurrency(runManager.LastRunReward);

        float bestDistance = Mathf.Max(runManager.BestDistance, SaveSystem.LoadBestDistance());
        int currencyBalance = economyManager != null ? economyManager.CurrencyBalance : SaveSystem.LoadCurrencyBalance();
        SaveSystem.SaveProgress(currencyBalance, bestDistance);

        hasPendingRunRewards = false;
    }

    bool CanTransition(GameState currentState, GameState nextState)
    {
        switch (currentState)
        {
            case GameState.PreRun:
                return nextState == GameState.Riding;

            case GameState.Riding:
                return nextState == GameState.RiderEjected ||
                       nextState == GameState.RunComplete ||
                       nextState == GameState.GameOver;

            case GameState.RiderEjected:
                return nextState == GameState.DodgingFallingBike ||
                       nextState == GameState.GameOver;

            case GameState.DodgingFallingBike:
                return nextState == GameState.RunComplete ||
                       nextState == GameState.GameOver;

            case GameState.RunComplete:
            case GameState.GameOver:
                return nextState == GameState.PreRun ||
                       nextState == GameState.Riding;

            default:
                return false;
        }
    }

    void HandleRunStateTransition(GameState previousState, GameState nextState)
    {
        if (runManager == null)
            return;

        if (nextState == GameState.Riding)
        {
            hasPendingRunRewards = false;
            runManager.BeginRun();
            return;
        }

        if (nextState != GameState.RunComplete && nextState != GameState.GameOver)
            return;

        bool dodgedFallingBike = previousState == GameState.DodgingFallingBike && nextState == GameState.RunComplete;
        runManager.EndRun(dodgedFallingBike);
        hasPendingRunRewards = true;
    }
}

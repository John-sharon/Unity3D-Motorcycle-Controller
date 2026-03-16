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

    public GameState CurrentState { get; private set; } = GameState.PreRun;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool SetState(GameState nextState)
    {
        if (CurrentState == nextState || !CanTransition(CurrentState, nextState))
            return false;

        GameState previousState = CurrentState;
        CurrentState = nextState;
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
        SetState(GameState.RunComplete);
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
}

using UnityEngine;

public sealed class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public bool hasBlackLantern;
    public bool isBlackLanternLit;
    public bool offeringPuzzleSolved;
    public bool hasSeenNamedRiverLantern;
    public bool hasVisitedTownGate;
    public bool hasCheckedNormalRiverLantern;
    public bool hasCompletedWellEnding;

    public bool oldWellPuzzleSolved
    {
        get => hasCompletedWellEnding;
        set => hasCompletedWellEnding = value;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static GameStateManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject stateObject = new GameObject("GameStateManager");
        return stateObject.AddComponent<GameStateManager>();
    }
}

using UnityEngine;

public sealed class LampViewController : MonoBehaviour
{
    public enum LampViewState
    {
        None,
        Weak,
        Full
    }

    [SerializeField] private GameObject realityBackground;
    [SerializeField] private GameObject weakBackground;
    [SerializeField] private GameObject fullBackground;
    [SerializeField] private GameObject weakOverlay;
    [SerializeField] private GameObject fullOverlay;
    [SerializeField] private GameObject[] weakLampObjects;
    [SerializeField] private GameObject[] fullLampObjects;

    private bool lampViewEnabled;
    private LampViewState currentState;

    public bool IsLampViewEnabled => lampViewEnabled;
    public LampViewState CurrentState => currentState;

    private void Start()
    {
        ForceLampView(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleLampView();
        }
    }

    public void Configure(GameObject[] objects)
    {
        fullLampObjects = objects;
        ForceLampView(false);
    }

    public void ConfigureViewGroups(
        GameObject reality,
        GameObject weakBg,
        GameObject fullBgObject,
        GameObject weakOverlayObject,
        GameObject fullOverlayObject,
        GameObject[] weakObjects,
        GameObject[] fullObjects)
    {
        realityBackground = reality;
        weakBackground = weakBg;
        fullBackground = fullBgObject;
        weakOverlay = weakOverlayObject;
        fullOverlay = fullOverlayObject;
        weakLampObjects = weakObjects;
        fullLampObjects = fullObjects;
        ForceLampView(false);
    }

    public void ToggleLampView()
    {
        LampViewState availableState = ResolveAvailableViewState();
        if (availableState == LampViewState.None)
        {
            DialogueManager.Show("还没有灯。");
            return;
        }

        lampViewEnabled = !lampViewEnabled;
        currentState = lampViewEnabled ? availableState : LampViewState.None;
        ApplyObjects();
    }

    public void ForceLampView(bool enabled)
    {
        lampViewEnabled = enabled && ResolveAvailableViewState() != LampViewState.None;
        currentState = lampViewEnabled ? ResolveAvailableViewState() : LampViewState.None;
        ApplyObjects();
    }

    private void ApplyObjects()
    {
        bool weakEnabled = lampViewEnabled && currentState == LampViewState.Weak;
        bool fullEnabled = lampViewEnabled && currentState == LampViewState.Full;

        SetActive(realityBackground, !lampViewEnabled);
        SetActive(weakBackground, weakEnabled);
        SetActive(fullBackground, fullEnabled);
        SetActive(weakOverlay, weakEnabled);
        SetActive(fullOverlay, fullEnabled);
        ApplyGroup(weakLampObjects, weakEnabled);
        ApplyGroup(fullLampObjects, fullEnabled);

        LanternVisionController lanternVision = GetComponent<LanternVisionController>();
        if (lanternVision != null)
        {
            lanternVision.SetLanternVision(lampViewEnabled);
        }
    }

    private LampViewState ResolveAvailableViewState()
    {
        GameStateManager state = GameStateManager.Instance;
        if (state == null || !state.hasBlackLantern)
        {
            return LampViewState.None;
        }

        return state.isBlackLanternLit ? LampViewState.Full : LampViewState.Weak;
    }

    private static void ApplyGroup(GameObject[] group, bool active)
    {
        if (group == null)
        {
            return;
        }

        foreach (GameObject item in group)
        {
            SetActive(item, active);
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}

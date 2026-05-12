using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LanternVisionController : MonoBehaviour
{
    [SerializeField] private SceneBackgroundSet backgroundSet;
    [SerializeField] private bool lanternVisionEnabled;

    public bool IsLanternVisionEnabled => lanternVisionEnabled;
    public bool IsLanternVision => lanternVisionEnabled;

    private void Awake()
    {
        lanternVisionEnabled = false;
        ResolveSceneReferences();
        ApplyState();
    }

    private void Start()
    {
        SetLanternVision(false);
    }

    private void Update()
    {
        if (IsLampViewHandledSeparately())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleLanternVision();
        }
    }

    public void ToggleLanternVision()
    {
        GameStateManager state = GameStateManager.Instance;
        if (state == null || !state.hasBlackLantern)
        {
            DialogueManager.Show("还没有灯。");
            return;
        }

        lanternVisionEnabled = !lanternVisionEnabled;
        ApplyState();
    }

    public void SetLanternVision(bool enabled)
    {
        lanternVisionEnabled = enabled;
        ApplyState();
    }

    public void ForceRealityMode()
    {
        lanternVisionEnabled = false;
        ResolveSceneReferences();
        ApplyState();
    }

    private void ApplyState()
    {
        ResolveSceneReferences();
        DisableLanternVisionOverlayUi();

        if (backgroundSet != null)
        {
            backgroundSet.SetLanternVision(lanternVisionEnabled);
        }

        GameStateManager state = GameStateManager.Instance;
        if (state != null)
        {
            state.isLanternVision = lanternVisionEnabled;
            Debug.Log("[Chapter1State] isLanternVision = " + state.isLanternVision);
        }

        UpdateQHint();

    }

    private void ResolveSceneReferences()
    {
        if (backgroundSet == null || !backgroundSet.gameObject.scene.IsValid() || backgroundSet.gameObject.scene.name != gameObject.scene.name)
        {
            backgroundSet = FindObjectOfType<SceneBackgroundSet>();
        }
    }

    private void UpdateQHint()
    {
        if (UIManager.Instance == null)
        {
            return;
        }

        GameStateManager state = GameStateManager.Instance;
        if (state == null || !state.hasBlackLantern)
        {
            UIManager.Instance.HideQHint();
            return;
        }

        UIManager.Instance.ShowQHint(lanternVisionEnabled ? "Q 返回现实视角" : "Q 切换灯影视角");
    }

    private static void DisableLanternVisionOverlayUi()
    {
        GameObject canvasOverlay = FindObjectIncludingInactive("LanternVisionOverlay_UI");
        if (canvasOverlay != null)
        {
            canvasOverlay.SetActive(false);
        }
    }

    private static GameObject FindObjectIncludingInactive(string objectName)
    {
        foreach (GameObject item in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (item.name == objectName && item.hideFlags == HideFlags.None && item.scene.IsValid())
            {
                return item;
            }
        }

        return null;
    }

    private static bool IsLampViewHandledSeparately()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool chapterOneScene = sceneName == "Chapter1_TownGate" ||
            sceneName == "Chapter1_StoneBridge" ||
            sceneName == "Chapter1_GrandmaHouse" ||
            sceneName == "Chapter1_MourningHall" ||
            sceneName == "Chapter1_OldWell" ||
            sceneName == "Scene_Ch01_StoneBridge";

        return chapterOneScene && FindObjectOfType<LampViewController>() != null;
    }
}

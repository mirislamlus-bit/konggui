using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LanternVisionController : MonoBehaviour
{
    [SerializeField] private SceneBackgroundSet backgroundSet;
    [SerializeField] private GameObject lanternVisionOverlay;
    [SerializeField, Range(0f, 1f)] private float weakLanternOverlayAlpha = 0.25f;
    [SerializeField, Range(0f, 1f)] private float fullLanternOverlayAlpha = 0.38f;
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
        ResolveCanvasOverlay();

        if (backgroundSet != null)
        {
            backgroundSet.SetLanternVision(lanternVisionEnabled);
        }

        if (lanternVisionOverlay != null)
        {
            lanternVisionOverlay.SetActive(lanternVisionEnabled);
            Image overlayImage = lanternVisionOverlay.GetComponent<Image>();
            if (overlayImage != null)
            {
                Color color = overlayImage.color;
                color.a = GameStateManager.Instance != null && GameStateManager.Instance.isBlackLanternLit
                    ? fullLanternOverlayAlpha
                    : weakLanternOverlayAlpha;
                overlayImage.color = color;
                overlayImage.raycastTarget = false;
            }
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

    private void ResolveCanvasOverlay()
    {
        if (lanternVisionOverlay != null && lanternVisionOverlay.GetComponent<Image>() != null)
        {
            return;
        }

        GameObject canvasOverlay = FindObjectIncludingInactive("LanternVisionOverlay_UI");
        if (canvasOverlay == null)
        {
            canvasOverlay = FindObjectIncludingInactive("LanternVisionOverlay");
            if (canvasOverlay != null && canvasOverlay.GetComponent<Image>() == null)
            {
                canvasOverlay = null;
            }
        }

        if (canvasOverlay != null)
        {
            lanternVisionOverlay = canvasOverlay;
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

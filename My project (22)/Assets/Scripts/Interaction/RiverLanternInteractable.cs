using UnityEngine;

public sealed class RiverLanternInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private bool namedLantern;

    public void SetNamedLantern(bool value)
    {
        namedLantern = value;
    }

    public void Interact(PlayerController player)
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        LampViewController lampView = FindObjectOfType<LampViewController>();
        bool fullLampView = lampView != null &&
            lampView.IsLampViewEnabled &&
            lampView.CurrentState == LampViewController.LampViewState.Full;
        bool lanternVision = fullLampView || state.isLanternVision;

        if (!namedLantern)
        {
            state.hasCheckedNormalRiverLantern = true;
            Debug.Log("[Chapter1State] hasCheckedNormalRiverLantern = true");
            DialogueManager.Show("\u6797\u7167\u8424\uff1a\u6cb3\u706f\u8fd8\u4eae\u7740\uff0c\u50cf\u662f\u521a\u88ab\u4eba\u653e\u4e0b\u3002");
            return;
        }

        if (!state.isBlackLanternLit || !lanternVision)
        {
            DialogueManager.Show("\u6797\u7167\u8424\uff1a\u6c34\u9762\u592a\u6697\uff0c\u770b\u4e0d\u6e05\u6cb3\u706f\u4e0a\u7684\u5b57\u3002");
            return;
        }

        state.hasSeenNamedRiverLantern = true;
        Debug.Log("[Chapter1State] hasSeenNamedRiverLantern = true");
        RevealNamedLanternEffects();

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowDialogue(new[]
            {
                "\u6797\u7167\u8424\uff1a\u8fd9\u4e0a\u9762\u2026\u2026\u5199\u7684\u662f\u6211\u7684\u540d\u5b57\u3002",
                "\u6797\u7167\u8424\uff1a\u4e3a\u4ec0\u4e48\u6211\u7684\u540d\u5b57\u4f1a\u5728\u6cb3\u706f\u4e0a\uff1f"
            });
        }
    }

    private static void RevealNamedLanternEffects()
    {
        SetActiveIfFound("Content_RiverLantern_Ghost", true);
        SetActiveIfFound("Content_WaterReflection_LanternOnly", true);
    }

    private static void SetActiveIfFound(string objectName, bool active)
    {
        foreach (GameObject item in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (item.name == objectName && item.hideFlags == HideFlags.None && item.scene.IsValid())
            {
                item.SetActive(active);
                return;
            }
        }
    }
}

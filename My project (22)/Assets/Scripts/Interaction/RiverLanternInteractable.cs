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

        if (!namedLantern)
        {
            state.hasCheckedNormalRiverLantern = true;
            DialogueManager.Show("林照萤：河灯还亮着，像是刚被人放下。");
            return;
        }

        if (!state.isBlackLanternLit || !fullLampView)
        {
            DialogueManager.Show("林照萤：水面太暗，看不清河灯上的字。");
            return;
        }

        state.hasSeenNamedRiverLantern = true;
        RevealNamedLanternEffects();

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowDialogue(new[]
            {
                "林照萤：这上面……写的是我的名字。",
                "林照萤：为什么我的名字会在河灯上？",
                "去井边。"
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

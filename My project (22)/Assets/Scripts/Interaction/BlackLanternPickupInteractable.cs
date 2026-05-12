using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class BlackLanternPickupInteractable : MonoBehaviour, IInteractable, IInteractionPrompt
{
    private bool pickedUp;

    public string PromptText => "[E] 取走黑灯";
    public bool CanShowPrompt => !pickedUp;

    public void Interact(PlayerController player)
    {
        if (pickedUp)
        {
            return;
        }

        pickedUp = true;

        GameStateManager state = GameStateManager.EnsureInstance();
        state.hasBlackLantern = true;
        state.isBlackLanternLit = false;
        Debug.Log("[Chapter1State] hasBlackLantern = true");
        Debug.Log("[Chapter1State] isBlackLanternLit = false");
        AddInventoryItem();

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowDialogue(new[]
            {
                "林照萤：这盏黑灯……外婆以前从不让我碰。",
                "按 Q 切换灯影视角。"
            });
        }

        GameObject visual = GameObject.Find("BlackLantern_Unlit_Visual") ??
            GameObject.Find("BlackLantern_Unlit") ??
            GameObject.Find("Content_BlackLantern_Unlit");
        if (visual != null)
        {
            visual.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    private static void AddInventoryItem()
    {
        if (InventoryManager.Instance == null || InventoryManager.Instance.HasItem("BlackLantern_Unlit"))
        {
            return;
        }

        ItemData item = LoadBlackLanternItem();
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemData>();
            item.itemId = "BlackLantern_Unlit";
            item.itemName = "黑灯（未点燃）";
            item.description = "外婆留下的黑灯，还没有点燃。";
            item.itemType = ItemType.Lantern;
        }

        InventoryManager.Instance.AddItem(item);
    }

    private static ItemData LoadBlackLanternItem()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<ItemData>("Assets/ScriptableObjects/Items/BlackLantern_Unlit.asset");
#else
        return null;
#endif
    }
}

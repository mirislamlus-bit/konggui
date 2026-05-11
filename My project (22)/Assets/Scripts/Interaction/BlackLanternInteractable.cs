using UnityEngine;

public sealed class BlackLanternInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData blackLanternItem;
    [SerializeField] private string itemId = "BlackLantern_Unlit";
    [SerializeField] private string dialogue = "这盏黑灯……外婆以前从不让我碰。";

    public void Interact(PlayerController player)
    {
        GameStateManager state = FindObjectOfType<GameStateManager>();
        if (state == null)
        {
            state = new GameObject("GameStateManager").AddComponent<GameStateManager>();
        }

        state.hasBlackLantern = true;
        if (InventoryManager.Instance != null && blackLanternItem != null)
        {
            InventoryManager.Instance.AddItem(blackLanternItem);
        }
        Debug.Log("Added item: " + itemId);
        DialogueManager.Show(dialogue);
        gameObject.SetActive(false);
    }
}

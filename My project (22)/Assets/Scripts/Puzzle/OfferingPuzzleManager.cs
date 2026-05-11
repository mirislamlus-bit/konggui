using System.Collections.Generic;
using UnityEngine;

public sealed class OfferingPuzzleManager : MonoBehaviour
{
    [SerializeField] private string[] correctOrder =
    {
        "Offering_Apple",
        "Offering_Cake",
        "Offering_WineCup",
        "Offering_IncenseBurner",
        "Offering_Candle"
    };

    [SerializeField] private ItemData litBlackLanternItem;
    [SerializeField] private GameObject afterimageFlash;
    [SerializeField] private GameObject grandmaAfterimage;
    [SerializeField] private SpriteRenderer candleRenderer;
    [SerializeField] private Sprite litCandleSprite;

    private readonly Dictionary<int, string> slotToOffering = new Dictionary<int, string>();
    private readonly List<string> heldOfferings = new List<string>();

    public bool SlotHasOffering(int slotIndex, string offeringId = null)
    {
        if (!slotToOffering.TryGetValue(slotIndex, out string currentOffering))
        {
            return false;
        }

        return string.IsNullOrEmpty(offeringId) || currentOffering == offeringId;
    }

    public bool SlotIsEmpty(int slotIndex)
    {
        return !slotToOffering.ContainsKey(slotIndex);
    }

    public bool CanPickUpMore()
    {
        return heldOfferings.Count < 2;
    }

    public bool IsHoldingOffering(string offeringId)
    {
        return heldOfferings.Contains(offeringId);
    }

    public void Configure(ItemData litItem, GameObject flash, GameObject afterimage, SpriteRenderer candle, Sprite litCandle)
    {
        litBlackLanternItem = litItem != null ? litItem : litBlackLanternItem;
        afterimageFlash = flash != null ? flash : afterimageFlash;
        grandmaAfterimage = afterimage != null ? afterimage : grandmaAfterimage;
        candleRenderer = candle != null ? candle : candleRenderer;
        litCandleSprite = litCandle != null ? litCandle : litCandleSprite;
    }

    public void RegisterInitialOffering(int slotIndex, string offeringId)
    {
        slotToOffering[slotIndex] = offeringId;
    }

    public bool TryPickUpOffering(int slotIndex, string offeringId, Sprite icon = null)
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        if (state.offeringPuzzleSolved)
        {
            DialogueManager.Show("黑灯已经点起来了。");
            return false;
        }

        if (!slotToOffering.TryGetValue(slotIndex, out string currentOffering) || currentOffering != offeringId)
        {
            DialogueManager.Show("这里现在没有这个供品。");
            return false;
        }

        if (heldOfferings.Count >= 2)
        {
            DialogueManager.Show("一次最多只能拿两个供品。");
            return false;
        }

        ItemData item = GetOrCreateOfferingItem(offeringId, icon);
        if (InventoryManager.Instance != null && !InventoryManager.Instance.HasItem(item.itemId))
        {
            InventoryManager.Instance.AddItem(item);
        }

        heldOfferings.Add(offeringId);
        slotToOffering.Remove(slotIndex);
        DialogueManager.Show("拾取了供品。");
        return true;
    }

    public bool TryPlaceOffering(int slotIndex, string offeringId)
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        if (state.offeringPuzzleSolved)
        {
            DialogueManager.Show("黑灯已经点起来了。");
            return false;
        }

        if (slotToOffering.ContainsKey(slotIndex))
        {
            DialogueManager.Show("这个位置已经有供品了。");
            return false;
        }

        if (!HasHeldOffering(offeringId))
        {
            DialogueManager.Show("先拿起这个供品。");
            return false;
        }

        slotToOffering[slotIndex] = offeringId;
        RemoveHeldOffering(offeringId);
        RemoveOfferingFromInventory(offeringId);

        if (IsSolved())
        {
            Solve(state);
        }
        else
        {
            DialogueManager.Show("供桌微微一震。");
        }

        return true;
    }

    private bool IsSolved()
    {
        for (int i = 0; i < correctOrder.Length; i++)
        {
            if (!slotToOffering.TryGetValue(i, out string currentOffering) || currentOffering != correctOrder[i])
            {
                return false;
            }
        }

        return true;
    }

    private bool HasHeldOffering(string offeringId)
    {
        return heldOfferings.Contains(offeringId);
    }

    private void RemoveHeldOffering(string offeringId)
    {
        heldOfferings.Remove(offeringId);
    }

    private void Solve(GameStateManager state)
    {
        state.hasBlackLantern = true;
        state.offeringPuzzleSolved = true;
        state.isBlackLanternLit = true;

        if (litBlackLanternItem == null)
        {
            litBlackLanternItem = CreateRuntimeLitLanternItem();
        }

        if (InventoryManager.Instance != null && litBlackLanternItem != null)
        {
            if (InventoryManager.Instance.HasItem("BlackLantern_Unlit"))
            {
                InventoryManager.Instance.ReplaceItem("BlackLantern_Unlit", litBlackLanternItem);
            }
            else if (!InventoryManager.Instance.HasItem(litBlackLanternItem.itemId))
            {
                InventoryManager.Instance.AddItem(litBlackLanternItem);
            }
        }

        if (afterimageFlash != null)
        {
            afterimageFlash.SetActive(true);
        }

        if (grandmaAfterimage != null)
        {
            grandmaAfterimage.SetActive(true);
        }

        if (candleRenderer != null && litCandleSprite != null)
        {
            candleRenderer.sprite = litCandleSprite;
        }

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowDialogue(new[]
            {
                "外婆残影：灯点起来，才能看见被藏住的名字。",
                "林照萤：外婆……？",
                "外婆残影：去井边。",
                "黑灯已点燃。",
                "按 Q 可进入完整灯影视角。"
            });
        }
    }

    private readonly Dictionary<string, ItemData> runtimeOfferingItems = new Dictionary<string, ItemData>();

    private ItemData GetOrCreateOfferingItem(string offeringId, Sprite icon)
    {
        if (runtimeOfferingItems.TryGetValue(offeringId, out ItemData existing) && existing != null)
        {
            return existing;
        }

        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.itemId = offeringId + "_Inventory";
        item.itemType = ItemType.Story;
        item.icon = icon;

        switch (offeringId)
        {
            case "Offering_Apple":
                item.itemName = "苹果";
                item.description = "灵堂供桌上的苹果。";
                break;
            case "Offering_Cake":
                item.itemName = "糕点";
                item.description = "灵堂供桌上的糕点。";
                break;
            case "Offering_WineCup":
                item.itemName = "酒杯";
                item.description = "灵堂供桌上的酒杯。";
                break;
            case "Offering_IncenseBurner":
                item.itemName = "香炉";
                item.description = "灵堂供桌上的香炉。";
                break;
            case "Offering_Candle":
                item.itemName = "白蜡烛";
                item.description = "灵堂供桌上的白蜡烛。";
                break;
            default:
                item.itemName = offeringId;
                item.description = "供品。";
                break;
        }

        runtimeOfferingItems[offeringId] = item;
        return item;
    }

    private void RemoveOfferingFromInventory(string offeringId)
    {
        if (InventoryManager.Instance == null)
        {
            return;
        }

        if (runtimeOfferingItems.TryGetValue(offeringId, out ItemData item) && item != null)
        {
            InventoryManager.Instance.RemoveItem(item.itemId);
        }
    }

    private static ItemData CreateRuntimeLitLanternItem()
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.itemId = "BlackLantern_Lit";
        item.itemName = "黑灯（已点燃）";
        item.description = "灯芯亮起后，黑灯能照见被藏住的名字。";
        item.itemType = ItemType.Lantern;
        return item;
    }
}

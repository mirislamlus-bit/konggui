using System.Collections.Generic;
using UnityEngine;

public sealed class OfferingPuzzleManager : MonoBehaviour
{
    private readonly string[] correctOrder =
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
    [SerializeField] private Sprite[] offeringSprites;

    private readonly Dictionary<int, string> slotToOffering = new Dictionary<int, string>();
    private readonly List<string> heldOfferings = new List<string>();
    private readonly Dictionary<string, ItemData> runtimeOfferingItems = new Dictionary<string, ItemData>();
    private OfferingPuzzleUI activePuzzleUi;

    public void Configure(ItemData litItem, GameObject flash, GameObject afterimage, SpriteRenderer candle, Sprite litCandle)
    {
        litBlackLanternItem = litItem != null ? litItem : litBlackLanternItem;
        afterimageFlash = flash != null ? flash : afterimageFlash;
        grandmaAfterimage = afterimage != null ? afterimage : grandmaAfterimage;
        candleRenderer = candle != null ? candle : candleRenderer;
        litCandleSprite = litCandle != null ? litCandle : litCandleSprite;
    }

    public void ConfigureOfferingSprites(Sprite[] sprites)
    {
        offeringSprites = sprites;
    }

    public void OpenPuzzleUi()
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        if (state.offeringPuzzleSolved)
        {
            DialogueManager.Show("\u9ed1\u706f\u5df2\u7ecf\u70b9\u8d77\u6765\u4e86\u3002");
            return;
        }

        if (activePuzzleUi == null)
        {
            activePuzzleUi = OfferingPuzzleUI.Create(this, offeringSprites);
        }

        activePuzzleUi.Open();
        Debug.Log("[Chapter1Offering] Offering puzzle UI opened.");
    }

    public bool TrySolveUiOrder(string[] order)
    {
        if (order == null || order.Length != correctOrder.Length)
        {
            DialogueManager.Show("\u4f9b\u54c1\u8fd8\u6ca1\u6709\u6446\u5b8c\u3002");
            Debug.Log("[Chapter1Offering] Confirm failed: incomplete order.");
            return false;
        }

        for (int i = 0; i < correctOrder.Length; i++)
        {
            if (order[i] != correctOrder[i])
            {
                DialogueManager.Show("\u987a\u5e8f\u4e0d\u5bf9\u3002");
                Debug.Log("[Chapter1Offering] Confirm failed at slot " + i + ": " + order[i]);
                return false;
            }
        }

        Solve(GameStateManager.EnsureInstance());
        if (activePuzzleUi != null)
        {
            activePuzzleUi.Close();
        }

        Debug.Log("[Chapter1Offering] Offering puzzle solved from drag UI.");
        return true;
    }

    public void RegisterInitialOffering(int slotIndex, string offeringId)
    {
        slotToOffering[slotIndex] = offeringId;
    }

    public bool SlotHasOffering(int slotIndex, string offeringId = null)
    {
        return slotToOffering.TryGetValue(slotIndex, out string currentOffering) &&
            (string.IsNullOrEmpty(offeringId) || currentOffering == offeringId);
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

    public bool TryPickUpOffering(int slotIndex, string offeringId, Sprite icon = null)
    {
        DialogueManager.Show("\u8bf7\u5728\u653e\u5927\u7684\u4f9b\u684c\u4e0a\u91cd\u65b0\u6446\u653e\u4f9b\u54c1\u3002");
        return false;
    }

    public bool TryPlaceOffering(int slotIndex, string offeringId)
    {
        DialogueManager.Show("\u8bf7\u5728\u653e\u5927\u7684\u4f9b\u684c\u4e0a\u91cd\u65b0\u6446\u653e\u4f9b\u54c1\u3002");
        return false;
    }

    private void Solve(GameStateManager state)
    {
        state.hasBlackLantern = true;
        state.offeringPuzzleSolved = true;
        state.isBlackLanternLit = true;
        Debug.Log("[Chapter1State] offeringPuzzleSolved = true");
        Debug.Log("[Chapter1State] isBlackLanternLit = true");

        if (litBlackLanternItem == null)
        {
            litBlackLanternItem = CreateRuntimeLitLanternItem();
        }

        if (InventoryManager.Instance != null && litBlackLanternItem != null)
        {
            if (InventoryManager.Instance.HasItem("BlackLantern_Unlit"))
            {
                InventoryManager.Instance.ReplaceItem("BlackLantern_Unlit", litBlackLanternItem);
                Debug.Log("[Chapter1Inventory] Replaced BlackLantern_Unlit with BlackLantern_Lit.");
            }
            else if (!InventoryManager.Instance.HasItem(litBlackLanternItem.itemId))
            {
                InventoryManager.Instance.AddItem(litBlackLanternItem);
                Debug.Log("[Chapter1Inventory] Added BlackLantern_Lit.");
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
                "\u5916\u5a46\u6b8b\u5f71\uff1a\u706f\u70b9\u8d77\u6765\uff0c\u624d\u80fd\u770b\u89c1\u88ab\u85cf\u4f4f\u7684\u540d\u5b57\u3002",
                "\u6797\u7167\u8424\uff1a\u5916\u5a46\u2026\u2026\uff1f",
                "\u5916\u5a46\u6b8b\u5f71\uff1a\u53bb\u4e95\u8fb9\u3002"
            });
        }
    }

    private static ItemData CreateRuntimeLitLanternItem()
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.itemId = "BlackLantern_Lit";
        item.itemName = "\u9ed1\u706f\uff08\u5df2\u70b9\u71c3\uff09";
        item.description = "\u706f\u82af\u4eae\u8d77\u540e\uff0c\u9ed1\u706f\u80fd\u7167\u89c1\u88ab\u85cf\u4f4f\u7684\u540d\u5b57\u3002";
        item.itemType = ItemType.Lantern;
        return item;
    }
}

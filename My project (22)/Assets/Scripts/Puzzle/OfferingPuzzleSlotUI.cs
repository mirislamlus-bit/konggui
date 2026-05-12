using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class OfferingPuzzleSlotUI : MonoBehaviour, IDropHandler
{
    [SerializeField] private int slotIndex;
    [SerializeField] private OfferingPuzzleItemUI currentItem;

    public string OfferingId => currentItem != null ? currentItem.OfferingId : string.Empty;
    public bool IsEmpty => currentItem == null;

    public void Configure(int index)
    {
        slotIndex = index;
        name = "OfferingSlot_" + (slotIndex + 1).ToString("00");
    }

    public void Clear()
    {
        currentItem = null;
    }

    public void PlaceItem(OfferingPuzzleItemUI item)
    {
        if (item == null)
        {
            return;
        }

        if (currentItem != null && currentItem != item)
        {
            currentItem.ReturnHome();
        }

        if (item.CurrentSlot != null && item.CurrentSlot != this)
        {
            item.CurrentSlot.Clear();
        }

        currentItem = item;
        item.SetSlot(this);
        item.transform.SetParent(transform, false);

        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(88f, 88f);

        Debug.Log("[Chapter1Offering] Placed " + item.OfferingId + " in slot " + slotIndex);
    }

    public void OnDrop(PointerEventData eventData)
    {
        OfferingPuzzleItemUI item = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<OfferingPuzzleItemUI>()
            : null;
        PlaceItem(item);
    }
}

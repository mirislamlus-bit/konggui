using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryUI : MonoBehaviour
{
    private static readonly List<InventoryUI> Instances = new List<InventoryUI>();

    [SerializeField] private GameObject panel;
    [SerializeField] private Image[] slotImages;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Text itemListText;

    private void OnEnable()
    {
        if (!Instances.Contains(this))
        {
            Instances.Add(this);
        }

        Refresh();
    }

    private void OnDisable()
    {
        Instances.Remove(this);
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.B)) && panel != null)
        {
            Toggle();
        }
    }

    public static void ShowAll()
    {
        foreach (InventoryUI ui in Instances)
        {
            ui.Show();
        }
    }

    private void Toggle()
    {
        panel.SetActive(!panel.activeSelf);
        Refresh();
    }

    private void Show()
    {
        ResolveReferences();
        if (panel != null)
        {
            panel.SetActive(true);
            Refresh();
        }
    }

    public static void RefreshAll()
    {
        foreach (InventoryUI ui in Instances)
        {
            ui.Refresh();
        }
    }

    public void Refresh()
    {
        ResolveReferences();

        if (InventoryManager.Instance == null || slotImages == null)
        {
            return;
        }

        IReadOnlyList<ItemData> items = InventoryManager.Instance.Items;
        if (itemListText != null)
        {
            itemListText.text = items.Count == 0 ? "背包为空" : string.Empty;
        }

        for (int i = 0; i < slotImages.Length; i++)
        {
            Image slot = slotImages[i];
            ItemData item = i < items.Count ? items[i] : null;
            slot.sprite = item != null ? item.icon : null;
            slot.color = item != null ? Color.white : new Color(1f, 1f, 1f, 0.18f);

            if (itemListText != null && item != null)
            {
                itemListText.text += item.itemName + "\n";
            }

            Button button = slot.GetComponent<Button>() ?? slot.gameObject.AddComponent<Button>();
            button.onClick.RemoveAllListeners();
            if (item != null)
            {
                ItemData captured = item;
                button.onClick.AddListener(() => ShowDescription(captured));
            }
        }

        if (descriptionText != null && items.Count > 0 && string.IsNullOrEmpty(descriptionText.text))
        {
            ShowDescription(items[0]);
        }
    }

    private void ShowDescription(ItemData item)
    {
        if (descriptionText != null && item != null)
        {
            descriptionText.text = item.itemName + "\n" + item.description;
        }
    }

    public void Configure(GameObject rootPanel, Image[] slots, Text listText, Text detailText)
    {
        panel = rootPanel;
        slotImages = slots;
        itemListText = listText;
        descriptionText = detailText;
        ResolveReferences();
        Refresh();
    }

    private void ResolveReferences()
    {
        if (panel == null)
        {
            panel = transform.Find("InventoryPanel") != null ? transform.Find("InventoryPanel").gameObject : gameObject;
        }

        if (slotImages == null || slotImages.Length == 0)
        {
            slotImages = GetComponentsInChildren<Image>(true);
        }

        if (itemListText == null)
        {
            Transform found = transform.Find("InventoryPanel/InventoryItemListText");
            itemListText = found != null ? found.GetComponent<Text>() : null;
        }

        if (descriptionText == null)
        {
            Transform found = transform.Find("InventoryPanel/InventoryDescriptionText");
            descriptionText = found != null ? found.GetComponent<Text>() : null;
        }

    }
}

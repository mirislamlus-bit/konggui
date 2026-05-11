using System.Collections.Generic;
using UnityEngine;

public sealed class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [SerializeField] private List<ItemData> items = new List<ItemData>();

    public IReadOnlyList<ItemData> Items => items;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddItem(ItemData item)
    {
        if (item == null || HasItem(item.itemId))
        {
            return;
        }

        items.Add(item);
        InventoryUI.RefreshAll();
    }

    public void RemoveItem(string itemId)
    {
        items.RemoveAll(item => item != null && item.itemId == itemId);
        InventoryUI.RefreshAll();
    }

    public bool HasItem(string itemId)
    {
        return items.Exists(item => item != null && item.itemId == itemId);
    }

    public void ReplaceItem(string oldItemId, ItemData newItem)
    {
        RemoveItem(oldItemId);
        AddItem(newItem);
    }
}

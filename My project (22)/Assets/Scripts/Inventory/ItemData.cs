using UnityEngine;

public enum ItemType
{
    Lantern,
    Paper,
    Key,
    Story
}

[CreateAssetMenu(menuName = "JianDeng/Inventory/Item Data", fileName = "ItemData")]
public sealed class ItemData : ScriptableObject
{
    public string itemId;
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemType itemType;
}

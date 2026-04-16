using UnityEngine;

public enum ItemCategory
{
    All,
    Bed,
    Table,
    Chair,
    Sofa,
    Shelf
}

[CreateAssetMenu(fileName = "NewInventoryItem", menuName = "Inventory/Item")]
public class InventoryItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;

    [TextArea(3, 6)]
    public string description;

    public Sprite icon;

    [Header("3D Placement")]
    public GameObject prefab3D;

    [Header("Category")]
    public ItemCategory category;

    [Header("Economy")]
    [Min(0)]
    public float price = 0f;
    
    [Header("Manual Height Adjustment")]
    public bool allowManualHeightAdjust = false;
    public float manualHeightStep = 0.25f;
    public float minManualHeightOffset = -2f;
    public float maxManualHeightOffset = 5f;
}
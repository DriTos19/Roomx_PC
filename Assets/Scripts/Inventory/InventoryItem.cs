using UnityEngine;

[CreateAssetMenu(fileName = "NewInventoryItem", menuName = "Inventory/Item")]
public class InventoryItem : ScriptableObject
{
    public string itemID; // optional unique id (set in editor for save/serialization)
    public string itemName;
    [TextArea(3,6)] public string description;
    public Sprite icon;         // UI thumbnail
    public GameObject prefab;   // the furniture prefab to instantiate in the world

    public Category category = Category.Other;

    [Header("Placement options")]
    public bool placeable = true;      // if false, item can't be placed (useful for "templates")
    public bool singlePlaceOnly = true; // if true, placing consumes selection (fits your "place once" requirement)
    public bool allowRotation = true;  // used by placement system to allow rotation controls

    [Header("Preview / spawn offsets")]
    public Vector3 previewPositionOffset = Vector3.zero;
    public Vector3 previewRotationOffset = Vector3.zero;

    [Header("Optional / meta")]
    public int maxStack = 1; // furniture should be 1, but keep field for extensibility
    public string notes;     // internal notes

    public enum Category
    {
        Table,
        Chair,
        Bed,
        Shelf,
        Decor,
        Lighting,
        Other
    }

    public string itemCategory { get; set; }
}

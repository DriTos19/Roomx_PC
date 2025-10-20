using UnityEngine;

public class InventoryDatabase : MonoBehaviour
{
    public InventoryItem[] allItems;

    private void Awake()
    {
        // Loads all InventoryItem assets from Resources/InventoryItems/
        allItems = Resources.LoadAll<InventoryItem>("InventoryItems");
        Debug.Log("Loaded " + allItems.Length + " items.");
    }
}
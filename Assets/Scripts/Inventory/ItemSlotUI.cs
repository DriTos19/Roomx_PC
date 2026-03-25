using UnityEngine;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    public Image iconImage;

    private InventoryItemData itemData;
    private InventoryManager inventoryManager;

    public void Setup(InventoryItemData item, InventoryManager manager)
    {
        itemData = item;
        inventoryManager = manager;

        iconImage.sprite = item.icon;

        // CLICK = show details ONLY
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        // 👉 Show item image + description
        inventoryManager.ShowItemDetails(itemData);
    }
}
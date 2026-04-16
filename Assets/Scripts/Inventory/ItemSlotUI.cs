using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ItemSlotUI : MonoBehaviour, IPointerClickHandler
{
    public Image iconImage;
    public TMP_Text nameLabel;

    private InventoryItemData item;
    private InventoryControll inventory;

    public void Setup(InventoryItemData newItem, InventoryManager inventoryControll)
    {
        item = newItem;
        inventory = inventoryControll;

        if (iconImage != null)
            iconImage.sprite = item.icon;

        if (nameLabel != null)
            nameLabel.text = item.itemName;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (item == null || inventory == null)
            return;

        if (eventData.clickCount == 2)
        {
            inventory.PreviewItemFromInventory(item);
        }
        else if (eventData.clickCount == 1)
        {
            inventory.ShowItemDetails(item);
        }
    }
}
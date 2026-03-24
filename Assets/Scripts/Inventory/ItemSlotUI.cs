using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlotUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public Image iconImage;

    private InventoryItemData itemData;
    private InventoryManager inventoryManager;

    public void Setup(InventoryItemData item, InventoryManager manager)
    {
        itemData = item;
        inventoryManager = manager;

        iconImage.sprite = item.icon;

        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Show description AND register this as the selected item for purchase
        inventoryManager.ShowItemDetails(itemData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        inventoryManager.HideDescription();
    }

    private void OnClick()
    {
        // Clicking a slot selects it and triggers the purchase flow.
        // The actual purchase is initiated by the dedicated Purchase button in InventoryManager.
        // If you want a single-click-to-buy flow instead, uncomment the line below
        // and remove the separate purchaseButton from InventoryManager.
        // PurchaseManager.Instance.Purchase(itemData);

        PurchaseManager.Instance.SelectItem(itemData);
        inventoryManager.ShowItemDetails(itemData);
    }
}
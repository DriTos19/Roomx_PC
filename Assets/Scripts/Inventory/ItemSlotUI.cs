using UnityEngine;

using UnityEngine.UI;

using UnityEngine.EventSystems;
 
public class ItemSlotUI : MonoBehaviour,

    IPointerEnterHandler,

    IPointerExitHandler

{

    [Header("UI")]

    public Image iconImage;
 
    private InventoryItemData itemData;

    private InventoryManager inventoryManager;
 
    public void Setup(InventoryItemData item, InventoryManager manager)

    {

        itemData = item;

        inventoryManager = manager;
 
        iconImage.sprite = item.icon;

    }
 
    // Hover → show description

    public void OnPointerEnter(PointerEventData eventData)

    {

        if (itemData != null)

            inventoryManager.ShowItemDetails(itemData);

    }
 
    // Exit hover → hide description

    public void OnPointerExit(PointerEventData eventData)

    {

        inventoryManager.HideDescription();

    }
 
    // Click → close menu & start placement

    public void OnClick()

    {

        inventoryManager.CloseMenu();

        PlacementManager.Instance.StartPlacement(itemData);

    }

}
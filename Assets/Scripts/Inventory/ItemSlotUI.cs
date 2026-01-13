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
        inventoryManager.ShowItemDetails(itemData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        inventoryManager.HideDescription();
    }

    void OnClick()
    {
        inventoryManager.HideMenu();
        PlacementManager.Instance.StartPlacement(itemData);
    }
}
using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    public Image iconImage;

    private InventoryItemData itemData;

    public void Setup(InventoryItemData item, InventoryManager manager)
    {
        itemData = item;
        iconImage.sprite = item.icon;

        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        PlacementManager.Instance.StartPlacement(itemData);
    }
}
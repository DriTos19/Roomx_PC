using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryButton : MonoBehaviour
{
    public Image icon;
    public TMP_Text itemName;

    private InventoryItem item;

    public void Setup(InventoryItem newItem)
    {
        item = newItem;
        icon.sprite = item.icon;
        itemName.text = item.itemName;
    }

    public void OnClick()
    {
        InventoryUI.Instance.SelectItem(item);
    }
}

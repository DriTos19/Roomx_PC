using UnityEngine;
using UnityEngine.UI;
using TMPro; // only if you’re using TextMeshPro

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject buttonPrefab;     // Your InventoryButton prefab
    public Transform contentPanel;      // The Content object inside ScrollView

    [Header("Items")]
    public InventoryItem[] items;       // Drag your Chair.asset, Table.asset, etc. here

    void Start()
    {
        PopulateInventory();
    }

    void PopulateInventory()
    {
        foreach (InventoryItem item in items)
        {
            // Create a new button from prefab
            GameObject buttonObj = Instantiate(buttonPrefab, contentPanel);

            // Set icon + name
            Image icon = buttonObj.transform.Find("Icon").GetComponent<Image>();
            TextMeshProUGUI nameText = buttonObj.transform.Find("Name").GetComponent<TextMeshProUGUI>();

            if (icon != null) icon.sprite = item.icon;
            if (nameText != null) nameText.text = item.itemName;

            // Add OnClick
            Button btn = buttonObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnItemSelected(item));
        }
    }

    void OnItemSelected(InventoryItem item)
    {
        Debug.Log("Selected item: " + item.itemName);
        // later you’ll spawn the 3D prefab here
        // Example:
        // Instantiate(item.prefab, somePosition, Quaternion.identity);
    }

    public class Instance
    {
        public static void SelectItem(InventoryItem item)
        {
            throw new System.NotImplementedException();
        }
    }
}
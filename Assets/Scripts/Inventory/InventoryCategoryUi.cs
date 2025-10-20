using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryCategoryUi : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject categoryPanel;

    [Header("Category Buttons")]
    public Button allButton;
    public Button tableButton;
    public Button chairButton;
    public Button bedButton;

    [Header("Inventory Content")]
    public Transform contentPanel; // The ScrollView content
    public GameObject itemButtonPrefab; // prefab for each item (icon + name + desc)

    [Header("Items")]
    public List<InventoryItem> allItems; // assign all your ScriptableObjects here

    private void Start()
    {
        allButton.onClick.AddListener(() => ShowCategory("All"));
        tableButton.onClick.AddListener(() => ShowCategory("Table"));
        chairButton.onClick.AddListener(() => ShowCategory("Chair"));
        bedButton.onClick.AddListener(() => ShowCategory("Bed"));
    }

    void ShowCategory(string category)
    {
        mainMenuPanel.SetActive(false);
        categoryPanel.SetActive(true);

        foreach (Transform child in contentPanel)
            Destroy(child.gameObject);

        foreach (var item in allItems)
        {
            if (category == "All" || item.itemCategory == category)
            {
                GameObject newButton = Instantiate(itemButtonPrefab, contentPanel);
                var icon = newButton.transform.Find("Icon").GetComponent<Image>();
                var text = newButton.transform.Find("Name").GetComponent<TMPro.TextMeshProUGUI>();
                var desc = newButton.transform.Find("Description").GetComponent<TMPro.TextMeshProUGUI>();

                icon.sprite = item.icon;
                text.text = item.itemName;
                desc.text = item.description;
            }
        }
    }

    public void BackToMainMenu()
    {
        categoryPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
}
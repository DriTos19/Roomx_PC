using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryControll : InventoryManager
{
    [Header("Sandbox")]
    public WallPlacer_PC wallPlacer;

    [Header("Pagination")]
    public Button nextButton;
    public Button backButton;
    public TMP_Text pageLabel;
    public int itemsPerPage = 9;

    private int currentPage = 0;

    protected override void Start()
    {
        base.Start();

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextPage);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(PreviousPage);
        }

        PopulateSlots();
    }

    public override void PopulateSlots()
    {
        if (slotParent == null || slotPrefab == null) return;

        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        int startIndex = currentPage * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, items.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            GameObject slot = Instantiate(slotPrefab, slotParent);
            var ui = slot.GetComponent<ItemSlotUI>();

            if (ui != null)
                ui.Setup((InventoryItemData)(object)items[i], this);
        }

        UpdatePaginationUI();
    }

    private void UpdatePaginationUI()
    {
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)items.Count / itemsPerPage));

        if (pageLabel != null)
            pageLabel.text = $"Page {currentPage + 1} / {totalPages}";

        if (backButton != null)
            backButton.interactable = currentPage > 0;

        if (nextButton != null)
            nextButton.interactable = currentPage < totalPages - 1;
    }

    public void NextPage()
    {
        currentPage++;
        PopulateSlots();
        HideDescription();
    }

    public void PreviousPage()
    {
        currentPage--;
        PopulateSlots();
        HideDescription();
    }

    public override void ShowItemDetails(InventoryItemData item)
    {
        base.ShowItemDetails(item);

        if (wallPlacer != null)
        {
            CloseInventory();
            wallPlacer.StartPlacement((InventoryItemData)(object)item);
        }
    }

    protected override void OnPurchaseSuccess(InventoryItemData item)
    {
        CloseInventory();

        if (wallPlacer != null)
            wallPlacer.StartPlacement((InventoryItemData)(object)item);
        else
            base.OnPurchaseSuccess(item);
    }
}
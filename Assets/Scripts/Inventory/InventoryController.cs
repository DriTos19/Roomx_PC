using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    private float previousTimeScale = 1f;

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

    public override void SetMenu(bool state)
    {
        base.SetMenu(state);

        if (state)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = previousTimeScale;
            HideDescription();
        }
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
                ui.Setup(items[i], this);
        }

        UpdatePaginationUI();
    }

    public override void PreviewItemFromInventory(InventoryItemData item)
    {
        if (item == null) return;

        CloseInventory();

        if (wallPlacer != null)
            wallPlacer.StartPlacement(item);
    }

    public override void ShowItemDetails(InventoryItemData item)
    {
        base.ShowItemDetails(item);
    }

    public void HideHoveredItemDetails()
    {
        HideDescription();
    }

    private void UpdatePaginationUI()
    {
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)items.Count / itemsPerPage));

        if (currentPage >= totalPages)
            currentPage = totalPages - 1;

        if (currentPage < 0)
            currentPage = 0;

        if (pageLabel != null)
            pageLabel.text = $"Page {currentPage + 1} / {totalPages}";

        if (backButton != null)
            backButton.interactable = currentPage > 0;

        if (nextButton != null)
            nextButton.interactable = currentPage < totalPages - 1;
    }

    public void NextPage()
    {
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)items.Count / itemsPerPage));

        if (currentPage < totalPages - 1)
        {
            currentPage++;
            PopulateSlots();
            HideDescription();
        }
    }

    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            PopulateSlots();
            HideDescription();
        }
    }

    protected override void OnPurchaseSuccess(InventoryItemData item)
    {
        CloseInventory();

        if (wallPlacer != null)
            wallPlacer.StartPlacement(item);
        else
            base.OnPurchaseSuccess(item);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (Time.timeScale == 0f)
            Time.timeScale = 1f;
    }
}
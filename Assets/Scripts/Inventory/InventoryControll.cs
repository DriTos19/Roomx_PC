using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryControll : MonoBehaviour
{
    [Header("Main UI")]
    public CanvasGroup inventoryCanvasGroup;

    [Header("Slots")]
    public Transform slotParent;
    public GameObject slotPrefab;

    [Header("Description")]
    public CanvasGroup descriptionCanvasGroup;
    public Image descriptionImage;
    public TMP_Text descriptionText;

    [Header("Items")]
    public List<InventoryItemData> items = new List<InventoryItemData>();

    [Header("Budget UI")]
    public TMP_Text balanceLabel;

    [Header("Purchase UI")]
    public Button purchaseButton;
    public TMP_Text priceLabel;
    public GameObject insufficientFundsNotice;

    [Header("Sandbox")]
    public WallPlacer_PC wallPlacer;

    [Header("Pagination")]
    public Button nextButton;
    public Button backButton;
    public TMP_Text pageLabel;
    public int itemsPerPage = 9;

    private bool menuOpen = false;
    private Coroutine _noticeRoutine;

    private int currentPage = 0;

    void Start()
    {
        SetMenu(false);

        if (descriptionCanvasGroup != null)
        {
            descriptionCanvasGroup.alpha = 0;
            descriptionCanvasGroup.blocksRaycasts = false;
        }

        if (nextButton != null)
            nextButton.onClick.AddListener(NextPage);

        if (backButton != null)
            backButton.onClick.AddListener(PreviousPage);

        PopulateSlots();

        if (BudgetManager.Instance != null)
            BudgetManager.Instance.onBalanceChanged.AddListener(RefreshBalanceUI);

        if (PurchaseManager.Instance != null)
        {
            PurchaseManager.Instance.onItemSelected.AddListener(RefreshPurchaseUI);
            PurchaseManager.Instance.onPurchaseFailed.AddListener(_ => ShowInsufficientFunds());
            PurchaseManager.Instance.onPurchaseSuccess.AddListener(OnPurchaseSuccess);
        }

        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);

        if (BudgetManager.Instance != null)
            RefreshBalanceUI(BudgetManager.Instance.Balance);

        SetPurchaseButtonInteractable(false);

        if (insufficientFundsNotice != null)
            insufficientFundsNotice.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            menuOpen = !menuOpen;
            SetMenu(menuOpen);
        }

        if (Input.GetKeyDown(KeyCode.Escape) && menuOpen)
        {
            HideMenu();
        }
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;

        if (BudgetManager.Instance != null)
            BudgetManager.Instance.onBalanceChanged.RemoveListener(RefreshBalanceUI);

        if (PurchaseManager.Instance != null)
        {
            PurchaseManager.Instance.onItemSelected.RemoveListener(RefreshPurchaseUI);
            PurchaseManager.Instance.onPurchaseSuccess.RemoveListener(OnPurchaseSuccess);
        }

        if (nextButton != null)
            nextButton.onClick.RemoveListener(NextPage);

        if (backButton != null)
            backButton.onClick.RemoveListener(PreviousPage);
    }

    void OnPurchaseButtonClicked()
    {
        if (PurchaseManager.Instance == null)
            return;

        PurchaseManager.Instance.PurchaseSelected();
    }

    public void PreviewItemFromInventory(InventoryItemData item)
    {
        HideMenu();

        if (wallPlacer == null)
        {
            Debug.LogError("InventoryControll: WallPlacer not assigned.");
            return;
        }

        if (item == null)
        {
            Debug.LogError("InventoryControll: Item is null.");
            return;
        }

        if (item.prefab3D == null)
        {
            Debug.LogError($"InventoryControll: Item '{item.itemName}' has no prefab3D assigned.");
            return;
        }

        wallPlacer.StartPlacement(item);
    }

    void SetMenu(bool state)
    {
        if (inventoryCanvasGroup == null)
            return;

        inventoryCanvasGroup.alpha = state ? 1 : 0;
        inventoryCanvasGroup.interactable = state;
        inventoryCanvasGroup.blocksRaycasts = state;

        Cursor.lockState = state ? CursorLockMode.Confined : CursorLockMode.Locked;
        Cursor.visible = state;

        // Freeze / unfreeze game
        Time.timeScale = state ? 0f : 1f;

        if (!state)
            HideDescription();
    }

    void PopulateSlots()
    {
        if (slotParent == null || slotPrefab == null)
        {
            Debug.LogError("InventoryManager: slotParent or slotPrefab is missing.");
            return;
        }

        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        int startIndex = currentPage * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, items.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            var item = items[i];

            if (item == null)
                continue;

            GameObject slot = Instantiate(slotPrefab, slotParent);

            ItemSlotUI slotUI = slot.GetComponent<ItemSlotUI>();
            if (slotUI == null)
            {
                Debug.LogError("InventoryManager: slotPrefab is missing ItemSlotUI component.");
                continue;
            }

            slotUI.Setup(item, this);
        }

        UpdatePaginationUI();
    }

    void UpdatePaginationUI()
    {
        int totalPages = Mathf.CeilToInt((float)items.Count / itemsPerPage);

        if (totalPages <= 0)
            totalPages = 1;

        Debug.Log($"UpdatePaginationUI -> currentPage={currentPage}, totalPages={totalPages}");

        if (pageLabel != null)
            pageLabel.text = $"Page {currentPage + 1} / {totalPages}";

        if (backButton != null)
            backButton.interactable = true;

        if (nextButton != null)
            nextButton.interactable = currentPage < totalPages - 1;
    }

    public void NextPage()
    {
        int totalPages = Mathf.CeilToInt((float)items.Count / itemsPerPage);
        Debug.Log($"Next clicked -> currentPage={currentPage}, totalPages={totalPages}");

        if (currentPage < totalPages - 1)
        {
            currentPage++;
            PopulateSlots();
            HideDescription();
        }
    }

    public void PreviousPage()
    {
        Debug.Log($"Back clicked -> currentPage={currentPage}");

        if (currentPage > 0)
        {
            currentPage--;
            PopulateSlots();
            HideDescription();
        }
    }

    public void ShowItemDetails(InventoryItemData item)
    {
        if (item == null)
            return;

        if (descriptionImage != null)
            descriptionImage.sprite = item.icon;

        if (descriptionText != null)
            descriptionText.text = $"<b>{item.itemName}</b>\n\n{item.description}";

        if (descriptionCanvasGroup != null)
        {
            descriptionCanvasGroup.alpha = 1;
            descriptionCanvasGroup.blocksRaycasts = true;
        }

        if (PurchaseManager.Instance != null)
            PurchaseManager.Instance.SelectItem(item);
    }

    public void HideDescription()
    {
        if (descriptionCanvasGroup == null)
            return;

        descriptionCanvasGroup.alpha = 0;
        descriptionCanvasGroup.blocksRaycasts = false;
    }

    public void HideMenu()
    {
        menuOpen = false;
        SetMenu(false);
    }

    public void ShowMenu()
    {
        menuOpen = true;
        SetMenu(true);
    }

    private void RefreshBalanceUI(float balance)
    {
        if (balanceLabel != null)
            balanceLabel.text = $"Balance: ${balance:F2}";

        if (PurchaseManager.Instance != null && PurchaseManager.Instance.SelectedItem != null)
            RefreshPurchaseUI(PurchaseManager.Instance.SelectedItem);
    }

    private void RefreshPurchaseUI(InventoryItemData item)
    {
        if (item == null)
        {
            SetPurchaseButtonInteractable(false);

            if (priceLabel != null)
                priceLabel.text = "";

            return;
        }

        if (priceLabel != null)
            priceLabel.text = item.price > 0 ? $"${item.price:F2}" : "Free";

        if (BudgetManager.Instance != null)
            SetPurchaseButtonInteractable(BudgetManager.Instance.CanAfford(item.price));
        else
            SetPurchaseButtonInteractable(false);
    }

    private void SetPurchaseButtonInteractable(bool state)
    {
        if (purchaseButton != null)
            purchaseButton.interactable = state;
    }

    private void ShowInsufficientFunds()
    {
        if (insufficientFundsNotice == null)
            return;

        if (_noticeRoutine != null)
            StopCoroutine(_noticeRoutine);

        _noticeRoutine = StartCoroutine(FlashNotice());
    }

    private IEnumerator FlashNotice()
    {
        insufficientFundsNotice.SetActive(true);
        yield return new WaitForSeconds(2f);
        insufficientFundsNotice.SetActive(false);
    }

    private void OnPurchaseSuccess(InventoryItemData item)
    {
        HideMenu();

        if (wallPlacer == null)
        {
            Debug.LogError("InventoryManager: WallPlacer not assigned.");
            return;
        }

        if (item == null)
        {
            Debug.LogError("InventoryManager: Purchased item is null.");
            return;
        }

        if (item.prefab3D == null)
        {
            Debug.LogError($"InventoryManager: Item '{item.itemName}' has no prefab3D assigned.");
            return;
        }

        wallPlacer.StartPlacement(item);
    }
}
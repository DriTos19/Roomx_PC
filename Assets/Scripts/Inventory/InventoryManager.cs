using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("Main UI")]
    public CanvasGroup inventoryCanvasGroup;

    [Header("Slots")]
    public Transform slotParent;
    public GameObject slotPrefab;

    [Header("Description Panel")]
    public CanvasGroup descriptionCanvasGroup;
    public Image descriptionImage;
    public TMP_Text descriptionText;

    [Header("Budget UI")]
    [Tooltip("Label that shows the player's current balance.")]
    public TMP_Text balanceLabel;

    [Header("Purchase UI")]
    [Tooltip("Button the player clicks to buy the selected item.")]
    public Button purchaseButton;
    [Tooltip("Label on / beside the purchase button showing the item's price.")]
    public TMP_Text priceLabel;
    [Tooltip("Shown briefly when the player cannot afford an item.")]
    public GameObject insufficientFundsNotice;

    [Header("Items")]
    public List<InventoryItemData> items = new List<InventoryItemData>();

    // ── Private state ────────────────────────────────────────────────────────
    private bool menuOpen = false;
    private Coroutine _noticeRoutine;

    // ── Unity lifecycle ──────────────────────────────────────────────────────
    void Start()
    {
        SetMenu(false);
        HideDescription();
        PopulateSlots();

        // Subscribe to budget & purchase events
        BudgetManager.Instance.onBalanceChanged.AddListener(RefreshBalanceUI);
        PurchaseManager.Instance.onItemSelected.AddListener(RefreshPurchaseUI);
        PurchaseManager.Instance.onPurchaseFailed.AddListener(_ => ShowInsufficientFunds());
        PurchaseManager.Instance.onPurchaseSuccess.AddListener(OnPurchaseSuccess);

        // Wire up the purchase button
        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(() => PurchaseManager.Instance.PurchaseSelected());

        // Initial UI state
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
    }

    void OnDestroy()
    {
        // Always clean up UnityEvent listeners to avoid leaks
        if (BudgetManager.Instance != null)
            BudgetManager.Instance.onBalanceChanged.RemoveListener(RefreshBalanceUI);

        if (PurchaseManager.Instance != null)
        {
            PurchaseManager.Instance.onItemSelected.RemoveListener(RefreshPurchaseUI);
            PurchaseManager.Instance.onPurchaseSuccess.RemoveListener(OnPurchaseSuccess);
        }
    }

    // ── Menu control ─────────────────────────────────────────────────────────
    void SetMenu(bool state)
    {
        inventoryCanvasGroup.alpha = state ? 1 : 0;
        inventoryCanvasGroup.interactable = state;
        inventoryCanvasGroup.blocksRaycasts = state;

        if (!state) HideDescription();
    }

    public void HideMenu() { menuOpen = false; SetMenu(false); }
    public void ShowMenu() { menuOpen = true; SetMenu(true); }

    // ── Slot population ──────────────────────────────────────────────────────
    void PopulateSlots()
    {
        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        foreach (var item in items)
        {
            GameObject slot = Instantiate(slotPrefab, slotParent);
            slot.GetComponent<ItemSlotUI>().Setup(item, this);
        }
    }

    // ── Description panel ────────────────────────────────────────────────────
    public void ShowItemDetails(InventoryItemData item)
    {
        descriptionImage.sprite = item.icon;
        descriptionText.text = $"<b>{item.itemName}</b>\n\n{item.description}";

        descriptionCanvasGroup.alpha = 1;
        descriptionCanvasGroup.blocksRaycasts = true;

        // Also update the purchase panel
        PurchaseManager.Instance.SelectItem(item);
    }

    public void HideDescription()
    {
        descriptionCanvasGroup.alpha = 0;
        descriptionCanvasGroup.blocksRaycasts = false;
    }

    // ── Budget UI helpers ────────────────────────────────────────────────────
    private void RefreshBalanceUI(float balance)
    {
        if (balanceLabel != null)
            balanceLabel.text = $"Balance: ${balance:F2}";

        // Re-evaluate affordability whenever balance changes
        if (PurchaseManager.Instance.SelectedItem != null)
            RefreshPurchaseUI(PurchaseManager.Instance.SelectedItem);
    }

    private void RefreshPurchaseUI(InventoryItemData item)
    {
        if (item == null) { SetPurchaseButtonInteractable(false); return; }

        if (priceLabel != null)
            priceLabel.text = item.price > 0 ? $"${item.price:F2}" : "Free";

        bool canAfford = BudgetManager.Instance.CanAfford(item.price);
        SetPurchaseButtonInteractable(canAfford);
    }

    private void SetPurchaseButtonInteractable(bool state)
    {
        if (purchaseButton != null)
            purchaseButton.interactable = state;
    }

    private void ShowInsufficientFunds()
    {
        if (insufficientFundsNotice == null) return;

        if (_noticeRoutine != null) StopCoroutine(_noticeRoutine);
        _noticeRoutine = StartCoroutine(FlashNotice());
    }

    private System.Collections.IEnumerator FlashNotice()
    {
        insufficientFundsNotice.SetActive(true);
        yield return new WaitForSeconds(2f);
        insufficientFundsNotice.SetActive(false);
    }

    private void OnPurchaseSuccess(InventoryItemData item)
    {
        HideMenu();
        PlacementManager.Instance.StartPlacement(item);
    }
}
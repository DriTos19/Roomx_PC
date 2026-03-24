using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Mediates between the UI and BudgetManager.
/// Tracks the currently-selected item and exposes a single Purchase() call.
/// </summary>
public class PurchaseManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static PurchaseManager Instance { get; private set; }

    // ── Events ───────────────────────────────────────────────────────────────
    /// <summary>Fired after a successful purchase. Carries the purchased item.</summary>
    public UnityEvent<InventoryItemData> onPurchaseSuccess = new UnityEvent<InventoryItemData>();

    /// <summary>Fired when a purchase attempt fails (insufficient funds).</summary>
    public UnityEvent<InventoryItemData> onPurchaseFailed = new UnityEvent<InventoryItemData>();

    /// <summary>Fired whenever the selected item changes.</summary>
    public UnityEvent<InventoryItemData> onItemSelected = new UnityEvent<InventoryItemData>();

    // ── State ────────────────────────────────────────────────────────────────
    public InventoryItemData SelectedItem { get; private set; }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the item the player is currently inspecting.
    /// Call this from ItemSlotUI when the player hovers or clicks a slot.
    /// </summary>
    public void SelectItem(InventoryItemData item)
    {
        SelectedItem = item;
        onItemSelected.Invoke(item);
    }

    /// <summary>
    /// Attempts to purchase the currently selected item.
    /// Fires onPurchaseSuccess or onPurchaseFailed accordingly.
    /// </summary>
    public void PurchaseSelected()
    {
        if (SelectedItem == null)
        {
            Debug.LogWarning("[PurchaseManager] PurchaseSelected called with no item selected.");
            return;
        }

        Purchase(SelectedItem);
    }

    /// <summary>
    /// Attempts to purchase a specific item directly.
    /// </summary>
    public void Purchase(InventoryItemData item)
    {
        if (item == null) return;

        if (BudgetManager.Instance.TrySpend(item.price))
        {
            Debug.Log($"[PurchaseManager] Purchased '{item.itemName}' for ${item.price:F2}. " +
                      $"Remaining balance: ${BudgetManager.Instance.Balance:F2}");
            onPurchaseSuccess.Invoke(item);
        }
        else
        {
            Debug.Log($"[PurchaseManager] Cannot afford '{item.itemName}' (${item.price:F2}). " +
                      $"Balance: ${BudgetManager.Instance.Balance:F2}");
            onPurchaseFailed.Invoke(item);
        }
    }

    // ── Unity lifecycle ──────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
}
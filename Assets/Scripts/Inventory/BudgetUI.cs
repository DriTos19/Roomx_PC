using UnityEngine;
using TMPro;

/// <summary>
/// Attach to any always-visible HUD object to display the player's balance.
/// Automatically syncs with BudgetManager via its onBalanceChanged event.
/// </summary>
public class BudgetUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text balanceText;

    [Header("Formatting")]
    [Tooltip("Prefix shown before the amount, e.g. '$' or '€' or 'Gold: '.")]
    public string prefix = "$";

    void Start()
    {
        if (BudgetManager.Instance == null)
        {
            Debug.LogError("[BudgetUI] BudgetManager instance not found! " +
                           "Make sure a BudgetManager exists in the scene.");
            return;
        }

        BudgetManager.Instance.onBalanceChanged.AddListener(UpdateDisplay);
        UpdateDisplay(BudgetManager.Instance.Balance);   // initialise immediately
    }

    void OnDestroy()
    {
        if (BudgetManager.Instance != null)
            BudgetManager.Instance.onBalanceChanged.RemoveListener(UpdateDisplay);
    }

    private void UpdateDisplay(float balance)
    {
        if (balanceText != null)
            balanceText.text = $"{prefix}{balance:F2}";
    }
}
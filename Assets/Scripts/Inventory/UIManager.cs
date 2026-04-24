using UnityEngine;
using TMPro;

namespace Inventory
{
    public class UIManager : MonoBehaviour
    {
        public TMP_Text balanceText;

        private void OnEnable()
        {
            if (BudgetManager.Instance != null)
                BudgetManager.Instance.onBalanceChanged.AddListener(UpdateBalanceUI);
        }

        private void OnDisable()
        {
            if (BudgetManager.Instance != null)
                BudgetManager.Instance.onBalanceChanged.RemoveListener(UpdateBalanceUI);
        }

        private void Start()
        {
            if (BudgetManager.Instance != null)
            {
                BudgetManager.Instance.onBalanceChanged.AddListener(UpdateBalanceUI);
                UpdateBalanceUI(BudgetManager.Instance.Balance);
            }
        }

        private void UpdateBalanceUI(float balance)
        {
            if (balanceText != null)
                balanceText.text = balance.ToString("F0") + "$";
        }
    }
}
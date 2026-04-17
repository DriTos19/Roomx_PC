using UnityEngine;
using UnityEngine.Events;

public class BudgetManager : MonoBehaviour
{
    public static BudgetManager Instance { get; private set; }

    [Header("Starting Balance")]
    [Min(0)] public float startingBalance = 500000f;

    public UnityEvent<float> onBalanceChanged = new UnityEvent<float>();

    private const string SAVE_KEY = "PlayerBudget";
    private const string BUDGET_SET_KEY = "BudgetHasBeenSet";

    private float _balance;

    public float Balance => _balance;

    public bool CanAfford(float cost) => _balance >= cost;

    public bool TrySpend(float cost)
    {
        if (!CanAfford(cost)) return false;
        SetBalance(_balance - cost);
        return true;
    }

    public void AddFunds(float amount)
    {
        if (amount <= 0) return;
        SetBalance(_balance + amount);
    }

    public void SetBalance(float newBalance)
    {
        _balance = Mathf.Max(0, newBalance);
        Save();
        onBalanceChanged.Invoke(_balance);
    }

    // Only sets starting balance once ever (first launch)
    public void InitialiseWithAmount(float amount)
    {
        if (PlayerPrefs.HasKey(BUDGET_SET_KEY)) return;

        SetBalance(amount);
        PlayerPrefs.SetInt(BUDGET_SET_KEY, 1);
        PlayerPrefs.Save();
    }

    public void ResetBudget()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.DeleteKey(BUDGET_SET_KEY);
        SetBalance(startingBalance);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
        // Optional: clear saves every play in editor
        // Comment this out if you want persistence while testing
        PlayerPrefs.DeleteAll();
#endif

        Load();

        // Ensure starting balance is applied only once
        InitialiseWithAmount(startingBalance);
    }

    private void Save()
    {
        PlayerPrefs.SetFloat(SAVE_KEY, _balance);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        _balance = PlayerPrefs.HasKey(SAVE_KEY)
            ? PlayerPrefs.GetFloat(SAVE_KEY)
            : startingBalance;

        onBalanceChanged.Invoke(_balance);
    }
}
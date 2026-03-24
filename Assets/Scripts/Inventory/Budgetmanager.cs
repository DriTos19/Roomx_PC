using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Singleton that owns the player's currency.
/// Persists the balance via PlayerPrefs automatically.
/// </summary>
public class BudgetManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static BudgetManager Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Starting Balance")]
    [Tooltip("Used only on the very first run (no saved data).")]
    [Min(0)] public float startingBalance = 1000f;

    // ── Events ───────────────────────────────────────────────────────────────
    /// <summary>Fired whenever the balance changes. Carries the new balance.</summary>
    public UnityEvent<float> onBalanceChanged = new UnityEvent<float>();

    // ── Private state ────────────────────────────────────────────────────────
    private const string SAVE_KEY = "PlayerBudget";
    private float _balance;

    // ── Public API ───────────────────────────────────────────────────────────
    public float Balance => _balance;

    /// <summary>Returns true when the player can afford <paramref name="cost"/>.</summary>
    public bool CanAfford(float cost) => _balance >= cost;

    /// <summary>
    /// Attempts to spend <paramref name="cost"/>.
    /// Returns <c>true</c> and deducts the amount on success; <c>false</c> otherwise.
    /// </summary>
    public bool TrySpend(float cost)
    {
        if (!CanAfford(cost)) return false;

        SetBalance(_balance - cost);
        return true;
    }

    /// <summary>Adds <paramref name="amount"/> to the balance (e.g. selling items).</summary>
    public void AddFunds(float amount)
    {
        if (amount <= 0) return;
        SetBalance(_balance + amount);
    }

    /// <summary>Hard-sets the balance. Useful for cheat codes / debug.</summary>
    public void SetBalance(float newBalance)
    {
        _balance = Mathf.Max(0, newBalance);
        Save();
        onBalanceChanged.Invoke(_balance);
    }

    /// <summary>Wipes saved data and resets to the starting balance.</summary>
    public void ResetBudget()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        SetBalance(startingBalance);
    }

    // ── Unity lifecycle ──────────────────────────────────────────────────────
    private void Awake()
    {
        // Classic singleton pattern with DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    // ── Persistence ──────────────────────────────────────────────────────────
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

        // Notify listeners so UI initialises correctly
        onBalanceChanged.Invoke(_balance);
    }
}
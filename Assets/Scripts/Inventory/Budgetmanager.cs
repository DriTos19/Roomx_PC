using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class BudgetManager : MonoBehaviour
{
    public static BudgetManager Instance { get; private set; }

    public UnityEvent<float> onBalanceChanged = new UnityEvent<float>();

    private const float DEFAULT_HOUSE_BUDGET = 80000f;
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

    public void ResetBudget()
    {
        PlayerPrefs.DeleteKey(GetSaveKey());
        Load();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        #if UNITY_EDITOR
        PlayerPrefs.DeleteAll();
        #endif

        Load();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Load();
    }
    
    private string GetSaveKey()
    {
        return $"PlayerBudget_{SceneManager.GetActiveScene().name}";
    }

    private void Save()
    {
        PlayerPrefs.SetFloat(GetSaveKey(), _balance);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        string key = GetSaveKey();

        if (PlayerPrefs.HasKey(key))
            _balance = PlayerPrefs.GetFloat(key);
        else
            _balance = DEFAULT_HOUSE_BUDGET;

        onBalanceChanged.Invoke(_balance);
    }
}
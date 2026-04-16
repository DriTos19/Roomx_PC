using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

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

    [Header("Default Assets")]
    public Sprite defaultIcon;

    protected bool menuOpen = false;
    private Coroutine _noticeRoutine;
    private string _glbSavePath;

    [System.Serializable]
    private class GLBPathList { public List<string> paths = new List<string>(); }

    protected virtual void Awake()
    {
        if (Instance == null) Instance = this;
        _glbSavePath = Path.Combine(Application.persistentDataPath, "imported_glbs.json");
    }

    // FIX: global access like your example
    public static bool IsMenuOpen()
    {
        return Instance != null && Instance.menuOpen;
    }

    protected virtual void Start()
    {
        SetMenu(false);
        HideDescription();

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
            purchaseButton.onClick.AddListener(() => PurchaseManager.Instance.PurchaseSelected());

        if (BudgetManager.Instance != null)
            RefreshBalanceUI(BudgetManager.Instance.Balance);

        SetPurchaseButtonInteractable(false);

        if (insufficientFundsNotice != null)
            insufficientFundsNotice.SetActive(false);

        LoadSavedGLBs();
    }

    protected virtual void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            menuOpen = !menuOpen;
            SetMenu(menuOpen);
        }
    }

    protected virtual void OnDestroy()
    {
        if (BudgetManager.Instance != null)
            BudgetManager.Instance.onBalanceChanged.RemoveListener(RefreshBalanceUI);

        if (PurchaseManager.Instance != null)
        {
            PurchaseManager.Instance.onItemSelected.RemoveListener(RefreshPurchaseUI);
            PurchaseManager.Instance.onPurchaseSuccess.RemoveListener(OnPurchaseSuccess);
        }
    }
    
    public virtual void PreviewItemFromInventory(InventoryItemData item)
    {
        if (item == null) return;

        ShowItemDetails(item);
    }

    public virtual void SetMenu(bool state)
    {
        menuOpen = state;

        if (inventoryCanvasGroup == null) return;

        inventoryCanvasGroup.alpha = state ? 1 : 0;
        inventoryCanvasGroup.interactable = state;
        inventoryCanvasGroup.blocksRaycasts = state;

        Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = state;

        if (!state)
            HideDescription();
    }
    
    public async void ImportGLBsFromDirectory(string directoryPath, System.Action<bool, string> onComplete = null)
    {
        if (!Directory.Exists(directoryPath))
        {
            onComplete?.Invoke(false, $"Directory not found: {directoryPath}");
            return;
        }

        string[] files = Directory.GetFiles(directoryPath, "*.glb");

        if (files == null || files.Length == 0)
        {
            onComplete?.Invoke(false, "No .glb files found in directory.");
            return;
        }

        int importedCount = 0;

        foreach (string filePath in files)
        {
            if (!File.Exists(filePath))
                continue;

            GameObject loaded = await GLBLoader.LoadGLB(filePath);
            if (loaded == null)
                continue;

            items.Add(CreateItemFromGLB(loaded, Path.GetFileNameWithoutExtension(filePath)));
            SaveGLBPath(filePath);
            importedCount++;
        }

        PopulateSlots();

        if (importedCount > 0)
            onComplete?.Invoke(true, $"Imported {importedCount} .glb file(s).");
        else
            onComplete?.Invoke(false, "Failed to import any .glb files.");
    }

    // FIX: external close support
    public virtual void CloseInventory()
    {
        SetMenu(false);
        HideDescription();
    }

    public virtual void PopulateSlots()
    {
        if (slotParent == null || slotPrefab == null) return;

        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        foreach (var item in items)
        {
            GameObject slot = Instantiate(slotPrefab, slotParent);
            slot.GetComponent<ItemSlotUI>().Setup(item, this);
        }
    }

    public virtual void ShowItemDetails(InventoryItemData item)
    {
        if (item == null) return;

        if (descriptionImage) descriptionImage.sprite = item.icon;
        if (descriptionText) descriptionText.text = $"<b>{item.itemName}</b>\n\n{item.description}";

        if (descriptionCanvasGroup != null)
        {
            descriptionCanvasGroup.alpha = 1;
            descriptionCanvasGroup.blocksRaycasts = true;
        }

        PurchaseManager.Instance?.SelectItem(item);
    }

    public void HideDescription()
    {
        if (descriptionCanvasGroup == null) return;
        descriptionCanvasGroup.alpha = 0;
        descriptionCanvasGroup.blocksRaycasts = false;
    }

    private void RefreshBalanceUI(float balance)
    {
        if (balanceLabel != null)
            balanceLabel.text = $"Balance: ${balance:F2}";
    }

    private void RefreshPurchaseUI(InventoryItemData item)
    {
        if (item == null)
        {
            SetPurchaseButtonInteractable(false);
            return;
        }

        if (priceLabel != null)
            priceLabel.text = item.price > 0 ? $"${item.price:F2}" : "Free";

        if (BudgetManager.Instance != null)
            SetPurchaseButtonInteractable(BudgetManager.Instance.CanAfford(item.price));
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

    private IEnumerator FlashNotice()
    {
        insufficientFundsNotice.SetActive(true);
        yield return new WaitForSeconds(2f);
        insufficientFundsNotice.SetActive(false);
    }

    protected virtual void OnPurchaseSuccess(InventoryItemData item)
    {
        CloseInventory();

        if (PlacementManager.Instance != null)
            PlacementManager.Instance.StartPlacement(item);
    }

    // ===== GLB SYSTEM (unchanged) =====

    public async void ImportGLBFromPath(string filePath, System.Action<bool, string> onComplete = null)
    {
        if (!File.Exists(filePath))
        {
            onComplete?.Invoke(false, $"File not found: {filePath}");
            return;
        }

        GameObject loaded = await GLBLoader.LoadGLB(filePath);
        if (loaded == null)
        {
            onComplete?.Invoke(false, "Failed to load.");
            return;
        }

        items.Add(CreateItemFromGLB(loaded, Path.GetFileNameWithoutExtension(filePath)));
        SaveGLBPath(filePath);
        PopulateSlots();
        onComplete?.Invoke(true, "Imported.");
    }

    private InventoryItemData CreateItemFromGLB(GameObject prefab, string itemName)
    {
        InventoryItemData item = ScriptableObject.CreateInstance<InventoryItemData>();
        item.itemName = itemName;
        item.description = $"Imported model: {itemName}";
        item.prefab3D = prefab;
        item.icon = defaultIcon;
        item.price = 0f;
        return item;
    }

    private void SaveGLBPath(string filePath)
    {
        GLBPathList list = LoadGLBPathList();
        if (!list.paths.Contains(filePath))
        {
            list.paths.Add(filePath);
            File.WriteAllText(_glbSavePath, JsonUtility.ToJson(list, true));
        }
    }

    private GLBPathList LoadGLBPathList()
    {
        if (!File.Exists(_glbSavePath)) return new GLBPathList();
        return JsonUtility.FromJson<GLBPathList>(File.ReadAllText(_glbSavePath)) ?? new GLBPathList();
    }

    private async void LoadSavedGLBs()
    {
        GLBPathList list = LoadGLBPathList();

        foreach (string path in list.paths)
        {
            if (!File.Exists(path)) continue;

            GameObject loaded = await GLBLoader.LoadGLB(path);
            if (loaded != null)
                items.Add(CreateItemFromGLB(loaded, Path.GetFileNameWithoutExtension(path)));
        }

        PopulateSlots();
    }
}
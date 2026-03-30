using System.Collections;
using System.Collections.Generic;
using System.IO;
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

    private bool menuOpen = false;
    private Coroutine _noticeRoutine;
    private string _glbSavePath;

    [System.Serializable]
    private class GLBPathList { public List<string> paths = new List<string>(); }

    void Awake()
    {
        _glbSavePath = Path.Combine(Application.persistentDataPath, "imported_glbs.json");
    }

    void Start()
    {
        SetMenu(false);

        if (descriptionCanvasGroup != null)
        {
            descriptionCanvasGroup.alpha = 0;
            descriptionCanvasGroup.blocksRaycasts = false;
        }

        PopulateSlots();

        BudgetManager.Instance.onBalanceChanged.AddListener(RefreshBalanceUI);
        PurchaseManager.Instance.onItemSelected.AddListener(RefreshPurchaseUI);
        PurchaseManager.Instance.onPurchaseFailed.AddListener(_ => ShowInsufficientFunds());
        PurchaseManager.Instance.onPurchaseSuccess.AddListener(OnPurchaseSuccess);

        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(() => PurchaseManager.Instance.PurchaseSelected());

        RefreshBalanceUI(BudgetManager.Instance.Balance);
        SetPurchaseButtonInteractable(false);

        if (insufficientFundsNotice != null)
            insufficientFundsNotice.SetActive(false);

        LoadSavedGLBs();
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
        if (BudgetManager.Instance != null)
            BudgetManager.Instance.onBalanceChanged.RemoveListener(RefreshBalanceUI);

        if (PurchaseManager.Instance != null)
        {
            PurchaseManager.Instance.onItemSelected.RemoveListener(RefreshPurchaseUI);
            PurchaseManager.Instance.onPurchaseSuccess.RemoveListener(OnPurchaseSuccess);
        }
    }

    void SetMenu(bool state)
    {
        inventoryCanvasGroup.alpha = state ? 1 : 0;
        inventoryCanvasGroup.interactable = state;
        inventoryCanvasGroup.blocksRaycasts = state;

        // Unlock cursor when menu is open, lock it when closed
        Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = state;

        if (!state)
            HideDescription();
    }

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

    public void ShowItemDetails(InventoryItemData item)
    {
        descriptionImage.sprite = item.icon;
        descriptionText.text = $"<b>{item.itemName}</b>\n\n{item.description}";

        descriptionCanvasGroup.alpha = 1;
        descriptionCanvasGroup.blocksRaycasts = true;

        PurchaseManager.Instance.SelectItem(item);
    }

    public void HideDescription()
    {
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

        if (PurchaseManager.Instance.SelectedItem != null)
            RefreshPurchaseUI(PurchaseManager.Instance.SelectedItem);
    }

    private void RefreshPurchaseUI(InventoryItemData item)
    {
        if (item == null) { SetPurchaseButtonInteractable(false); return; }

        if (priceLabel != null)
            priceLabel.text = item.price > 0 ? $"${item.price:F2}" : "Free";

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

    private void OnPurchaseSuccess(InventoryItemData item)
    {
        HideMenu();
        PlacementManager.Instance.StartPlacement(item);
    }

    public async void ImportGLBFromPath(string filePath, System.Action<bool, string> onComplete = null)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            onComplete?.Invoke(false, "File path is empty.");
            return;
        }

        if (!File.Exists(filePath))
        {
            onComplete?.Invoke(false, $"File not found: {filePath}");
            return;
        }

        GameObject loaded = await GLBLoader.LoadGLB(filePath);
        if (loaded == null)
        {
            onComplete?.Invoke(false, $"Failed to load: {Path.GetFileName(filePath)}");
            return;
        }

        items.Add(CreateItemFromGLB(loaded, Path.GetFileNameWithoutExtension(filePath)));
        SaveGLBPath(filePath);
        PopulateSlots();
        onComplete?.Invoke(true, $"Imported: {Path.GetFileNameWithoutExtension(filePath)}");
    }

    public async void ImportGLBsFromDirectory(string directoryPath, System.Action<bool, string> onComplete = null)
    {
        if (string.IsNullOrEmpty(directoryPath))
        {
            onComplete?.Invoke(false, "Directory path is empty.");
            return;
        }

        if (!Directory.Exists(directoryPath))
        {
            onComplete?.Invoke(false, $"Directory not found: {directoryPath}");
            return;
        }

        string[] glbFiles = Directory.GetFiles(directoryPath, "*.glb", SearchOption.AllDirectories);
        if (glbFiles.Length == 0)
        {
            onComplete?.Invoke(false, "No .glb files found in directory.");
            return;
        }

        int imported = 0;
        foreach (string path in glbFiles)
        {
            GameObject loaded = await GLBLoader.LoadGLB(path);
            if (loaded != null)
            {
                items.Add(CreateItemFromGLB(loaded, Path.GetFileNameWithoutExtension(path)));
                SaveGLBPath(path);
                imported++;
            }
        }

        if (imported > 0)
        {
            PopulateSlots();
            onComplete?.Invoke(true, $"Imported {imported} of {glbFiles.Length} file(s).");
        }
        else
        {
            onComplete?.Invoke(false, "No files could be imported.");
        }
    }

    private InventoryItemData CreateItemFromGLB(GameObject prefab, string itemName)
    {
        InventoryItemData item = ScriptableObject.CreateInstance<InventoryItemData>();
        item.itemName = itemName;
        item.description = $"Imported model: {itemName}";
        item.prefab3D = prefab;
        item.icon = defaultIcon;
        item.category = ItemCategory.All;
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
        try { return JsonUtility.FromJson<GLBPathList>(File.ReadAllText(_glbSavePath)) ?? new GLBPathList(); }
        catch { return new GLBPathList(); }
    }

    private async void LoadSavedGLBs()
    {
        GLBPathList list = LoadGLBPathList();
        if (list.paths.Count == 0) return;

        foreach (string path in list.paths)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Previously imported GLB no longer found: {path}");
                continue;
            }
            GameObject loaded = await GLBLoader.LoadGLB(path);
            if (loaded != null)
                items.Add(CreateItemFromGLB(loaded, Path.GetFileNameWithoutExtension(path)));
        }

        if (items.Count > 0)
            PopulateSlots();
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

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
        Cursor.lockState = state ? CursorLockMode.Confined : CursorLockMode.Locked;

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

    /// <summary>
    /// Imports a single FBX file and creates a prefab from it.
    /// </summary>
    public void ImportFBXFromPath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("File path is empty!");
            return;
        }

        if (!File.Exists(filePath))
        {
            Debug.LogError($"FBX file does not exist: {filePath}");
            return;
        }

        try
        {
            // Load the FBX file
            GameObject loadedObject = FBXLoader.LoadFBX(filePath);
            if (loadedObject == null)
            {
                Debug.LogError($"Failed to load FBX file: {filePath}");
                return;
            }

            // Create an InventoryItemData from the loaded FBX
            InventoryItemData newItem = CreateInventoryItemFromPrefab(loadedObject, Path.GetFileNameWithoutExtension(filePath));
            if (newItem != null)
            {
                items.Add(newItem);
                PopulateSlots();
                Debug.Log($"Successfully imported FBX: {filePath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception while importing FBX: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// Imports all FBX files from a directory.
    /// </summary>
    public void ImportFBXsFromDirectory(string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath))
        {
            Debug.LogError("Directory path is empty!");
            return;
        }

        if (!Directory.Exists(directoryPath))
        {
            Debug.LogError($"Directory does not exist: {directoryPath}");
            return;
        }

        try
        {
            string[] fbxFiles = Directory.GetFiles(directoryPath, "*.fbx", SearchOption.AllDirectories);
            if (fbxFiles.Length == 0)
            {
                Debug.LogWarning($"No FBX files found in directory: {directoryPath}");
                return;
            }

            int importedCount = 0;
            foreach (string fbxPath in fbxFiles)
            {
                try
                {
                    GameObject loadedObject = FBXLoader.LoadFBX(fbxPath);
                    if (loadedObject != null)
                    {
                        InventoryItemData newItem = CreateInventoryItemFromPrefab(loadedObject, Path.GetFileNameWithoutExtension(fbxPath));
                        if (newItem != null)
                        {
                            items.Add(newItem);
                            importedCount++;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to import FBX {fbxPath}: {e.Message}");
                }
            }

            if (importedCount > 0)
            {
                PopulateSlots();
                Debug.Log($"Successfully imported {importedCount} FBX files from: {directoryPath}");
            }
            else
            {
                Debug.LogWarning($"No FBX files were successfully imported from: {directoryPath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception while importing FBX directory: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// Creates an InventoryItemData from a loaded FBX prefab.
    /// </summary>
    private InventoryItemData CreateInventoryItemFromPrefab(GameObject prefab, string itemName)
    {
        // Create a new InventoryItemData
        InventoryItemData newItem = ScriptableObject.CreateInstance<InventoryItemData>();
        newItem.itemName = itemName;
        newItem.description = $"Imported FBX model: {itemName}";
        newItem.prefab3D = prefab;
        newItem.icon = defaultIcon; // Will be null if not assigned, handled by ItemSlotUI
        newItem.category = ItemCategory.All;
        newItem.price = 0f; // Free by default

        // Optionally, save the asset to disk if you want persistence
        // String assetPath = Path.Combine("Assets/Resources", $"{itemName}.asset");
        // AssetDatabase.CreateAsset(newItem, assetPath);
        // AssetDatabase.SaveAssets();

        return newItem;
    }
}
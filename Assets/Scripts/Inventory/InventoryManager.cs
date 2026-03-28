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

    [Header("Description")]
    public CanvasGroup descriptionCanvasGroup;
    public Image descriptionImage;
    public TMP_Text descriptionText;

    [Header("Items")]
    public List<InventoryItemData> items = new List<InventoryItemData>();
    public Sprite defaultIcon;

    private bool menuOpen = false;

    void Start()
    {
        SetMenu(false);

        if (descriptionCanvasGroup != null)
        {
            descriptionCanvasGroup.alpha = 0;
            descriptionCanvasGroup.blocksRaycasts = false;
        }

        PopulateSlots();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            menuOpen = !menuOpen;
            SetMenu(menuOpen);
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
        descriptionImage.sprite = item.icon ?? defaultIcon;
        descriptionText.text = $"<b>{item.itemName}</b>\n\n{item.description}";

        descriptionCanvasGroup.alpha = 1;
        descriptionCanvasGroup.blocksRaycasts = true;
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

    public void ImportFBXFromPath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("FBX file path is empty!");
            return;
        }

        if (!System.IO.File.Exists(filePath))
        {
            Debug.LogError("FBX file does not exist: " + filePath);
            return;
        }

        if (!filePath.ToLower().EndsWith(".fbx"))
        {
            Debug.LogError("File is not an FBX file: " + filePath);
            return;
        }

        GameObject loadedModel = FBXLoader.LoadFBX(filePath);

        if (loadedModel != null)
        {
            // Create a new InventoryItemData for the custom model
            InventoryItemData customItem = ScriptableObject.CreateInstance<InventoryItemData>();
            customItem.itemName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            customItem.description = "Custom imported 3D model from: " + System.IO.Path.GetFileName(filePath);
            customItem.prefab3D = loadedModel;
            customItem.category = ItemCategory.All;
            customItem.icon = defaultIcon;

            items.Add(customItem);
            PopulateSlots();

            Debug.Log("Successfully imported FBX model: " + customItem.itemName);
        }
        else
        {
            Debug.LogError("Failed to load FBX model from path: " + filePath);
        }
    }

    /// <summary>
    /// Loads all FBX files from a specific directory.
    /// </summary>
    public void ImportFBXsFromDirectory(string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath) || !System.IO.Directory.Exists(directoryPath))
        {
            Debug.LogError("Directory does not exist: " + directoryPath);
            return;
        }

        string[] fbxFiles = System.IO.Directory.GetFiles(directoryPath, "*.fbx");

        if (fbxFiles.Length == 0)
        {
            Debug.LogWarning("No FBX files found in directory: " + directoryPath);
            return;
        }

        Debug.Log("Found " + fbxFiles.Length + " FBX files. Starting import...");

        foreach (string fbxFile in fbxFiles)
        {
            ImportFBXFromPath(fbxFile);
        }

        Debug.Log("Finished importing FBX files from directory.");
    }
}
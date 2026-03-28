using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI system for importing FBX files. Provides input fields and buttons for file path entry.
/// </summary>
public class FBXImportUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField filePathInputField;
    public Button importButton;
    public Button importFromDirectoryButton;
    public TextMeshProUGUI statusText;

    [Header("Manager Reference")]
    public InventoryManager inventoryManager;

    void Start()
    {
        if (importButton != null)
            importButton.onClick.AddListener(OnImportButtonClicked);

        if (importFromDirectoryButton != null)
            importFromDirectoryButton.onClick.AddListener(OnImportDirectoryButtonClicked);
    }

    void OnImportButtonClicked()
    {
        if (filePathInputField == null || string.IsNullOrEmpty(filePathInputField.text))
        {
            UpdateStatus("Please enter a file path!", Color.red);
            return;
        }

        string filePath = filePathInputField.text;
        inventoryManager.ImportFBXFromPath(filePath);
        UpdateStatus("Import attempt completed. Check console for details.", Color.yellow);
    }

    void OnImportDirectoryButtonClicked()
    {
        if (filePathInputField == null || string.IsNullOrEmpty(filePathInputField.text))
        {
            UpdateStatus("Please enter a directory path!", Color.red);
            return;
        }

        string directoryPath = filePathInputField.text;
        inventoryManager.ImportFBXsFromDirectory(directoryPath);
        UpdateStatus("Directory import attempt completed. Check console for details.", Color.yellow);
    }

    void UpdateStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
        Debug.Log("[FBXImportUI] " + message);
    }
}


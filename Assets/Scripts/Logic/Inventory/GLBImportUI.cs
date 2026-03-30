using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GLBImportUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField pathInputField;
    public Button importFolderButton;
    public TextMeshProUGUI statusText;

    [Header("Dependencies")]
    public InventoryManager inventoryManager;

    void Start()
    {
        importFolderButton.onClick.AddListener(OnImportFolder);
        SetStatus("Enter a folder path containing .glb files.", Color.white);
    }

    void Update()
    {
        // XR Input System often blocks TMP InputField keyboard events.
        // Force-activate the field whenever the user clicks over any UI element.
        if (Input.GetMouseButtonDown(0) && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            pathInputField.Select();
            pathInputField.ActivateInputField();
        }
    }

    void OnImportFolder()
    {
        string path = pathInputField.text.Trim();
        if (string.IsNullOrEmpty(path))
        {
            SetStatus("Path cannot be empty.", Color.red);
            return;
        }
        SetStatus("Importing folder...", Color.yellow);
        inventoryManager.ImportGLBsFromDirectory(path, OnImportResult);
    }

    void OnImportResult(bool success, string message)
    {
        SetStatus(message, success ? Color.green : Color.red);
    }

    void SetStatus(string message, Color color)
    {
        if (statusText == null) return;
        statusText.text = message;
        statusText.color = color;
    }
}

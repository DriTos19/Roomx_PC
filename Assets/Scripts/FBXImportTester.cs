using UnityEngine;
using TMPro;

/// <summary>
/// Test Helper for FBX Import System
/// Add this to a GameObject and call methods to test FBX import functionality
/// </summary>
public class FBXImportTester : MonoBehaviour
{
    public InventoryManager inventoryManager;
    public TMP_InputField importPathInput;

    private bool showingInputPrompt = false;
    private string testImportPath = "";

    void Update()
    {
        // Press I to import (opens input or uses last path)
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (importPathInput != null && !showingInputPrompt)
            {
                showingInputPrompt = true;
                importPathInput.gameObject.SetActive(true);
                importPathInput.ActivateInputField();
                Debug.Log("Enter FBX file path and press Enter to import");
            }
            else if (importPathInput == null)
            {
                // No input field - use console input
                Debug.Log("Press I again to import test path, or add TMP_InputField to FBXImportTester");
                Debug.Log("Test paths:");
                Debug.Log("  Single: C:/Models/model.fbx");
                Debug.Log("  Directory: C:/Models/");
            }
        }

        // Press F to import single file (quick test)
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("Testing single FBX import...");
            // Modify path to test
            string testPath = "C:/Models/test_model.fbx";
            if (System.IO.File.Exists(testPath))
            {
                inventoryManager.ImportFBXFromPath(testPath);
            }
            else
            {
                Debug.LogWarning("Test file not found: " + testPath);
                Debug.Log("Create file or modify path in FBXImportTester.cs line 31");
            }
        }

        // Press D to import directory
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("Testing directory FBX import...");
            // Modify path to test
            string testDir = "C:/Models/";
            if (System.IO.Directory.Exists(testDir))
            {
                inventoryManager.ImportFBXsFromDirectory(testDir);
            }
            else
            {
                Debug.LogWarning("Test directory not found: " + testDir);
                Debug.Log("Create directory or modify path in FBXImportTester.cs line 51");
            }
        }

        // Press P to list item count
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Inventory contains " + inventoryManager.items.Count + " items");
            foreach (InventoryItemData item in inventoryManager.items)
            {
                Debug.Log("  - " + item.itemName);
            }
        }

        // Handle input field submission
        if (importPathInput != null && importPathInput.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Return))
        {
            string path = importPathInput.text;
            if (!string.IsNullOrEmpty(path))
            {
                if (System.IO.File.Exists(path))
                {
                    Debug.Log("Importing single file: " + path);
                    inventoryManager.ImportFBXFromPath(path);
                }
                else if (System.IO.Directory.Exists(path))
                {
                    Debug.Log("Importing directory: " + path);
                    inventoryManager.ImportFBXsFromDirectory(path);
                }
                else
                {
                    Debug.LogError("Path not found: " + path);
                }
            }
            importPathInput.gameObject.SetActive(false);
            showingInputPrompt = false;
        }

        // Press Escape to close input
        if (Input.GetKeyDown(KeyCode.Escape) && showingInputPrompt)
        {
            importPathInput.gameObject.SetActive(false);
            showingInputPrompt = false;
            Debug.Log("Import cancelled");
        }
    }
}



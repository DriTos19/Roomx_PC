using UnityEngine;

public class FBXImportTest : MonoBehaviour
{
    public InventoryManager inventoryManager;

    void Start()
    {
        if (inventoryManager == null)
        {
            Debug.LogError("InventoryManager not assigned!");
            return;
        }

        // Test importing an existing FBX file
        string testPath = Application.dataPath + "/Resources/Prefabs/Furniture/baked_chair.fbx";

        if (System.IO.File.Exists(testPath))
        {
            Debug.Log("Testing FBX import with: " + testPath);
            inventoryManager.ImportFBXFromPath(testPath);
        }
        else
        {
            Debug.LogWarning("Test file not found: " + testPath);
            // List available FBX files
            string furnitureDir = Application.dataPath + "/Resources/Prefabs/Furniture/";
            if (System.IO.Directory.Exists(furnitureDir))
            {
                string[] fbxFiles = System.IO.Directory.GetFiles(furnitureDir, "*.fbx");
                Debug.Log("Available FBX files in Furniture directory:");
                foreach (string file in fbxFiles)
                {
                    Debug.Log("  - " + file);
                }

                if (fbxFiles.Length > 0)
                {
                    Debug.Log("Testing with first available file: " + fbxFiles[0]);
                    inventoryManager.ImportFBXFromPath(fbxFiles[0]);
                }
            }
            else
            {
                Debug.LogError("Furniture directory not found: " + furnitureDir);
            }
        }
    }

    void Update()
    {
        // Press T to test import
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("Manual test triggered");
            string testPath = Application.dataPath + "/Resources/Prefabs/Furniture/baked_chair.fbx";
            if (System.IO.File.Exists(testPath))
            {
                inventoryManager.ImportFBXFromPath(testPath);
            }
            else
            {
                Debug.LogWarning("Test file not found: " + testPath);
            }
        }

        // Press L to list inventory items
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("Current inventory items:");
            foreach (var item in inventoryManager.items)
            {
                Debug.Log("  - " + item.itemName + " (prefab: " + (item.prefab3D != null ? "yes" : "no") + ")");
            }
        }
    }
}

# 🚀 FBX Import System - Quick Command Reference

## Import Methods

### Single File Import
```csharp
inventoryManager.ImportFBXFromPath("C:/path/to/model.fbx");
```

### Directory Batch Import
```csharp
inventoryManager.ImportFBXsFromDirectory("C:/path/to/models/");
```

## Usage Patterns

### Pattern 1: Direct Code Call
```csharp
InventoryManager inv = FindObjectOfType<InventoryManager>();
inv.ImportFBXFromPath("C:/Models/chair.fbx");
```

### Pattern 2: From Button Click
```csharp
// In a button's OnClick event
public void OnImportButtonClicked()
{
    inventoryManager.ImportFBXFromPath("C:/Models/table.fbx");
}
```

### Pattern 3: From Menu
```csharp
public class GameMenu : MonoBehaviour
{
    public InventoryManager inventory;
    
    public void ImportFurniture()
    {
        inventory.ImportFBXFromPath("C:/Furniture/sofa.fbx");
    }
}
```

### Pattern 4: With Error Checking
```csharp
string path = "C:/Models/model.fbx";
if (System.IO.File.Exists(path))
{
    inventoryManager.ImportFBXFromPath(path);
}
else
{
    Debug.LogError("File not found: " + path);
}
```

## File Path Examples

### Windows Absolute Paths
```
C:/Users/Username/Desktop/model.fbx
C:/Program Files/Models/chair.fbx
C:\Users\Username\Documents\Models\table.fbx
```

### Project Relative Paths
```
Assets/Models/furniture.fbx
../ExternalModels/model.fbx
./Models/chair.fbx
```

### Network Paths
```
//server/share/models/chair.fbx
\\server\share\models\table.fbx
```

## Keyboard Shortcuts (with FBXImportTester)

| Key | Action |
|-----|--------|
| F | Import single file (test path) |
| D | Import directory (test path) |
| P | Print item count to console |

## UI Integration Code

### Button OnClick Reference
```csharp
// In FBXImportUI script
// Assign to Import button:
public void OnImportButtonClicked()
{
    if (filePathInputField != null && inventoryManager != null)
    {
        inventoryManager.ImportFBXFromPath(filePathInputField.text);
    }
}
```

### Input Field Setup
```csharp
// Create InputField and assign to FBXImportUI.filePathInputField
// Leave other fields blank for optional setup
```

## Common Tasks

### Import Single Chair
```csharp
inventoryManager.ImportFBXFromPath("C:/Models/Furniture/chair.fbx");
```

### Import All Furniture
```csharp
inventoryManager.ImportFBXsFromDirectory("C:/Models/Furniture/");
```

### Import and Get Count
```csharp
inventoryManager.ImportFBXFromPath("model.fbx");
int totalItems = inventoryManager.items.Count;
Debug.Log("Now have " + totalItems + " items");
```

### Import From Specific Folder
```csharp
string folder = System.IO.Path.Combine(
    System.Environment.GetFolderPath(
        System.Environment.SpecialFolder.Desktop
    ), 
    "Models"
);
inventoryManager.ImportFBXsFromDirectory(folder);
```

## Error Handling

### Check if File Exists
```csharp
string path = "C:/model.fbx";
if (!System.IO.File.Exists(path))
{
    Debug.LogError("File not found: " + path);
    return;
}
inventoryManager.ImportFBXFromPath(path);
```

### Check if Directory Exists
```csharp
string dir = "C:/Models/";
if (!System.IO.Directory.Exists(dir))
{
    Debug.LogError("Directory not found: " + dir);
    return;
}
inventoryManager.ImportFBXsFromDirectory(dir);
```

### Check File Extension
```csharp
string path = "C:/model.fbx";
if (!path.ToLower().EndsWith(".fbx"))
{
    Debug.LogError("Not an FBX file: " + path);
    return;
}
```

## Advanced Usage

### Get Imported Item
```csharp
inventoryManager.ImportFBXFromPath("model.fbx");
InventoryItemData lastItem = inventoryManager.items[
    inventoryManager.items.Count - 1
];
Debug.Log("Imported: " + lastItem.itemName);
```

### Loop Through Imported Items
```csharp
foreach (InventoryItemData item in inventoryManager.items)
{
    if (item.description.Contains("Custom imported"))
    {
        Debug.Log("Found custom item: " + item.itemName);
    }
}
```

### Import and Place
```csharp
inventoryManager.ImportFBXFromPath("model.fbx");
InventoryItemData item = inventoryManager.items[
    inventoryManager.items.Count - 1
];
PlacementManager.Instance.StartPlacement(item);
```

### Batch Import with Status
```csharp
string[] fbxFiles = System.IO.Directory.GetFiles(
    "C:/Models/", 
    "*.fbx"
);
foreach (string file in fbxFiles)
{
    inventoryManager.ImportFBXFromPath(file);
    Debug.Log("Imported: " + System.IO.Path.GetFileName(file));
}
Debug.Log("Import complete. Total items: " + 
    inventoryManager.items.Count);
```

## Debugging

### Enable Detailed Logging
```csharp
// Open Console window in Unity (Window > General > Console)
// All import events will log:
// - Success messages
// - Error messages
// - File paths processed
// - Item counts
```

### Check Console Output
```
Successfully imported FBX model: model_name
File does not exist: /path/to/file.fbx
Found 5 FBX files. Starting import...
```

## Performance Notes

```csharp
// Fast: < 5 MB files
inventoryManager.ImportFBXFromPath("small.fbx"); // ~instant

// Medium: 5-20 MB files
inventoryManager.ImportFBXFromPath("medium.fbx"); // 1-5 sec

// Slow: 20-50 MB files
inventoryManager.ImportFBXFromPath("large.fbx"); // 5-10 sec

// Very Slow: > 50 MB files
inventoryManager.ImportFBXsFromDirectory("huge/"); // 10+ sec
```

## Testing Commands

### Quick Test
```csharp
// In OnGUI() or Update()
if (Input.GetKeyDown(KeyCode.I))
{
    FindObjectOfType<InventoryManager>()
        .ImportFBXFromPath("C:/test.fbx");
}
```

### Batch Test
```csharp
if (Input.GetKeyDown(KeyCode.B))
{
    FindObjectOfType<InventoryManager>()
        .ImportFBXsFromDirectory("C:/test_models/");
}
```

### Print Stats
```csharp
if (Input.GetKeyDown(KeyCode.P))
{
    int count = FindObjectOfType<InventoryManager>()
        .items.Count;
    Debug.Log("Inventory items: " + count);
}
```

## Common Errors & Fixes

| Error | Fix |
|-------|-----|
| `File does not exist` | Check path spelling and forward slashes |
| `File is not an FBX file` | Ensure .fbx extension |
| `NullReferenceException` | Verify InventoryManager is assigned |
| `Directory not found` | Check directory path exists |
| Model appears black | FBX may not have proper normals |
| Model won't place | Ensure PlacementManager exists |

## Next Steps

1. Copy the import method you need
2. Assign file path
3. Call the method
4. Check console for results
5. Verify model appears in inventory
6. Select and place in scene

## Support

For detailed information, see:
- `FBX_IMPORT_README.md` - Technical details
- `FBX_IMPORT_SETUP.md` - Setup instructions
- `IMPLEMENTATION_SUMMARY.md` - System overview

---

**Last Updated:** March 26, 2026  
**Version:** 1.0


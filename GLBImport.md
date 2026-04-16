# GLB Import Documentation

## Overview

The GLB import system allows users to load custom 3D models at runtime from their local file system and place them in the room like any other catalog item. The system is built on the **GLTFast** package and is split across three layers: loading, inventory integration, and UI.

---

## Architecture

```
User enters path
        │
        ▼
GLBImportUI          → validates input, shows status feedback
        │
        ▼
InventoryManager     → orchestrates import, saves paths, updates inventory
        │
        ▼
GLBLoader            → loads the GLB file via GLTFast, normalizes the model
        │
        ▼
InventoryItemData    → wraps the loaded prefab as a catalog item
        │
        ▼
PlacementManager     → lets the user place the item in the scene
```

---

## 1. Dependencies

The system relies on **GLTFast** for all GLB/GLTF parsing and scene instantiation.

| Package | Version |
|---|---|
| `com.unity.cloud.gltfast` | 6.10.0 |

The two key calls from GLTFast used in the pipeline are:

```csharp
var gltf = new GltfImport();
await gltf.Load(filePath);
await gltf.InstantiateMainSceneAsync(root.transform);
```

---

## 2. GLBLoader

**File:** `Assets/Scripts/Inventory/GLBLoader.cs`  
**Responsibility:** Loads a single GLB file from disk, instantiates its scene hierarchy, normalizes its transform, and returns it as an inactive `GameObject` ready to be used as a prefab.

### LoadGLB

```csharp
public static async Task<GameObject> LoadGLB(string filePath)
{
    var root = new GameObject(Path.GetFileNameWithoutExtension(filePath));
    var gltf = new GltfImport();

    bool success = await gltf.Load(filePath);
    if (!success)
    {
        Object.Destroy(root);
        return null;
    }

    bool instantiated = await gltf.InstantiateMainSceneAsync(root.transform);
    if (!instantiated)
    {
        Object.Destroy(root);
        return null;
    }

    NormalizeModel(root);
    root.SetActive(false); // kept inactive — used only as an Instantiate source
    return root;
}
```

### NormalizeModel

After loading, every model is centered and uniformly scaled so it fits within a 1-unit cube. This makes placement predictable regardless of the original file's units or origin.

```csharp
static void NormalizeModel(GameObject model)
{
    var renderers = model.GetComponentsInChildren<Renderer>();
    if (renderers.Length == 0) return;

    Bounds bounds = renderers[0].bounds;
    foreach (var r in renderers)
        bounds.Encapsulate(r.bounds);

    // Center around origin
    Vector3 offset = bounds.center - model.transform.position;
    foreach (Transform child in model.transform)
        child.position -= offset;

    // Scale to fit within 1 unit
    float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
    if (maxDim > 0f)
        model.transform.localScale = Vector3.one * (1f / maxDim);
}
```

---

## 3. InventoryManager — Import Orchestration

**File:** `Assets/Scripts/Inventory/InventoryManager.cs`  
**Responsibility:** Coordinates the full import pipeline — validates input, calls `GLBLoader`, wraps the result as an `InventoryItemData`, and persists the file path for future sessions.

### ImportGLBFromPath — Single File

```csharp
public async void ImportGLBFromPath(string filePath, Action<bool, string> onComplete = null)
{
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
```

### ImportGLBsFromDirectory — Batch Import

Recursively searches a folder for `.glb` files and imports each one in sequence.

```csharp
public async void ImportGLBsFromDirectory(string directoryPath, Action<bool, string> onComplete = null)
{
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

    PopulateSlots();
    onComplete?.Invoke(imported > 0, $"Imported {imported} of {glbFiles.Length} file(s).");
}
```

### CreateItemFromGLB

Wraps the loaded `GameObject` as a runtime `InventoryItemData` ScriptableObject so it integrates with the existing inventory and placement systems without modification.

```csharp
private InventoryItemData CreateItemFromGLB(GameObject prefab, string itemName)
{
    InventoryItemData item = ScriptableObject.CreateInstance<InventoryItemData>();
    item.itemName    = itemName;
    item.description = $"Imported model: {itemName}";
    item.prefab3D    = prefab;
    item.icon        = defaultIcon;
    item.category    = ItemCategory.All;
    item.price       = 0f;
    return item;
}
```

---

## 4. GLBImportUI

**File:** `Assets/Scripts/Inventory/GLBImportUI.cs`  
**Responsibility:** Provides the user-facing interface — a text input field for the folder path and a button that triggers the import. Status messages give real-time feedback.

```csharp
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
```

Status colors:

| Color | Meaning |
|---|---|
| 🟡 Yellow | Import in progress |
| 🟢 Green | Import succeeded |
| 🔴 Red | Error (path missing, no files found, load failed) |

---

## 5. Placement of Imported Models

**File:** `Assets/Scripts/Logic/PlacementManager.cs`  
**Responsibility:** Imported items integrate with `PlacementManager` the same way built-in catalog items do. On selection, a ghost copy is spawned for preview; on confirmation it is placed in the scene.

```csharp
public void StartPlacement(InventoryItemData item)
{
    ghostObject = Instantiate(item.prefab3D);
    ghostObject.SetActive(true);

    foreach (Collider col in ghostObject.GetComponentsInChildren<Collider>())
        col.enabled = false;

    SetGhostMaterial(validMaterial);
}
```

On placement confirmation the final object is registered with `FurnitureSaveManager` so it is included in the next save.

```csharp
void TryPlaceObject()
{
    GameObject newObj = Instantiate(
        currentItem.prefab3D,
        ghostObject.transform.position,
        ghostObject.transform.rotation
    );

    FurniturePrefabReference prefabRef = newObj.AddComponent<FurniturePrefabReference>();
    prefabRef.prefabPath = currentItem.name;

    saveManager.activeFurniture.Add(newObj);
}
```

---

## 6. Persistence

Imported GLB paths are stored in a JSON file so models are automatically reloaded on the next application start without requiring user action.

### Saving a path

```csharp
private void SaveGLBPath(string filePath)
{
    GLBPathList list = LoadGLBPathList();

    if (!list.paths.Contains(filePath))
        list.paths.Add(filePath);

    File.WriteAllText(_glbSavePath, JsonUtility.ToJson(list, true));
}
```

### Reloading on startup

```csharp
private async void LoadSavedGLBs()
{
    GLBPathList list = LoadGLBPathList();

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
```

---

## 7. Full Import Pipeline

```
1. User enters folder path in UI text field
           │
           ▼
2. GLBImportUI.OnImportFolder()
   └─ validates input
   └─ calls ImportGLBsFromDirectory()
           │
           ▼
3. InventoryManager.ImportGLBsFromDirectory()
   └─ Directory.GetFiles("*.glb", SearchOption.AllDirectories)
   └─ for each file → await GLBLoader.LoadGLB(path)
           │
           ▼
4. GLBLoader.LoadGLB()
   ├─ new GltfImport()
   ├─ await gltf.Load(filePath)
   ├─ await gltf.InstantiateMainSceneAsync(root.transform)
   └─ NormalizeModel(root)  →  centered + scaled to 1 unit
           │
           ▼
5. InventoryManager.CreateItemFromGLB()
   └─ ScriptableObject.CreateInstance<InventoryItemData>()
   └─ items.Add(item)
           │
           ▼
6. SaveGLBPath(filePath)
   └─ appends to imported_glbs.json
           │
           ▼
7. PopulateSlots()
   └─ item appears in inventory UI
           │
           ▼
8. User selects item → PlacementManager.StartPlacement()
   └─ ghost preview → TryPlaceObject() → scene placement
```

---

## 8. Error Handling Summary

| Scenario | Handling |
|---|---|
| Empty path | Callback `(false, "Path cannot be empty.")` |
| File / directory not found | Callback `(false, "Not found: ...")` |
| No `.glb` files in folder | Callback `(false, "No .glb files found.")` |
| GLTFast load failure | `Object.Destroy(root)`, returns `null` |
| Scene instantiation failure | `Object.Destroy(root)`, returns `null` |
| Previously saved file deleted | `Debug.LogWarning`, silently skipped |
| Partial batch failure | Reports `"Imported X of Y file(s)."` |

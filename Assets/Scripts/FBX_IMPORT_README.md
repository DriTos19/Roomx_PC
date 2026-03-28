# FBX Import System Documentation

## Overview
This system allows runtime import of 3rd party FBX 3D models into the Roomx_PC inventory and placement system.

## Components

### 1. FBXLoader.cs (Assets/Scripts/Logic/)
**Purpose:** Core FBX loading engine using Autodesk FBX SDK
**Key Methods:**
- `LoadFBX(string filePath)` - Loads an FBX file and converts it to a GameObject

**Features:**
- Robust error handling with detailed logging
- Converts FBX meshes to Unity meshes
- Automatically triangulates polygons
- Adds mesh colliders for interaction
- Proper normal recalculation for lighting

### 2. InventoryManager.cs (Assets/Scripts/Inventory/)
**Purpose:** Manages inventory items and FBX imports
**Key Methods:**
- `ImportFBXFromPath(string filePath)` - Import single FBX file
- `ImportFBXsFromDirectory(string directoryPath)` - Import all FBX files from directory
- `ShowItemDetails(InventoryItemData item)` - Display item information
- `PopulateSlots()` - Refresh inventory UI

**Features:**
- Creates dynamic InventoryItemData for imported models
- Integrates with existing inventory system
- Automatic icon fallback to defaultIcon
- File validation (existence, format)

### 3. FBXImportUI.cs (Assets/Scripts/Inventory/)
**Purpose:** UI for FBX file import
**Features:**
- Text input field for file path entry
- Single file import button
- Directory batch import button
- Status text display with color feedback

### 4. PlacementManager.cs (Assets/Scripts/Logic/)
**Purpose:** Handles placement of objects (updated for custom models)
**Key Updates:**
- New overload: `StartPlacement(GameObject prefab)` for custom models
- Tracks both inventory items and custom prefabs
- Full support for custom model placement and rotation

## Usage

### Method 1: Direct Path Import
```csharp
// In your code, get reference to InventoryManager
InventoryManager inventoryManager = GetComponent<InventoryManager>();

// Import single FBX file
inventoryManager.ImportFBXFromPath("C:/Models/chair.fbx");
```

### Method 2: Directory Import
```csharp
// Import all FBX files from directory
inventoryManager.ImportFBXsFromDirectory("C:/MyModels/Furniture/");
```

### Method 3: UI Import
1. Add FBXImportUI script to a UI GameObject
2. Create UI elements:
   - TMP_InputField for file path
   - Button for single import
   - Button for directory import
   - TextMeshPro for status text
3. Drag references to FBXImportUI component
4. User enters path and clicks button

### Method 4: Editor Testing
```csharp
// In any Update() method or OnGUI():
if (Input.GetKeyDown(KeyCode.I))
{
    InventoryManager inv = FindObjectOfType<InventoryManager>();
    inv.ImportFBXFromPath("C:/path/to/model.fbx");
}
```

## File Paths

### Example Windows Paths:
```
C:/Users/YourUsername/Desktop/model.fbx
C:\Users\YourUsername\Desktop\Models\
```

### Example Project-Relative Paths:
```
Assets/Models/furniture.fbx
../MyModels/model.fbx
```

## Workflow

1. **User provides FBX file path** (via UI input field or code)
2. **InventoryManager.ImportFBXFromPath()** is called
3. **FBXLoader.LoadFBX()** processes the FBX file
4. **Mesh data** is converted to Unity format
5. **GameObject** is created with MeshFilter, MeshRenderer, and BoxCollider
6. **InventoryItemData** is created dynamically
7. **Item appears in inventory** UI
8. **User can place** the model in the scene using normal placement mechanics

## Features

✅ Runtime FBX loading (no editor import needed)
✅ Automatic mesh conversion
✅ Polygon triangulation
✅ Batch import from directories
✅ Error handling with detailed logging
✅ Automatic material assignment (Standard shader)
✅ Collider generation
✅ Full placement system integration
✅ Inventory system integration
✅ Default icon fallback
✅ File validation

## Limitations

⚠️ Complex FBX files may load slowly
⚠️ Animations are not currently supported
⚠️ Only basic materials are applied (Standard shader)
⚠️ Textures from FBX are not automatically loaded
⚠️ Maximum recommended model complexity: ~50k polygons

## Troubleshooting

### "File does not exist" Error
- Check file path spelling
- Ensure file has .fbx extension
- Use forward slashes (/) or escaped backslashes (\\)

### Model appears black/unlit
- Normal recalculation may be failing on complex geometry
- Try simpler models first
- Check console for specific errors

### Model doesn't show collider
- Ensure MeshCollider component is added
- Check if mesh is valid (non-zero polygon count)

### Import very slow
- File may be too complex
- Check file size (~10MB+ may take time)
- Monitor console for progress

## Future Enhancements

- [ ] Support for embedded textures
- [ ] Animation support
- [ ] Material preservation from FBX
- [ ] Async loading for large files
- [ ] UI file browser dialog
- [ ] Model preview before import
- [ ] Custom collider shapes
- [ ] LOD support

## Files Modified

1. `Assets/Scripts/Logic/FBXLoader.cs` - Created/Enhanced
2. `Assets/Scripts/Logic/PlacementManager.cs` - Updated (added GameObject overload)
3. `Assets/Scripts/Inventory/InventoryManager.cs` - Updated (added import methods)
4. `Assets/Scripts/Inventory/FBXImportUI.cs` - Created
5. `Assets/Scripts/Inventory/ItemSlotUI.cs` - Minor update (icon fallback)
6. `Packages/manifest.json` - Updated (added fbx SDK, removed problematic package)

## Quick Start Checklist

- [ ] Ensure Autodesk FBX SDK is in manifest.json
- [ ] Verify FBXLoader.cs exists
- [ ] Verify InventoryManager.cs has no SFB errors
- [ ] Test with single FBX file path
- [ ] Add UI for user-friendly import
- [ ] Set defaultIcon in InventoryManager inspector
- [ ] Assign PlacementManager reference


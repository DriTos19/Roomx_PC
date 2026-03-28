# FBX Import Testing Guide

## Quick Test Setup

1. **Add the test script to your scene:**
   - Create a new GameObject called "FBXImportTest"
   - Attach the `FBXImportTest.cs` script to it
   - Assign your `InventoryManager` to the `inventoryManager` field

2. **Run the scene:**
   - The test will automatically try to import `baked_chair.fbx` on startup
   - Check the console for success/failure messages

3. **Manual testing during runtime:**
   - Press **T** to manually trigger an import test
   - Press **L** to list all current inventory items

## Expected Behavior

When the import succeeds, you should see:
- Console message: "Successfully imported FBX: [path]"
- The imported item appears in your inventory UI
- Item details show when clicked

## Troubleshooting

### If import fails:
1. Check that the FBX file exists at the expected path
2. Verify Autodesk FBX SDK is installed (check Packages/manifest.json)
3. Check console for detailed error messages

### If UI doesn't update:
1. Ensure `PopulateSlots()` is called after import
2. Check that `slotPrefab` and `slotParent` are assigned in InventoryManager
3. Verify `defaultIcon` is assigned for imported items

### If placement doesn't work:
1. Check that imported items have `prefab3D` assigned
2. Ensure `PlacementManager` can handle the imported GameObject

## Test Files Available

The project includes several FBX files you can test with:
- `Assets/Resources/Prefabs/Furniture/baked_chair.fbx`
- `Assets/Resources/Prefabs/Furniture/sofa.fbx`
- `Assets/Resources/Prefabs/Furniture/stuhl.fbx`
- And more in the Furniture directory

## Alternative Testing Methods

### Using FBXImportTester (Keyboard Shortcuts):
- Add `FBXImportTester.cs` to a GameObject
- Press **F** to import single file
- Press **D** to import directory
- Press **P** to list inventory count

### Using FBXImportUI (User Interface):
- Create UI with TMP_InputField and Buttons
- Add `FBXImportUI.cs` to a GameObject
- Type file path and click import

### Direct Script Call:
```csharp
InventoryManager inv = GetComponent<InventoryManager>();
inv.ImportFBXFromPath("C:/path/to/your/model.fbx");
```

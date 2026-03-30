# Quick Test UI Setup - 2 Minutes!

## 🚀 Option 1: Keyboard-Only Testing (FASTEST - 30 seconds)

Just add the **FBXImportTester** component and press keys:

1. Create an empty GameObject (name: "FBXImportTester")
2. Add `FBXImportTester.cs` script to it
3. Drag **InventoryManager** to the field
4. **Done!** Press:
   - **I** - Open input field for file path (then Enter to import)
   - **F** - Quick test single file import
   - **D** - Quick test directory import
   - **P** - Print all items

---

## 🎨 Option 2: Simple Canvas UI (3 minutes)

### Step 1: Create Canvas
1. Right-click in Hierarchy → UI → Panel
2. Select the Canvas
3. Set Canvas Scaler to "Scale with Screen Size"

### Step 2: Add UI Elements
1. Right-click Canvas → UI → Panel
   - Name: "ImportPanel"
   - Set size: Width 400, Height 300
   - Position: Center of screen

2. Right-click ImportPanel → UI → TextMeshPro - Input Field
   - Name: "FilePathInput"
   - Placeholder text: "Enter FBX file path..."
   - Height: 60

3. Right-click ImportPanel → UI → Button - TextMeshPro
   - Name: "ImportButton"
   - Text: "Import FBX"
   - Position: Below input field

4. Right-click ImportPanel → UI → Button - TextMeshPro
   - Name: "ImportDirButton"
   - Text: "Import Directory"
   - Position: Next to Import button

5. Right-click ImportPanel → UI → TextMeshPro - Text
   - Name: "StatusText"
   - Text: "Ready to import"
   - Color: Yellow

### Step 3: Add FBXImportUI Script
1. Create new empty GameObject → Name: "ImportManager"
2. Add `FBXImportUI.cs` script
3. Drag references:
   - **filePathInputField** → FilePathInput component
   - **importButton** → ImportButton component
   - **importFromDirectoryButton** → ImportDirButton component
   - **statusText** → StatusText component
   - **inventoryManager** → Your InventoryManager

### Step 4: Test
1. Play the game
2. Enter file path in input field: `C:/Models/model.fbx`
3. Click "Import FBX"
4. Check console for results

---

## 📝 Option 3: Ultra-Simple Debug Text (1 minute)

Add this to any script:

```csharp
void Update()
{
    // Press I to test import
    if (Input.GetKeyDown(KeyCode.I))
    {
        string path = "C:/path/to/your/model.fbx";
        FindObjectOfType<InventoryManager>().ImportFBXFromPath(path);
    }
}
```

Just change the path and press I!

---

## 🧪 Testing Checklist

### Before Testing
- [ ] Have an FBX file ready (get sample from internet if needed)
- [ ] Note the full file path: `C:/Users/YourName/Desktop/model.fbx`
- [ ] Open Console (Window → General → Console)

### Test Steps
1. [ ] Press I (or click Import button)
2. [ ] Enter your FBX file path
3. [ ] Watch Console for success/error messages
4. [ ] Check Hierarchy for imported model
5. [ ] Look in Inventory UI for new item
6. [ ] Select item and click to place
7. [ ] Use Q/E to rotate
8. [ ] Left-click to place, Right-click to cancel

---

## 🐛 Common Issues

### "File does not exist"
- Check file path spelling
- Use forward slashes: `C:/Models/model.fbx`
- Not backslashes: `C:\Models\model.fbx`

### Model doesn't appear
- Check Console for errors
- Ensure FBX file is valid
- Try a simpler model first

### UI doesn't work
- Verify Canvas exists in scene
- Check all references are assigned
- Console should show button clicks

---

## 📂 Example File Paths

### Windows
```
C:/Users/drini/Desktop/chair.fbx
C:/Models/Furniture/table.fbx
C:\Users\drini\Documents\model.fbx (with backslashes)
```

### Within Project
```
Assets/Models/model.fbx
../ExternalModels/furniture.fbx
```

---

## ✅ You're Ready!

Pick the option above that works for you:
1. **Quick & Easy**: Option 1 (Keyboard only)
2. **Professional**: Option 2 (Canvas UI)
3. **Debug**: Option 3 (Code)

**Start testing now!** 🚀

---

## 💡 Pro Tips

### Use Keyboard to Test Instantly
Press `I` and type: `C:/Models/model.fbx` then Enter

### Batch Import Test
Press `D` and type: `C:/Models/` to import all FBX files

### List All Imported
Press `P` to see count and names

### Debug Mode
Set test paths directly in FBXImportTester.cs line 31 & 51

---

**Need help? Check QUICK_REFERENCE.md for more examples!**


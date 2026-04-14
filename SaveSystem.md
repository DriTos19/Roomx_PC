# Save System Documentation

## Overview

The save system in RoomX is split into several independent managers, each responsible for persisting a specific area of application state. Two serialization strategies are used:

- **`JsonUtility`** — for complex structured data written to files in `Application.persistentDataPath`
- **`PlayerPrefs`** — for lightweight scalar values (floats, strings)

---

## Architecture

```
Save System
├── FurnitureSaveManager   → JSON        → furniture_data_<SceneName>.json
├── InventoryManager       → JSON        → imported_glbs.json
├── BudgetManager          → PlayerPrefs → "PlayerBudget"
└── PlayerPrefExample      → PlayerPrefs → "player_name", "volume"
```

---

## 1. Data Models

All classes serialized with `JsonUtility` must carry the `[System.Serializable]` attribute.

### FurnitureData

Represents the state of a single placed furniture object.

```csharp
[System.Serializable]
public class FurnitureData
{
    public string prefabName;   // name of the InventoryItemData asset
    public Vector3 position;
    public Quaternion rotation;
}
```

### SaveData

Root object written to the current scene's `furniture_data_<SceneName>.json`.

```csharp
[System.Serializable]
public class SaveData
{
    public string sceneName;
    public List<FurnitureData> allItems = new List<FurnitureData>();
}
```

### GLBPathList

Stores file-system paths of every GLB model imported at runtime.

```csharp
[System.Serializable]
private class GLBPathList
{
    public List<string> paths = new List<string>();
}
```

### InventoryItemData

A `ScriptableObject` describing a catalog item. The `prefab3D` reference is used during load to reconstruct placed furniture.

```csharp
[CreateAssetMenu(fileName = "NewInventoryItem", menuName = "Inventory/Item")]
public class InventoryItemData : ScriptableObject
{
    public string itemName;
    public GameObject prefab3D;
    public float price;
    public ItemCategory category;
}
```

---

## 2. FurnitureSaveManager

**File:** `Assets/Scripts/Save/FurnitureSaveManager.cs`  
**Responsibility:** Serializes and restores the position, rotation, and identity of every active furniture object in the scene.

### Initialization

The save path is resolved once in `Awake` so it is available before any other lifecycle method runs.

```csharp
void Awake()
{
    string sceneName = SceneManager.GetActiveScene().name;
    savePath = Path.Combine(Application.persistentDataPath, $"furniture_data_{sceneName}.json");
}
```

### Saving

`SaveGame` iterates over every tracked `GameObject`, reads its `FurniturePrefabReference` component to get the asset name, and writes the list to disk as pretty-printed JSON.

```csharp
public void SaveGame()
{
    SaveData data = new SaveData();
    data.sceneName = SceneManager.GetActiveScene().name;

    foreach (GameObject obj in activeFurniture)
    {
        FurniturePrefabReference reference = obj.GetComponent<FurniturePrefabReference>();
        FurnitureData item = new FurnitureData
        {
            prefabName = reference.prefabPath,
            position   = obj.transform.position,
            rotation   = obj.transform.rotation
        };
        data.allItems.Add(item);
    }

    string json = JsonUtility.ToJson(data, true);
    File.WriteAllText(savePath, json);
}
```

### Loading

`LoadGame` reads only the current scene's JSON file, verifies the save belongs to the active scene, clears currently placed furniture for that scene, then looks up each prefab by name from `Resources` and instantiates it at the saved transform.

```csharp
public void LoadGame()
{
    if (!File.Exists(savePath)) return;

    string json = File.ReadAllText(savePath);
    SaveData data = JsonUtility.FromJson<SaveData>(json);
    if (data.sceneName != SceneManager.GetActiveScene().name) return;

    foreach (FurnitureData item in data.allItems)
    {
        GameObject prefab = Resources.Load<GameObject>(item.prefabName);
        Instantiate(prefab, item.position, item.rotation);
    }
}
```

### Trigger Points

Saving and loading can be triggered from UI buttons or from keyboard shortcuts.

```csharp
void Start()
{
    saveButton.onClick.AddListener(SaveGame);
    loadButton.onClick.AddListener(LoadGame);
}

void Update()
{
    if (Input.GetKeyDown(KeyCode.S)) SaveGame();
    if (Input.GetKeyDown(KeyCode.L)) LoadGame();
}
```

### Tracking Active Furniture

`PlacementManager` keeps the `activeFurniture` list up to date so the save manager always reflects the current scene state.

```csharp
// PlacementManager.cs
void TryPlaceObject()
{
    // ... placement logic ...
    saveManager.activeFurniture.Add(placedObject);
}

public void PickUpFurniture(GameObject obj)
{
    saveManager.activeFurniture.Remove(obj);
}
```

---

## 3. BudgetManager

**File:** `Assets/Scripts/Inventory/BudgetManager.cs`  
**Responsibility:** Persists the player's current balance using `PlayerPrefs`.

The manager follows the **Singleton** pattern and loads the saved balance immediately in `Awake`.

```csharp
void Awake()
{
    Instance = this;
    DontDestroyOnLoad(gameObject);
    Load();
}
```

Balance changes are always flushed to `PlayerPrefs` through `SetBalance`, keeping the in-memory and persisted values in sync.

```csharp
private const string SAVE_KEY = "PlayerBudget";

public void SetBalance(float newBalance)
{
    _balance = newBalance;
    Save();
    onBalanceChanged.Invoke(_balance);
}

private void Save()
{
    PlayerPrefs.SetFloat(SAVE_KEY, _balance);
    PlayerPrefs.Save();
}

private void Load()
{
    _balance = PlayerPrefs.GetFloat(SAVE_KEY, startingBalance);
}
```

Spending and adding funds both route through `SetBalance`, ensuring the value on disk is always up to date.

```csharp
public bool TrySpend(float cost)
{
    if (!CanAfford(cost)) return false;
    SetBalance(_balance - cost);
    return true;
}

public void AddFunds(float amount)
{
    SetBalance(_balance + amount);
}
```

To wipe saved progress, the key is explicitly deleted and the balance is reset to the starting value.

```csharp
public void ResetBudget()
{
    PlayerPrefs.DeleteKey(SAVE_KEY);
    _balance = startingBalance;
    onBalanceChanged.Invoke(_balance);
}
```

---

## 4. InventoryManager — GLB Import Persistence

**File:** `Assets/Scripts/Inventory/InventoryManager.cs`  
**Responsibility:** Remembers which custom GLB models the user imported from disk and reloads them automatically on the next application start.

The path to the JSON file is set in `Awake`, and previously imported models are reloaded asynchronously in `Start`.

```csharp
void Awake()
{
    _glbSavePath = Application.persistentDataPath + "/imported_glbs.json";
}

void Start()
{
    LoadSavedGLBs();
}
```

When the user imports a new model, its file path is appended to the persisted list.

```csharp
private void SaveGLBPath(string filePath)
{
    GLBPathList list = LoadGLBPathList();

    if (!list.paths.Contains(filePath))
        list.paths.Add(filePath);

    File.WriteAllText(_glbSavePath, JsonUtility.ToJson(list, true));
}
```

On startup, every saved path is re-imported so custom items appear in the inventory without any user action.

```csharp
private async void LoadSavedGLBs()
{
    GLBPathList list = LoadGLBPathList();

    foreach (string path in list.paths)
    {
        if (File.Exists(path))
            await ImportGLBFromPath(path, null);
    }
}
```

The JSON file is read through a single helper that returns a fresh empty list when no file exists yet.

```csharp
private GLBPathList LoadGLBPathList()
{
    if (!File.Exists(_glbSavePath))
        return new GLBPathList();

    string json = File.ReadAllText(_glbSavePath);
    return JsonUtility.FromJson<GLBPathList>(json);
}
```

---

## 5. User Preferences

**File:** `Assets/Scripts/PlayerPrefExample.cs`  
**Key constants:** `Assets/Scripts/PlayerPrefKeys.cs`  
**Responsibility:** Persists the player's display name and master volume level.

All key names are defined as constants to prevent typos across the codebase.

```csharp
public static class PlayerPrefKeys
{
    public const string PlayerName = "player_name";
    public const string Volume     = "volume";
}
```

Settings are read back and applied to the UI controls on load.

```csharp
public void LoadSettings()
{
    nameInput.text     = PlayerPrefs.GetString(PlayerPrefKeys.PlayerName, "Player");
    volumeSlider.value = PlayerPrefs.GetFloat(PlayerPrefKeys.Volume, 1f);
}
```

Settings are written and flushed when the user confirms their input.

```csharp
public void saveSettings()
{
    PlayerPrefs.SetString(PlayerPrefKeys.PlayerName, nameInput.text);
    PlayerPrefs.SetFloat(PlayerPrefKeys.Volume,      volumeSlider.value);
    PlayerPrefs.Save();
}
```

Individual keys can be removed to restore the default values.

```csharp
public void ResetSettings()
{
    PlayerPrefs.DeleteKey(PlayerPrefKeys.PlayerName);
    PlayerPrefs.DeleteKey(PlayerPrefKeys.Volume);
    LoadSettings();
}
```

---

## 6. Save File Locations

| Data | Storage | Path / Key |
|---|---|---|
| Furniture state | JSON file | `Application.persistentDataPath/furniture_data_<SceneName>.json` |
| Imported GLB paths | JSON file | `Application.persistentDataPath/imported_glbs.json` |
| Player balance | PlayerPrefs | `"PlayerBudget"` |
| Player name | PlayerPrefs | `"player_name"` |
| Master volume | PlayerPrefs | `"volume"` |

On Windows the persistent data path resolves to:  
`C:\Users\<user>\AppData\LocalLow\<CompanyName>\<ProductName>\`

---

## 7. Save & Load Flow

```
User places furniture
        │
        ▼
PlacementManager.TryPlaceObject()
  └─► activeFurniture.Add(obj)
        │
        ▼
User presses S  /  clicks Save button
        │
        ▼
FurnitureSaveManager.SaveGame()
  ├─ Reads FurniturePrefabReference from each object
  ├─ Builds SaveData { List<FurnitureData> }
  └─ JsonUtility.ToJson → File.WriteAllText

────────────────────────────────────────────────

Application start  /  User presses L
        │
        ▼
FurnitureSaveManager.LoadGame()
  ├─ File.ReadAllText → JsonUtility.FromJson<SaveData>
  ├─ Resources.Load<GameObject>(prefabName)
  └─ Instantiate(prefab, position, rotation)
```

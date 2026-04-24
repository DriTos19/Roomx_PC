using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class FurnitureSaveManager : MonoBehaviour
{
    public List<GameObject> activeFurniture = new List<GameObject>();
    private string savePath;

    [Header("UI Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    
    [Header("Debug")]
    [SerializeField] private bool enablePointerDebug = true;

    void Awake() {
        RefreshSavePath();
    }

    void Start() {
        // Try to auto-assign buttons by name if they were not wired in the inspector
        TryAutoAssignButtons();

        // Ensure there's an EventSystem present so UI receives pointer events (debug helper)
        EnsureEventSystemExists();

        // Log diagnostic information about the EventSystem which is required for UI clicks
        Debug.Log("FurnitureSaveManager: EventSystem present? " + (EventSystem.current != null));
        Debug.Log($"FurnitureSaveManager: Cursor.lockState={Cursor.lockState}, Cursor.visible={Cursor.visible}");

        if (saveButton != null) {
            Debug.Log("FurnitureSaveManager: Save button assigned");
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(() => {
                Debug.Log("FurnitureSaveManager: Save button clicked (UI)");
                SaveGame();
            });
        } else {
            Debug.Log("FurnitureSaveManager: Save button is NOT assigned in the inspector");
        }

        if (loadButton != null) {
            Debug.Log("FurnitureSaveManager: Load button assigned");
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(() => {
                Debug.Log("FurnitureSaveManager: Load button clicked (UI)");
                LoadGame();
            });
        } else {
            Debug.Log("FurnitureSaveManager: Load button is NOT assigned in the inspector");
        }

        // Provide detailed diagnostics for both buttons so you can see why clicks might not be registered
        LogButtonDiagnostics(saveButton, "Save");
        LogButtonDiagnostics(loadButton, "Load");
    }

    // This checks for key presses every single frame
    void Update() {
        // Pointer debug: on left mouse click, log what UI elements are under the cursor using GraphicRaycaster
        if (enablePointerDebug && Input.GetMouseButtonDown(0))
        {
            DebugUIUnderPointer();
        }

        // Press F5 to Save (avoids conflict with WASD movement in sandbox)
        if (Input.GetKeyDown(KeyCode.F5)) {
            SaveGame();
        }

        // Press F9 to Load (avoids conflict with WASD movement in sandbox)
        if (Input.GetKeyDown(KeyCode.F9)) {
            LoadGame();
        }
    }

    public void SaveGame() {
        RefreshSavePath();

        SaveData data = new SaveData();
        data.sceneName = SceneManager.GetActiveScene().name;
        activeFurniture.RemoveAll(item => item == null);

        Debug.Log($"=== SAVE GAME STARTED ===");
        Debug.Log($"activeFurniture list has {activeFurniture.Count} items before saving");
        Debug.Log($"Save path: {savePath}");

        foreach (GameObject obj in activeFurniture) {
            FurniturePrefabReference prefabRef = obj.GetComponent<FurniturePrefabReference>();
            string prefabName = prefabRef != null ? prefabRef.prefabPath : obj.name.Replace("(Clone)", "").Trim();
            
            // If we have itemData, use its actual name for consistency
            if (prefabRef != null && prefabRef.itemData != null)
                prefabName = prefabRef.itemData.name;
            
            FurnitureData itemData = new FurnitureData {
                prefabName = prefabName,
                position = obj.transform.position,
                rotation = obj.transform.rotation,
                isRuntimeGLB = prefabRef != null && !string.IsNullOrEmpty(prefabRef.glbSourceFilePath),
                glbSourceFilePath = prefabRef != null ? prefabRef.glbSourceFilePath : ""
            };
            data.allItems.Add(itemData);
            Debug.Log("Saved: " + prefabName + " at " + obj.transform.position + 
                     (itemData.isRuntimeGLB ? " (GLB from: " + itemData.glbSourceFilePath + ")" : ""));
        }

        string json = JsonUtility.ToJson(data, true);
        Debug.Log($"Preparing to save {data.allItems.Count} items");
        Debug.Log("JSON to save: " + json);
        
        try {
            File.WriteAllText(savePath, json);
            Debug.Log($"✓ FILE SAVED SUCCESSFULLY! {data.allItems.Count} items written to: {savePath}");
        } catch (System.Exception e) {
            Debug.LogError("Failed to save file: " + e.Message);
        }
        Debug.Log("=== SAVE GAME COMPLETE ===");
    }

    public void LoadGame() {
        RefreshSavePath();
        Debug.Log("=== LOAD GAME STARTED ===");
        Debug.Log("Save file path: " + savePath);
        
        if (!File.Exists(savePath)) {
            Debug.LogWarning("No save file found at: " + savePath);
            Debug.LogWarning("Press F5 first to save furniture!");
            Debug.LogWarning("=== LOAD GAME ABORTED ===");
            return;
        }

        Debug.Log("✓ Save file exists!");
        string json = File.ReadAllText(savePath);
        Debug.Log("Raw JSON content: " + json);
        Debug.Log("JSON length: " + json.Length);
        
        if (string.IsNullOrEmpty(json)) {
            Debug.LogWarning("Save file is empty!");
            Debug.LogWarning("=== LOAD GAME ABORTED ===");
            return;
        }

        try {
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            
            if (data == null || data.allItems == null) {
                Debug.LogWarning("Failed to deserialize JSON - data is null!");
                return;
            }

            string activeSceneName = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(data.sceneName) && data.sceneName != activeSceneName) {
                Debug.LogWarning("Save file belongs to scene '" + data.sceneName + "' and will not load in scene '" + activeSceneName + "'.");
                return;
            }
            
            // Clear existing furniture before loading to prevent duplicates
            activeFurniture.RemoveAll(item => item == null);
            foreach (GameObject obj in activeFurniture)
                Destroy(obj);
            activeFurniture.Clear();

            if (data.allItems.Count == 0) {
                Debug.LogWarning("Save file has no furniture items!");
                Debug.LogWarning("=== LOAD GAME ABORTED ===");
                return;
            }

            Debug.Log($"✓ Found {data.allItems.Count} items in save file to load.");


            foreach (FurnitureData item in data.allItems) {
                // Handle runtime GLB items differently
                if (item.isRuntimeGLB && !string.IsNullOrEmpty(item.glbSourceFilePath))
                {
                    LoadRuntimeGLBItem(item);
                }
                else
                {
                    LoadStandardItem(item);
                }
            }
            
            Debug.Log($"✓ Load complete! Total furniture loaded: {activeFurniture.Count} / {data.allItems.Count}");
            Debug.Log("=== LOAD GAME COMPLETE ===");
        } catch (System.Exception e) {
            Debug.LogError("Error during load: " + e.Message + "\n" + e.StackTrace);
        }
    }

    private async void LoadRuntimeGLBItem(FurnitureData item)
    {
        // Check if the file still exists
        if (!File.Exists(item.glbSourceFilePath))
        {
            Debug.LogWarning($"GLB file no longer exists: {item.glbSourceFilePath}");
            return;
        }

        // Reload the GLB from disk
        GameObject loaded = await GLBLoader.LoadGLB(item.glbSourceFilePath);
        if (loaded == null)
        {
            Debug.LogError($"Failed to reload GLB: {item.glbSourceFilePath}");
            return;
        }

        // Instantiate at the saved position and rotation
        GameObject newObj = Instantiate(loaded, item.position, item.rotation);
        newObj.SetActive(true);

        // Add the reference component
        FurniturePrefabReference prefabRef = newObj.GetComponent<FurniturePrefabReference>();
        if (prefabRef == null)
            prefabRef = newObj.AddComponent<FurniturePrefabReference>();
        prefabRef.prefabPath = item.prefabName;
        prefabRef.glbSourceFilePath = item.glbSourceFilePath;

        // Restore FurnitureInstance so WallPlacer_PC can edit and re-place this object.
        // We create a minimal runtime InventoryItemData using the already-loaded GLB model as the prefab.
        FurnitureInstance instance = newObj.GetComponent<FurnitureInstance>();
        if (instance == null)
            instance = newObj.AddComponent<FurnitureInstance>();

        InventoryItemData glbItemData = ScriptableObject.CreateInstance<InventoryItemData>();
        glbItemData.name = item.prefabName;
        glbItemData.itemName = item.prefabName;
        glbItemData.prefab3D = loaded;
        glbItemData.glbSourceFilePath = item.glbSourceFilePath;
        instance.itemDataSandbox = glbItemData;
        prefabRef.itemData = glbItemData;

        // Put object on the Furniture layer
        int furnitureLayer = LayerMask.NameToLayer("Furniture");
        if (furnitureLayer != -1)
            SetLayerRecursively(newObj, furnitureLayer);

        // Register with save manager list
        if (!activeFurniture.Contains(newObj))
            activeFurniture.Add(newObj);

        Debug.Log("Loaded GLB: " + item.prefabName + " from " + item.glbSourceFilePath);
    }

    private void LoadStandardItem(FurnitureData item)
    {
        Debug.Log("Attempting to load InventoryItemData: " + item.prefabName);
        
        // Try multiple possible Resources paths where InventoryItemData assets might be stored
        string[] possiblePaths = new string[]
        {
            "ScriptableObjects/InventoryItems/" + item.prefabName,
            "Items/" + item.prefabName,  // Alternative location
            item.prefabName  // Root of Resources folder
        };

        InventoryItemData itemData = null;
        string foundPath = "";

        foreach (string path in possiblePaths)
        {
            itemData = Resources.Load<InventoryItemData>(path);
            if (itemData != null)
            {
                foundPath = path;
                Debug.Log($"  ✓ Found at: Resources/{path}");
                break;
            }
        }

        // If still not found, try normalized name (remove spaces, underscores)
        if (itemData == null && !string.IsNullOrEmpty(item.prefabName)) {
            string normalizedName = item.prefabName.Replace("_", "").Replace(" ", "");
            
            foreach (string basePath in new string[] { "ScriptableObjects/InventoryItems/", "Items/", "" })
            {
                itemData = Resources.Load<InventoryItemData>(basePath + normalizedName);
                if (itemData != null)
                {
                    foundPath = basePath + normalizedName;
                    Debug.Log($"  ✓ Found with normalized name at: Resources/{foundPath}");
                    break;
                }
            }
            
            if (itemData == null) {
                Debug.LogWarning($"  ✗ NOT FOUND: Searched in Resources/ScriptableObjects/InventoryItems/, Resources/Items/, and root");
            }
        }

        if (itemData != null && itemData.prefab3D != null) {
            GameObject newObj = Instantiate(itemData.prefab3D, item.position, item.rotation);
            newObj.SetActive(true);

            // Ensure FurniturePrefabReference exists and is populated. This partial class exists in two files
            // so it contains both 'prefabPath' (save) and 'itemData' (logic).
            FurniturePrefabReference prefabRef = newObj.GetComponent<FurniturePrefabReference>();
            if (prefabRef == null)
                prefabRef = newObj.AddComponent<FurniturePrefabReference>();
            prefabRef.prefabPath = itemData.name;  // Use the actual asset name to ensure consistency
            prefabRef.itemData = itemData;

            // Ensure FurnitureInstance exists and points back to the InventoryItemData so WallPlacer can edit/pick it
            FurnitureInstance instance = newObj.GetComponent<FurnitureInstance>();
            if (instance == null)
                instance = newObj.AddComponent<FurnitureInstance>();
            instance.itemDataSandbox = itemData;

            // Put object on the Furniture layer so placement/wall placer pickup raycasts will find it
            int furnitureLayer = LayerMask.NameToLayer("Furniture");
            if (furnitureLayer != -1)
                SetLayerRecursively(newObj, furnitureLayer);

            // Register with save manager list
            if (!activeFurniture.Contains(newObj))
                activeFurniture.Add(newObj);

            Debug.Log("✓ Loaded: " + itemData.name + " at " + item.position);
        } else {
            // Gracefully skip items that no longer exist in resources (deleted/renamed items)
            Debug.LogWarning("✗ Skipped loading missing item: '" + item.prefabName + "' — asset no longer exists at Resources/ScriptableObjects/InventoryItems/");
            if (itemData != null && itemData.prefab3D == null) {
                Debug.LogWarning("  Item '" + item.prefabName + "' has null prefab3D");
            }
        }
    }

    private void RefreshSavePath() {
        string sceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrWhiteSpace(sceneName)) {
            sceneName = "scene_" + SceneManager.GetActiveScene().buildIndex;
        }

        foreach (char invalidChar in Path.GetInvalidFileNameChars()) {
            sceneName = sceneName.Replace(invalidChar, '_');
        }

        savePath = Path.Combine(Application.persistentDataPath, "furniture_data_" + sceneName + ".json");
    }

    // Ensure loaded objects are put on the correct layer so other systems (placement, raycasts)
    // can find them. Also used to keep runtime behavior consistent with freshly placed objects.
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        obj.layer = layer;
        foreach (Transform t in obj.transform)
            SetLayerRecursively(t.gameObject, layer);
    }

    private void TryAutoAssignButtons()
    {
        if (saveButton == null || loadButton == null)
        {
            Button[] allButtons = FindObjectsOfType<Button>(true);

            foreach (Button b in allButtons)
            {
                string n = b.gameObject.name.ToLowerInvariant();
                if (saveButton == null && n.Contains("save"))
                    saveButton = b;
                if (loadButton == null && (n.Contains("load") || n.Contains("load") || n.Contains("open")))
                    loadButton = b;
                if (saveButton != null && loadButton != null)
                    break;
            }
        }
    }

    private void LogButtonDiagnostics(Button btn, string label)
    {
        if (btn == null)
        {
            Debug.Log($"FurnitureSaveManager: {label} Button = null (not assigned)");
            return;
        }

        Debug.Log($"FurnitureSaveManager: {label} Button diagnostics:\n" +
                  $"  GameObject activeInHierarchy: {btn.gameObject.activeInHierarchy}\n" +
                  $"  Interactable: {btn.interactable}\n" +
                  $"  Layer: {btn.gameObject.layer}\n" +
                  $"  Persistent listener count: {btn.onClick.GetPersistentEventCount()}");

        var targetGraphic = btn.targetGraphic;
        Debug.Log($"  targetGraphic present: {targetGraphic != null}, raycastTarget: { (targetGraphic != null ? targetGraphic.raycastTarget.ToString() : "N/A") }");

        // Check parent Canvas and GraphicRaycaster
        Canvas canvas = btn.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            var gr = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log($"  Parent Canvas: {canvas.gameObject.name}, renderMode: {canvas.renderMode}, GraphicRaycaster present: { (gr != null) }");
        }
        else
        {
            Debug.Log("  No parent Canvas found for this button");
        }

        // Check for CanvasGroup up the parent chain which may block clicks
        Transform t = btn.transform;
        while (t != null)
        {
            CanvasGroup cg = t.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                Debug.Log($"  Found CanvasGroup on {t.gameObject.name}: interactable={cg.interactable}, blocksRaycasts={cg.blocksRaycasts}, alpha={cg.alpha}");
            }
            t = t.parent;
        }
    }

    private void DebugUIUnderPointer()
    {
        if (EventSystem.current == null)
        {
            Debug.Log("FurnitureSaveManager: EventSystem.current is null — UI won't receive pointer events.");
            return;
        }

        GraphicRaycaster gr = FindObjectOfType<GraphicRaycaster>();
        if (gr == null)
        {
            Debug.Log("FurnitureSaveManager: No GraphicRaycaster found in scene — UI won't receive pointer events.");
            return;
        }

        PointerEventData ped = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
        gr.Raycast(ped, results);

        if (results.Count == 0)
        {
            Debug.Log("FurnitureSaveManager: Pointer raycast found NO UI elements under cursor at position " + Input.mousePosition);
            return;
        }

        Debug.Log("FurnitureSaveManager: Pointer raycast results (top to bottom):");
        bool invoked = false;
        foreach (var r in results)
        {
            Debug.Log($"  hit: {r.gameObject.name} (module: {r.module?.GetType().Name}, distance: {r.distance})");

            // Debug-only: if the raycast hits the save or load button (or one of their children), invoke the click handler
            if (!invoked && saveButton != null && (r.gameObject == saveButton.gameObject || r.gameObject.transform.IsChildOf(saveButton.transform)))
            {
                Debug.Log("FurnitureSaveManager: Auto-invoking Save button (debug mode)");
                saveButton.onClick.Invoke();
                invoked = true;
            }

            if (!invoked && loadButton != null && (r.gameObject == loadButton.gameObject || r.gameObject.transform.IsChildOf(loadButton.transform)))
            {
                Debug.Log("FurnitureSaveManager: Auto-invoking Load button (debug mode)");
                loadButton.onClick.Invoke();
                invoked = true;
            }
        }
    }

    private void EnsureEventSystemExists()
    {
        if (EventSystem.current != null)
            return;

        Debug.Log("FurnitureSaveManager: No EventSystem present — creating one for debug (EventSystem + StandaloneInputModule)");
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    [System.Serializable]
    public class FurnitureData {
        public string prefabName;
        public Vector3 position;
        public Quaternion rotation;
        public bool isRuntimeGLB;           // True if this is an imported GLB model
        public string glbSourceFilePath;    // Original file path for runtime GLBs
    }

    [System.Serializable]
    public class SaveData {
        public string sceneName;
        public List<FurnitureData> allItems = new List<FurnitureData>();
    }
}
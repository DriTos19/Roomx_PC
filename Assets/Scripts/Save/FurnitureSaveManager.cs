using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

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

        // Debug helper: if pointer debug is enabled, ensure cursor is unlocked & visible so UI can be clicked
        if (enablePointerDebug)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("FurnitureSaveManager: Pointer debug enabled — unlocking cursor and making it visible.");
        }

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

        // Press 'S' to Save
        if (Input.GetKeyDown(KeyCode.S)) {
            SaveGame();
        }

        // Press 'L' to Load
        if (Input.GetKeyDown(KeyCode.L)) {
            LoadGame();
        }
    }

    public void SaveGame() {
        RefreshSavePath();

        SaveData data = new SaveData();
        data.sceneName = SceneManager.GetActiveScene().name;
        activeFurniture.RemoveAll(item => item == null);

        foreach (GameObject obj in activeFurniture) {
            // Get the prefab reference from the component
            FurniturePrefabReference prefabRef = obj.GetComponent<FurniturePrefabReference>();
            string prefabName = prefabRef != null ? prefabRef.prefabPath : obj.name.Replace("(Clone)", "").Trim();
            
            FurnitureData itemData = new FurnitureData {
                prefabName = prefabName,
                position = obj.transform.position,
                rotation = obj.transform.rotation
            };
            data.allItems.Add(itemData);
            Debug.Log("Saved: " + prefabName + " at " + obj.transform.position);
        }

        string json = JsonUtility.ToJson(data, true);
        Debug.Log("JSON to save: " + json);
        
        try {
            File.WriteAllText(savePath, json);
            Debug.Log("FILE SAVED! Look here: " + savePath);
        } catch (System.Exception e) {
            Debug.LogError("Failed to save file: " + e.Message);
        }
    }

    public void LoadGame() {
        RefreshSavePath();
        Debug.Log("Save file path: " + savePath);
        
        if (!File.Exists(savePath)) {
            Debug.LogWarning("No save file found at: " + savePath);
            Debug.LogWarning("Press 'S' first to save furniture!");
            return;
        }

        string json = File.ReadAllText(savePath);
        Debug.Log("Raw JSON content: " + json);
        Debug.Log("JSON length: " + json.Length);
        
        if (string.IsNullOrEmpty(json)) {
            Debug.LogWarning("Save file is empty!");
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
                return;
            }

            Debug.Log("Found " + data.allItems.Count + " items to load.");

            foreach (FurnitureData item in data.allItems) {
                Debug.Log("Attempting to load InventoryItemData: " + item.prefabName);
                
                // Load the InventoryItemData ScriptableObject from the correct path
                InventoryItemData itemData = Resources.Load<InventoryItemData>("ScriptableObjects/InventoryItems/" + item.prefabName);

                if (itemData != null && itemData.prefab3D != null) {
                    GameObject newObj = Instantiate(itemData.prefab3D, item.position, item.rotation);
                    newObj.SetActive(true);

                    // Ensure FurniturePrefabReference exists and is populated. This partial class exists in two files
                    // so it contains both 'prefabPath' (save) and 'itemData' (logic).
                    FurniturePrefabReference prefabRef = newObj.GetComponent<FurniturePrefabReference>();
                    if (prefabRef == null)
                        prefabRef = newObj.AddComponent<FurniturePrefabReference>();
                    prefabRef.prefabPath = item.prefabName;
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

                    Debug.Log("Loaded: " + item.prefabName);
                } else {
                    Debug.LogError("FAILED: Cannot find InventoryItemData at Resources/ScriptableObjects/InventoryItems/" + item.prefabName);
                    if (itemData != null && itemData.prefab3D == null) {
                        Debug.LogError("InventoryItemData found but prefab3D is null!");
                    }
                }
            }
            
            Debug.Log("Load complete! Total furniture loaded: " + activeFurniture.Count);
        } catch (System.Exception e) {
            Debug.LogError("Error during load: " + e.Message + "\n" + e.StackTrace);
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
    }

    [System.Serializable]
    public class SaveData {
        public string sceneName;
        public List<FurnitureData> allItems = new List<FurnitureData>();
    }
}

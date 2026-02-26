using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class FurnitureSaveManager : MonoBehaviour
{
    public List<GameObject> activeFurniture = new List<GameObject>();
    private string savePath;

    void Awake() {
        savePath = Application.persistentDataPath + "/furniture_data.json";
    }

    // This checks for key presses every single frame
    void Update() {
        // Press 'S' to Save
        if (Input.GetKeyDown(KeyCode.S)) {
            SaveGame();
        }

        // Press 'L' to Load (Duplicates)
        if (Input.GetKeyDown(KeyCode.L)) {
            LoadGame();
        }
    }

    public void SaveGame() {
        SaveData data = new SaveData();
        activeFurniture.RemoveAll(item => item == null);

        foreach (GameObject obj in activeFurniture) {
            FurnitureData itemData = new FurnitureData {
                prefabName = obj.name.Replace("(Clone)", "").Trim(),
                position = obj.transform.position,
                rotation = obj.transform.rotation
            };
            data.allItems.Add(itemData);
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log("FILE SAVED! Look here: " + savePath);
    }

    public void LoadGame() {
        if (!File.Exists(savePath)) {
            Debug.LogWarning("No save file found! Press 'S' first.");
            return;
        }

        string json = File.ReadAllText(savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        foreach (FurnitureData item in data.allItems) {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Furniture/" + item.prefabName);

            if (prefab != null) {
                GameObject newObj = Instantiate(prefab, item.position, item.rotation);
                activeFurniture.Add(newObj);
                Debug.Log("SPAWNED DUPLICATE: " + item.prefabName);
            } else {
                Debug.LogError("FAILED: Cannot find " + item.prefabName + " in Resources/Prefabs/Furniture/");
            }
        }
    }

    [System.Serializable]
    public class FurnitureData {
        public string prefabName;
        public Vector3 position;
        public Quaternion rotation;
    }

    [System.Serializable]
    public class SaveData {
        public List<FurnitureData> allItems = new List<FurnitureData>();
    }
}
using UnityEngine;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance;

    public LayerMask groundLayer;
    public Material ghostMaterial;
    public InventoryManager inventoryManager;

    public float rotationSpeed = 120f;

    private GameObject ghostObject;
    private InventoryItemData currentItem;
    private GameObject currentPrefab;
    private float currentRotationY;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (ghostObject == null) return;

        FollowMouse();
        HandleRotation();

        if (Input.GetMouseButtonDown(0))
            PlaceObject();

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            CancelPlacement();
    }

    public void StartPlacement(InventoryItemData item)
    {
        CancelPlacement();

        currentItem = item;
        ghostObject = Instantiate(item.prefab3D);

        currentRotationY = 0;
        SetGhostMaterial(ghostObject);
    }

    public void StartPlacement(GameObject prefab)
    {
        CancelPlacement();

        currentItem = null; // No item data for custom models
        currentPrefab = prefab;
        ghostObject = Instantiate(prefab);

        currentRotationY = 0;
        SetGhostMaterial(ghostObject);
    }

    void FollowMouse()
    {
        // Place object at a fixed distance in front of the camera
        float distanceInFront = 3.0f; // Distance in front of camera
        ghostObject.transform.position = Camera.main.transform.position + Camera.main.transform.forward * distanceInFront;

        // Optional: Still allow ground snapping if needed
        // Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        // {
        //     ghostObject.transform.position = hit.point + Vector3.up * 0.0f;
        // }
    }

    void HandleRotation()
    {
        if (Input.GetKey(KeyCode.Q))
            currentRotationY -= rotationSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.E))
            currentRotationY += rotationSpeed * Time.deltaTime;

        ghostObject.transform.rotation = Quaternion.Euler(0, currentRotationY, 0);
    }

    void PlaceObject()
    {
        GameObject newObj = Instantiate(currentItem != null ? currentItem.prefab3D : currentPrefab,
            ghostObject.transform.position,
            ghostObject.transform.rotation);

        // Add a component to store the prefab name for save/load
        FurniturePrefabReference prefabRef = newObj.AddComponent<FurniturePrefabReference>();
        prefabRef.prefabPath = currentItem != null ? currentItem.name : currentPrefab.name; // Use the ScriptableObject name or prefab name as the identifier

        // Register the placed object with the FurnitureSaveManager
        FurnitureSaveManager saveManager = FindObjectOfType<FurnitureSaveManager>();
        if (saveManager != null) {
            saveManager.activeFurniture.Add(newObj);
            Debug.Log("Added " + newObj.name + " to activeFurniture list");
        } else {
            Debug.LogWarning("FurnitureSaveManager not found in scene!");
        }

        CancelPlacement();
    }

    void CancelPlacement()
    {
        if (ghostObject != null)
            Destroy(ghostObject);

        ghostObject = null;
        currentItem = null;
        currentPrefab = null;
    }

    void SetGhostMaterial(GameObject obj)
    {
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
            r.material = ghostMaterial;
    }

    /// <summary>
    /// Picks up a placed furniture object and prepares it for re-placement.
    /// </summary>
    public void PickUpFurniture(GameObject furniture)
    {
        if (furniture == null)
        {
            Debug.LogWarning("Attempted to pick up null furniture!");
            return;
        }

        // Get the prefab reference from the furniture
        FurniturePrefabReference prefabRef = furniture.GetComponent<FurniturePrefabReference>();
        if (prefabRef == null)
        {
            Debug.LogWarning("Furniture does not have a FurniturePrefabReference component!");
            return;
        }

        // Try to find the original InventoryItemData or create a temporary placement
        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();
        InventoryItemData itemData = null;

        if (inventoryManager != null)
        {
            // Search for the item in the inventory
            foreach (InventoryItemData item in inventoryManager.items)
            {
                if (item.name == prefabRef.prefabPath || item.itemName == prefabRef.prefabPath)
                {
                    itemData = item;
                    break;
                }
            }
        }

        if (itemData != null)
        {
            // Start placement with the found item
            StartPlacement(itemData);
        }
        else
        {
            // If no item data found, start placement with the prefab directly
            StartPlacement(furniture);
        }

        // Remove the furniture from the scene
        FurnitureSaveManager saveManager = FindObjectOfType<FurnitureSaveManager>();
        if (saveManager != null)
        {
            saveManager.activeFurniture.Remove(furniture);
            Debug.Log("Removed " + furniture.name + " from activeFurniture list");
        }

        Destroy(furniture);
        Debug.Log("Picked up furniture: " + furniture.name);
    }
}

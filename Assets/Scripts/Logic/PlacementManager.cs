using UnityEngine;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance;

    [Header("Layers")]
    public LayerMask groundLayer;
    public LayerMask furnitureLayer;

    [Header("Materials")]
    public Material validMaterial;
    public Material invalidMaterial;

    [Header("Settings")]
    public float rotationSpeed = 120f;
    public float gridSize = 1f;
    public bool enableSnapping = true;

    private GameObject ghostObject;
    private InventoryItemData currentItem;

    private float currentRotationY;
    private bool isValidPlacement = true;

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
            PlaceObject(); // ✅ UPDATED

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            CancelPlacement();
    }

    public void StartPlacement(InventoryItemData item)
    {
        CancelPlacement();

        currentItem = item;
        ghostObject = Instantiate(item.prefab3D);

        currentRotationY = 0f;

        foreach (Collider col in ghostObject.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        SetGhostMaterial(validMaterial);
    }

    void FollowMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            Vector3 pos = hit.point;

            if (enableSnapping)
                pos = SnapToGrid(pos);

            ghostObject.transform.position = pos;

            isValidPlacement = CheckCollisionAtPosition(pos);

            SetGhostMaterial(isValidPlacement ? validMaterial : invalidMaterial);
        }
    }

    void HandleRotation()
    {
        if (Input.GetKey(KeyCode.Q))
            currentRotationY -= rotationSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.E))
            currentRotationY += rotationSpeed * Time.deltaTime;

        ghostObject.transform.rotation = Quaternion.Euler(0, currentRotationY, 0);
    }

    bool CheckCollisionAtPosition(Vector3 position)
    {
        foreach (Collider col in ghostObject.GetComponentsInChildren<Collider>())
            col.enabled = true;

        Collider[] hits = Physics.OverlapBox(
            position,
            GetColliderBounds() / 2f,
            ghostObject.transform.rotation,
            furnitureLayer
        );

        foreach (Collider col in ghostObject.GetComponentsInChildren<Collider>())
            col.enabled = false;

        return hits.Length == 0;
    }

    Vector3 GetColliderBounds()
    {
        Collider[] cols = ghostObject.GetComponentsInChildren<Collider>();

        if (cols.Length == 0) return Vector3.one;

        Bounds bounds = cols[0].bounds;

        foreach (Collider c in cols)
            bounds.Encapsulate(c.bounds);

        return bounds.size;
    }

    // ✅🔥 THIS IS YOUR FIXED METHOD
    void PlaceObject()
    {
        if (!isValidPlacement) return;

        GameObject newObj = Instantiate(
            currentItem.prefab3D,
            ghostObject.transform.position,
            ghostObject.transform.rotation
        );

        // Enable colliders
        foreach (Collider col in newObj.GetComponentsInChildren<Collider>())
            col.enabled = true;

        // Set layer
        SetLayerRecursively(newObj, "Furniture");

        // ✅ ADD PREFAB REFERENCE (CRUCIAL FOR SAVE/LOAD)
        FurniturePrefabReference prefabRef = newObj.AddComponent<FurniturePrefabReference>();
        prefabRef.prefabPath = currentItem.name;

        // ✅ REGISTER IN SAVE SYSTEM (THIS WAS YOUR BUG)
        FurnitureSaveManager saveManager = FindObjectOfType<FurnitureSaveManager>();
        if (saveManager != null)
        {
            saveManager.activeFurniture.Add(newObj);
            Debug.Log("Added " + newObj.name + " to activeFurniture list");
        }
        else
        {
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
    }

    void SetGhostMaterial(Material mat)
    {
        foreach (Renderer r in ghostObject.GetComponentsInChildren<Renderer>())
            r.material = mat;
    }

    void SetLayerRecursively(GameObject obj, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);

        obj.layer = layer;

        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layerName);
    }

    Vector3 SnapToGrid(Vector3 pos)
    {
        float x = Mathf.Round(pos.x / gridSize) * gridSize;
        float y = pos.y;
        float z = Mathf.Round(pos.z / gridSize) * gridSize;

        return new Vector3(x, y, z);
    }
}
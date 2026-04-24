using UnityEngine;
using UnityEngine.EventSystems;

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
    public float pickupRange = 10f;
    public float doubleClickDelay = 0.3f;

    private GameObject ghostObject;
    private InventoryItemData currentItem;

    private float currentRotationY;
    private bool isValidPlacement = true;
    private float lastClickTime = -1f;
    private float cachedHalfHeight = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (ghostObject != null)
        {
            FollowMouse();
            HandleRotation();

            if (Input.GetMouseButtonDown(0))
            {
                if (!IsPointerOverUI())
                    TryPlaceObject();
            }

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                CancelPlacement();

            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI()) return;

            float timeSinceLastClick = Time.time - lastClickTime;

            if (timeSinceLastClick <= doubleClickDelay)
            {
                TryPickUpFurniture();
                lastClickTime = -1f;
            }
            else
            {
                lastClickTime = Time.time;
            }
        }
    }

    void TryPickUpFurniture()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, furnitureLayer))
        {
            GameObject target = hit.collider.gameObject;
            FurniturePrefabReference refData = target.GetComponentInParent<FurniturePrefabReference>();

            if (refData != null)
                PickUpFurniture(refData.gameObject);
        }
    }

    public void StartPlacement(InventoryItemData item)
    {
        CancelPlacement();

        currentItem = item;

        // Spawn at origin first to calculate bounds correctly
        ghostObject = Instantiate(item.prefab3D, Vector3.zero, Quaternion.identity);
        ghostObject.SetActive(true);
        currentRotationY = 0f;

        foreach (Collider col in ghostObject.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // Cache the half height at spawn so it never drifts
        cachedHalfHeight = CalculateHalfHeight();

        SetGhostMaterial(validMaterial);
    }

    float CalculateHalfHeight()
    {
        Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return 0f;

        // Use local bounds to avoid world-space errors
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (Renderer r in renderers)
        {
            Bounds b = r.bounds;
            minY = Mathf.Min(minY, b.min.y - ghostObject.transform.position.y);
            maxY = Mathf.Max(maxY, b.max.y - ghostObject.transform.position.y);
        }

        // Half height is how much we need to lift the object so its bottom touches the ground
        return Mathf.Abs(minY);
    }

    void FollowMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            Vector3 pos = hit.point;

            if (enableSnapping)
                pos = SnapToGrid(pos);

            // Use cached half height so the bottom of the object sits exactly on the floor
            pos.y = hit.point.y + cachedHalfHeight;

            ghostObject.transform.position = pos;
            isValidPlacement = CheckCollision(pos);
            SetGhostMaterial(isValidPlacement ? validMaterial : invalidMaterial);
        }
    }

    bool CheckCollision(Vector3 position)
    {
        foreach (Collider col in ghostObject.GetComponentsInChildren<Collider>())
            col.enabled = true;

        Collider[] hits = Physics.OverlapBox(
            position,
            GetBounds() / 2f,
            ghostObject.transform.rotation,
            furnitureLayer
        );

        foreach (Collider col in ghostObject.GetComponentsInChildren<Collider>())
            col.enabled = false;

        return hits.Length == 0;
    }

    Vector3 GetBounds()
    {
        Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return Vector3.one;

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);

        return bounds.size;
    }

    void HandleRotation()
    {
        if (Input.GetKey(KeyCode.Q))
            currentRotationY -= rotationSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.E))
            currentRotationY += rotationSpeed * Time.deltaTime;

        ghostObject.transform.rotation = Quaternion.Euler(0, currentRotationY, 0);
    }

    void TryPlaceObject()
    {
        if (!isValidPlacement) return;

        GameObject newObj = Instantiate(
            currentItem.prefab3D,
            ghostObject.transform.position,
            ghostObject.transform.rotation
        );
        newObj.SetActive(true);

        FurniturePrefabReference prefabRef = newObj.AddComponent<FurniturePrefabReference>();
        prefabRef.prefabPath = currentItem.name;

        FurnitureSaveManager saveManager = FindObjectOfType<FurnitureSaveManager>();
        if (saveManager != null)
            saveManager.activeFurniture.Add(newObj);

        SetLayerRecursively(newObj, "Furniture");
        CancelPlacement();
    }

    public void PickUpFurniture(GameObject obj)
    {
        FurniturePrefabReference refData = obj.GetComponent<FurniturePrefabReference>();
        if (refData == null) return;

        FurnitureSaveManager saveManager = FindObjectOfType<FurnitureSaveManager>();
        if (saveManager != null)
            saveManager.activeFurniture.Remove(obj);

        InventoryManager inv = FindObjectOfType<InventoryManager>();
        if (inv == null) return;

        foreach (var item in inv.items)
        {
            if (item.name == refData.prefabPath)
            {
                Destroy(obj);
                StartPlacement(item);
                return;
            }
        }
    }

    void CancelPlacement()
    {
        if (ghostObject != null)
            Destroy(ghostObject);

        ghostObject = null;
        currentItem = null;
        cachedHalfHeight = 0f;
    }

    void SetGhostMaterial(Material mat)
    {
        if (ghostObject == null) return;
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

    bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
using UnityEngine;
using UnityEngine.EventSystems;

public class WallPlacer_PC : MonoBehaviour
{
    [Header("Camera")]
    public Camera playerCamera;

    [Header("Settings")]
    public float maxDistance = 20f;
    public float gridSize = 0.5f;

    [Header("Preview Distance")]
    public float previewDistance = 4f;
    public float minPreviewDistance = 2f;
    public float maxPreviewDistance = 20f;
    public float scrollSensitivity = 4f;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 6f;

    [Header("Placement Layers")]
    public LayerMask placementMask;   // Ground only
    public LayerMask stackingMask;    // Furniture only

    [Header("Placed Object Layer")]
    public string placedObjectLayerName = "Furniture";

    [Header("Stacking")]
    public float supportRayHeight = 100f;
    public float supportYOffset = 0.01f;
    public float supportInset = 0.05f;

    [Header("Material Wheel")]
    public MaterialWheelController materialWheelController;

    private InventoryItemData currentItem;
    private GameObject currentPrefab;
    private GameObject previewInstance;
    private GameObject placedObject;

    private bool isPlacing = false;
    private bool isEditingExisting = false;

    private float xRotation = 0f;
    private Material[] lastSavedMaterials;

    // Manual height offset for items that allow it (ex: roof)
    private float manualHeightOffset = 0f;

    // Restore data when canceling edit mode
    private InventoryItemData canceledEditItem;
    private Vector3 canceledEditPosition;
    private Quaternion canceledEditRotation;
    private Material[] canceledEditMaterials;

    public void StartPlacement(InventoryItemData item)
    {
        if (item == null || item.prefab3D == null)
        {
            Debug.LogError("WallPlacer_PC: Missing item or prefab!");
            return;
        }

        currentItem = item;
        currentPrefab = item.prefab3D;

        isEditingExisting = false;
        lastSavedMaterials = null;
        manualHeightOffset = 0f;
        ClearCanceledEditData();

        if (previewInstance != null)
            Destroy(previewInstance);

        previewInstance = Instantiate(currentPrefab);
        previewInstance.SetActive(true);
        MakePreviewTransparent(previewInstance);

        isPlacing = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (materialWheelController != null && materialWheelController.IsOpen())
            return;

        HandleMovement();
        HandleMouseLook();

        if (isPlacing && previewInstance != null)
        {
            HandlePreviewInput();
            HandlePreviewMovement();

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (Input.GetMouseButtonDown(0) && previewInstance.activeSelf)
                PlaceObject();

            if (Input.GetKeyDown(KeyCode.Escape))
                CancelPlacement();
        }
        else if (!isPlacing)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (Input.GetMouseButtonDown(0))
                TryEditPlacedObject();
        }
    }

    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        transform.position += move * moveSpeed * Time.deltaTime;
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 100f * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 100f * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        transform.Rotate(Vector3.up * mouseX);
    }

    void HandlePreviewInput()
    {
        if (currentItem == null)
            return;

        // 🖱 Mouse wheel ALWAYS controls distance
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.001f)
        {
            previewDistance += scroll * scrollSensitivity;
            previewDistance = Mathf.Clamp(previewDistance, minPreviewDistance, maxPreviewDistance);
        }

        // ⬆️⬇️ Arrow keys control height ONLY for allowed items (ex: roof)
        if (currentItem.allowManualHeightAdjust)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                manualHeightOffset += currentItem.manualHeightStep * Time.deltaTime * 10f;
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                manualHeightOffset -= currentItem.manualHeightStep * Time.deltaTime * 10f;
            }

            manualHeightOffset = Mathf.Clamp(
                manualHeightOffset,
                currentItem.minManualHeightOffset,
                currentItem.maxManualHeightOffset
            );
        }
    }

    void HandlePreviewMovement()
    {
        if (playerCamera == null || previewInstance == null || currentItem == null)
            return;

        Bounds previewBounds = GetPreviewRenderBounds();
        float halfHeight = previewBounds.size.y * 0.5f;

        Vector3 targetPoint = playerCamera.transform.position + playerCamera.transform.forward * previewDistance;
        Vector3 snappedPoint = SnapToGrid(targetPoint, gridSize);

        Vector3 groundRayOrigin = new Vector3(snappedPoint.x, targetPoint.y + supportRayHeight, snappedPoint.z);

        if (!Physics.Raycast(
                groundRayOrigin,
                Vector3.down,
                out RaycastHit groundHit,
                supportRayHeight * 2f,
                placementMask,
                QueryTriggerInteraction.Ignore))
        {
            previewInstance.SetActive(false);
            return;
        }

        previewInstance.SetActive(true);

        float supportTopY = groundHit.point.y;

        float furnitureTopY;
        bool foundFurnitureSupport = TryGetFurnitureSupportTopY(snappedPoint, previewBounds, out furnitureTopY);

        if (foundFurnitureSupport)
            supportTopY = Mathf.Max(supportTopY, furnitureTopY);

        Vector3 finalPos = new Vector3(
            snappedPoint.x,
            supportTopY + halfHeight + supportYOffset + manualHeightOffset,
            snappedPoint.z
        );

        previewInstance.transform.position = finalPos;
        previewInstance.transform.rotation = Quaternion.identity;
    }

    bool TryGetFurnitureSupportTopY(Vector3 snappedPoint, Bounds previewBounds, out float topY)
    {
        topY = float.NegativeInfinity;
        bool found = false;

        float xExtent = Mathf.Max(previewBounds.extents.x - supportInset, 0.05f);
        float zExtent = Mathf.Max(previewBounds.extents.z - supportInset, 0.05f);

        Vector3[] samplePoints = new Vector3[]
        {
            new Vector3(snappedPoint.x, 0f, snappedPoint.z),
            new Vector3(snappedPoint.x + xExtent, 0f, snappedPoint.z + zExtent),
            new Vector3(snappedPoint.x - xExtent, 0f, snappedPoint.z + zExtent),
            new Vector3(snappedPoint.x + xExtent, 0f, snappedPoint.z - zExtent),
            new Vector3(snappedPoint.x - xExtent, 0f, snappedPoint.z - zExtent),
        };

        for (int i = 0; i < samplePoints.Length; i++)
        {
            Vector3 origin = new Vector3(samplePoints[i].x, supportRayHeight, samplePoints[i].z);

            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                Vector3.down,
                supportRayHeight * 2f,
                stackingMask,
                QueryTriggerInteraction.Ignore
            );

            for (int j = 0; j < hits.Length; j++)
            {
                Collider col = hits[j].collider;
                if (col == null)
                    continue;

                GameObject hitObj = col.transform.root.gameObject;

                if (hitObj == previewInstance)
                    continue;

                if (hitObj == gameObject)
                    continue;

                FurnitureInstance instance = hitObj.GetComponent<FurnitureInstance>();
                if (instance == null || instance.itemData == null)
                    continue;

                topY = Mathf.Max(topY, col.bounds.max.y);
                found = true;
            }
        }

        return found;
    }

    Bounds GetPreviewRenderBounds()
    {
        Renderer[] rends = previewInstance.GetComponentsInChildren<Renderer>();

        if (rends == null || rends.Length == 0)
            return new Bounds(previewInstance.transform.position, Vector3.one);

        Bounds bounds = rends[0].bounds;

        for (int i = 1; i < rends.Length; i++)
            bounds.Encapsulate(rends[i].bounds);

        return bounds;
    }

    void PlaceObject()
    {
        if (previewInstance == null || currentPrefab == null || currentItem == null || !previewInstance.activeSelf)
            return;

        placedObject = Instantiate(
            currentPrefab,
            previewInstance.transform.position,
            previewInstance.transform.rotation
        );

        placedObject.SetActive(true);

        int furnitureLayer = LayerMask.NameToLayer(placedObjectLayerName);
        if (furnitureLayer == -1)
        {
            Debug.LogError($"WallPlacer_PC: Layer '{placedObjectLayerName}' does not exist. Please create it in Unity.");
        }
        else
        {
            SetLayerRecursively(placedObject, furnitureLayer);
        }

        FurnitureInstance instance = placedObject.GetComponent<FurnitureInstance>();
        if (instance == null)
            instance = placedObject.AddComponent<FurnitureInstance>();

        instance.itemData = currentItem;

        if (isEditingExisting)
            ApplySavedMaterials(placedObject);

        Destroy(previewInstance);
        previewInstance = null;

        isPlacing = false;
        isEditingExisting = false;
        manualHeightOffset = 0f;
        ClearCanceledEditData();
    }

    void TryEditPlacedObject()
    {
        if (playerCamera == null)
            return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
            return;

        if (hit.collider == null)
            return;

        GameObject targetObject = hit.collider.transform.root.gameObject;

        if (targetObject == previewInstance)
            return;

        if (targetObject == gameObject)
            return;

        FurnitureInstance instance = targetObject.GetComponent<FurnitureInstance>();
        if (instance == null || instance.itemData == null || instance.itemData.prefab3D == null)
            return;

        Renderer rend = targetObject.GetComponentInChildren<Renderer>();
        if (rend == null)
            return;

        currentItem = instance.itemData;
        currentPrefab = currentItem.prefab3D;

        SaveMaterialsFromObject(targetObject);
        SaveCanceledEditData(instance.itemData, targetObject.transform.position, targetObject.transform.rotation, lastSavedMaterials);

        isEditingExisting = true;
        manualHeightOffset = 0f;

        Vector3 spawnPos = playerCamera.transform.position + playerCamera.transform.forward * previewDistance;

        Destroy(targetObject);

        previewInstance = Instantiate(currentPrefab, spawnPos, Quaternion.identity);
        previewInstance.SetActive(true);

        ApplySavedMaterials(previewInstance);
        MakePreviewTransparent(previewInstance);

        isPlacing = true;
    }

    void SaveMaterialsFromObject(GameObject obj)
    {
        if (obj == null)
        {
            lastSavedMaterials = null;
            return;
        }

        Renderer[] rends = obj.GetComponentsInChildren<Renderer>();
        if (rends == null || rends.Length == 0)
        {
            lastSavedMaterials = null;
            return;
        }

        int totalCount = 0;
        foreach (Renderer r in rends)
            totalCount += r.materials.Length;

        lastSavedMaterials = new Material[totalCount];

        int index = 0;
        foreach (Renderer r in rends)
        {
            Material[] mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                lastSavedMaterials[index] = new Material(mats[i]);
                index++;
            }
        }
    }

    void ApplySavedMaterials(GameObject obj)
    {
        if (obj == null || lastSavedMaterials == null)
            return;

        Renderer[] rends = obj.GetComponentsInChildren<Renderer>();
        if (rends == null || rends.Length == 0)
            return;

        int materialIndex = 0;

        foreach (Renderer rend in rends)
        {
            Material[] mats = rend.materials;

            for (int i = 0; i < mats.Length; i++)
            {
                if (materialIndex >= lastSavedMaterials.Length)
                    break;

                mats[i] = new Material(lastSavedMaterials[materialIndex]);
                materialIndex++;
            }

            rend.materials = mats;

            if (materialIndex >= lastSavedMaterials.Length)
                break;
        }
    }

    void CancelPlacement()
    {
        if (isEditingExisting)
            RestoreCanceledEditObject();

        if (previewInstance != null)
            Destroy(previewInstance);

        previewInstance = null;
        isPlacing = false;
        isEditingExisting = false;
        lastSavedMaterials = null;
        manualHeightOffset = 0f;

        currentItem = null;
        currentPrefab = null;
    }

    void RestoreCanceledEditObject()
    {
        if (canceledEditItem == null || canceledEditItem.prefab3D == null)
            return;

        GameObject restoredObject = Instantiate(
            canceledEditItem.prefab3D,
            canceledEditPosition,
            canceledEditRotation
        );

        restoredObject.SetActive(true);

        int furnitureLayer = LayerMask.NameToLayer(placedObjectLayerName);
        if (furnitureLayer != -1)
            SetLayerRecursively(restoredObject, furnitureLayer);

        FurnitureInstance instance = restoredObject.GetComponent<FurnitureInstance>();
        if (instance == null)
            instance = restoredObject.AddComponent<FurnitureInstance>();

        instance.itemData = canceledEditItem;

        if (canceledEditMaterials != null)
            ApplyMaterialsArrayToObject(restoredObject, canceledEditMaterials);

        ClearCanceledEditData();
    }

    void SaveCanceledEditData(InventoryItemData item, Vector3 position, Quaternion rotation, Material[] materials)
    {
        canceledEditItem = item;
        canceledEditPosition = position;
        canceledEditRotation = rotation;

        if (materials == null)
        {
            canceledEditMaterials = null;
            return;
        }

        canceledEditMaterials = new Material[materials.Length];
        for (int i = 0; i < materials.Length; i++)
        {
            canceledEditMaterials[i] = new Material(materials[i]);
        }
    }

    void ClearCanceledEditData()
    {
        canceledEditItem = null;
        canceledEditPosition = Vector3.zero;
        canceledEditRotation = Quaternion.identity;
        canceledEditMaterials = null;
    }

    void ApplyMaterialsArrayToObject(GameObject obj, Material[] materials)
    {
        if (obj == null || materials == null)
            return;

        Renderer[] rends = obj.GetComponentsInChildren<Renderer>();
        if (rends == null || rends.Length == 0)
            return;

        int materialIndex = 0;

        foreach (Renderer rend in rends)
        {
            Material[] mats = rend.materials;

            for (int i = 0; i < mats.Length; i++)
            {
                if (materialIndex >= materials.Length)
                    break;

                mats[i] = new Material(materials[materialIndex]);
                materialIndex++;
            }

            rend.materials = mats;

            if (materialIndex >= materials.Length)
                break;
        }
    }

    void MakePreviewTransparent(GameObject obj)
    {
        if (obj == null)
            return;

        Renderer[] rends = obj.GetComponentsInChildren<Renderer>();
        Shader transparentShader = Shader.Find("Legacy Shaders/Transparent/Diffuse");

        foreach (Renderer r in rends)
        {
            Material[] mats = r.materials;

            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = new Material(mats[i]);

                if (transparentShader != null)
                    mats[i].shader = transparentShader;

                Color c = mats[i].color;
                c.a = 0.5f;
                mats[i].color = c;
            }

            r.materials = mats;
        }
    }

    Vector3 SnapToGrid(Vector3 pos, float size)
    {
        pos.x = Mathf.Round(pos.x / size) * size;
        pos.z = Mathf.Round(pos.z / size) * size;
        return pos;
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, newLayer);
    }

    void OnDrawGizmosSelected()
    {
        if (previewInstance == null || !previewInstance.activeSelf)
            return;

        Bounds bounds = GetPreviewRenderBounds();
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
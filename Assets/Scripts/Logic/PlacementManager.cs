using UnityEngine;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance;

    [Header("Placement")]
    public LayerMask groundLayer;
    public Material ghostMaterial;
    public InventoryManager inventoryManager;

    [Header("Rotation")]
    public float rotationSpeed = 120f; // degrees per second
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.E;

    private GameObject ghostObject;
    private InventoryItemData currentItem;
    private float yOffset = 0.5f;
    private float currentRotationY = 0f;

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
            CancelPlacement(true);
    }

    public void StartPlacement(InventoryItemData item)
    {
        CancelPlacement(false);

        currentItem = item;
        ghostObject = Instantiate(item.prefab3D);

        currentRotationY = 0f;
        ghostObject.transform.rotation = Quaternion.identity;

        SetGhostMaterial(ghostObject);
    }

    void FollowMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            ghostObject.transform.position = hit.point + Vector3.up * yOffset;
        }
    }

    void HandleRotation()
    {
        if (Input.GetKey(rotateLeftKey))
            currentRotationY -= rotationSpeed * Time.deltaTime;

        if (Input.GetKey(rotateRightKey))
            currentRotationY += rotationSpeed * Time.deltaTime;

        ghostObject.transform.rotation = Quaternion.Euler(0f, currentRotationY, 0f);
    }

    void PlaceObject()
    {
        Instantiate(
            currentItem.prefab3D,
            ghostObject.transform.position,
            ghostObject.transform.rotation
        );

        CancelPlacement(false);
    }

    void CancelPlacement(bool reopenMenu)
    {
        if (ghostObject != null)
            Destroy(ghostObject);

        ghostObject = null;
        currentItem = null;

        if (reopenMenu)
            inventoryManager.ShowMenu();
    }

    void SetGhostMaterial(GameObject obj)
    {
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
            r.material = ghostMaterial;
    }
}

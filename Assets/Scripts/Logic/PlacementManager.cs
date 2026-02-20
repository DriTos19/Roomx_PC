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

    void FollowMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            ghostObject.transform.position = hit.point + Vector3.up * 0.5f;
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

    void PlaceObject()
    {
        Instantiate(currentItem.prefab3D,
            ghostObject.transform.position,
            ghostObject.transform.rotation);

        CancelPlacement();
    }

    void CancelPlacement()
    {
        if (ghostObject != null)
            Destroy(ghostObject);

        ghostObject = null;
        currentItem = null;
    }

    void SetGhostMaterial(GameObject obj)
    {
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
            r.material = ghostMaterial;
    }
}
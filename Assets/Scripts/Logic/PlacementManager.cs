using UnityEngine;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance;

    public LayerMask groundLayer;
    public Material ghostMaterial;

    private GameObject ghostObject;
    private InventoryItemData currentItem;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (ghostObject == null) return;

        FollowMouse();

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

        SetGhostMaterial(ghostObject);
    }

    void FollowMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            ghostObject.transform.position = hit.point;
        }
    }

    void PlaceObject()
    {
        Instantiate(currentItem.prefab3D, ghostObject.transform.position, Quaternion.identity);
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
        foreach (var r in obj.GetComponentsInChildren<Renderer>())
        {
            r.material = ghostMaterial;
        }
    }
}
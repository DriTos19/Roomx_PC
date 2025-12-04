using UnityEngine;

public class WallPlacer : MonoBehaviour
{
    [Header("Placement Settings")]
    public GameObject objectToPlacePrefab;     // The prefab chosen from the inventory
    public LayerMask placementLayer;           // Ground layer

    private GameObject previewObject;
    private bool isPlacing = false;

    void Update()
    {
        if (!isPlacing) return;

        MovePreviewToMouse();

        if (Input.GetMouseButtonDown(0))
            PlaceObject();

        if (Input.GetMouseButtonDown(1))
            CancelPlacement();
    }

    // Called from Inventory UI
    public void BeginPlacement(GameObject prefab)
    {
        objectToPlacePrefab = prefab;

        // Create preview object
        previewObject = Instantiate(objectToPlacePrefab);
        MakePreviewTransparent(previewObject);

        isPlacing = true;
    }

    private void MovePreviewToMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, placementLayer))
        {
            previewObject.transform.position = hit.point;
        }
    }

    private void PlaceObject()
    {
        Instantiate(objectToPlacePrefab, previewObject.transform.position, previewObject.transform.rotation);
        Destroy(previewObject);
        previewObject = null;
        isPlacing = false;
    }

    private void CancelPlacement()
    {
        Destroy(previewObject);
        previewObject = null;
        isPlacing = false;
    }

    private void MakePreviewTransparent(GameObject obj)
    {
        Renderer[] rends = obj.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in rends)
        {
            foreach (Material m in r.materials)
            {
                m.shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
                Color c = m.color;
                c.a = 0.5f;
                m.color = c;
            }
        }
    }
}
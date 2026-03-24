using UnityEngine;
using UnityEngine.EventSystems;

public class WallPlacer_PC : MonoBehaviour
{
    [Header("Prefabs")] 
    public GameObject previewPrefab;
    public GameObject realPrefab;

    [Header("Camera")] 
    public Camera playerCamera;

    [Header("Settings")] 
    public float maxDistance = 10f;
    public float gridSize = 0.5f;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 6f;
   // public Transform cameraHolder;

    [Header("Material Wheel")]
    public MaterialWheelController materialWheelController; // ADDED

    private GameObject previewInstance;
    private GameObject placedObject;
    private bool isPlacing = false;

    private float xRotation = 0f;

    void Start()
    {
        StartPlacement();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Freeze camera and movement if the Material Wheel is open
        if (materialWheelController != null && materialWheelController.IsOpen())
        { return; }
        // Normal camera and movement
        HandleMovement();
        HandleMouseLook();

        if (isPlacing && previewInstance)
        {
            HandlePreviewMovement();

            // Ignore clicks on UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (Input.GetMouseButtonDown(0))
            { PlaceObject(); }

            if (Input.GetKeyDown(KeyCode.Escape))
            { CancelPlacement(); }
        }
        else if (!isPlacing)
        {
            // Ignore clicks on UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (Input.GetMouseButtonDown(0))
            { TryEditPlacedObject(); }
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

     //   cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void StartPlacement()
    {
        if (!previewPrefab || !realPrefab)
        {
            Debug.LogError("Missing prefabs!");
            return;
        }

        if (previewInstance)
            Destroy(previewInstance);

        previewInstance = Instantiate(previewPrefab);
        MakePreviewTransparent(previewInstance);

        isPlacing = true;
    }

    void HandlePreviewMovement()
    {
        float distance = 4f;
        Vector3 pos = playerCamera.transform.position + playerCamera.transform.forward * distance;

        float height = previewInstance.GetComponentInChildren<Renderer>().bounds.size.y;
        pos.y = height / 2f;

        pos = SnapToGrid(pos, gridSize);

        previewInstance.transform.position = pos;
        previewInstance.transform.rotation = Quaternion.identity;
    }

    void PlaceObject()
    {
        if (!previewInstance)
            return;

        placedObject = Instantiate(realPrefab, previewInstance.transform.position, previewInstance.transform.rotation);
        placedObject.tag = "Placeable";

        Destroy(previewInstance);
        previewInstance = null;
        isPlacing = false;
    }

    void TryEditPlacedObject()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            if (hit.collider != null && hit.collider.CompareTag("Placeable"))
            {
                Destroy(hit.collider.gameObject);

                previewInstance = Instantiate(
                    previewPrefab,
                    playerCamera.transform.position + playerCamera.transform.forward * 4f,
                    Quaternion.identity
                );

                MakePreviewTransparent(previewInstance);
                isPlacing = true;
            }
        }
    }

    void CancelPlacement()
    {
        if (previewInstance)
            Destroy(previewInstance);

        isPlacing = false;
        previewInstance = null;
    }

    void MakePreviewTransparent(GameObject obj)
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
            }}}

    Vector3 SnapToGrid(Vector3 pos, float size)
    {
        pos.x = Mathf.Round(pos.x / size) * size;
        pos.z = Mathf.Round(pos.z / size) * size;
        return pos;
    }
}
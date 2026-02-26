using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class ObjectSelector : MonoBehaviour
{
    public MaterialWheelController materialWheelController;

    Camera mainCam;

    void Awake()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        if (materialWheelController == null)
        {
            Debug.LogWarning("MaterialWheelController is not assigned!");
            return;
        }

        if (mainCam == null)
        {
            Debug.LogError("No Main Camera found in the scene. Make sure your camera is tagged 'MainCamera'.");
            return;
        }

        if (Mouse.current == null)
            return;

        // Ignore clicks over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Renderer r = hit.collider.GetComponentInChildren<Renderer>();
                if (r != null)
                {
                    materialWheelController.SelectObject(r);
                }
            }
        }
    }
}
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class ObjectSelector : MonoBehaviour
{
    public MaterialWheelController materialWheelController;

    void Update()
    {
        if (materialWheelController == null) return;

        // Ignore clicks over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
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
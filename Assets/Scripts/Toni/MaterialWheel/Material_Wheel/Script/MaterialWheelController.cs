using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MaterialWheelController : MonoBehaviour
{
    [Header("Object & Wheel")]
    public Renderer currentObject;       
    public RectTransform wheelContainer; 
    public Button slicePrefab;           

    [Header("Materials")]
    public List<Material> materials;     

    private bool isOpen = false;
    private int currentSlot = 0;         
    private Material selectedMaterial;

    void Start()
    {
        if (wheelContainer != null)
            wheelContainer.gameObject.SetActive(false);

        CreateWheel();
    }

    void CreateWheel()
    {
        if (wheelContainer == null || slicePrefab == null || materials == null)
        {
            Debug.LogWarning("WheelContainer, SlicePrefab, or Materials not assigned!");
            return;
        }

        foreach (Transform child in wheelContainer)
            Destroy(child.gameObject);

        int n = materials.Count;
        float radius = 120f;

        for (int i = 0; i < n; i++)
        {
            float angle = i * Mathf.PI * 2 / n;
            Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            Button slice = Instantiate(slicePrefab, wheelContainer);
            slice.GetComponent<RectTransform>().anchoredPosition = pos;

            Material mat = materials[i];

            // Button text
            Text uiText = slice.GetComponentInChildren<Text>();
            if (uiText != null)
            {
                uiText.text = mat.name;
            }
            else
            {
                TMPro.TextMeshProUGUI tmpText = slice.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmpText != null)
                    tmpText.text = mat.name;
            }

            // Preview Image uses material texture/color
            RawImage previewImage = slice.transform.Find("PreviewImage")?.GetComponent<RawImage>();
            if (previewImage != null)
            {
                RectTransform rt = previewImage.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                if (mat.mainTexture != null)
                {
                    previewImage.texture = mat.mainTexture;
                    previewImage.color = Color.white;
                }
                else
                {
                    previewImage.texture = null;
                    previewImage.color = mat.color;
                }
            }
            else
            {
                Debug.LogWarning("No PreviewImage child found on slice prefab!");
            }

            slice.onClick.AddListener(() => ApplyMaterialToSlotWithMaterial(mat));
        }
    }

    public void SelectObject(Renderer newRenderer)
    {
        if (newRenderer == null) return;
        currentObject = newRenderer;
    }

    public void OpenWheel(int slotIndex = 0)
    {
        if (currentObject == null) return;

        currentSlot = slotIndex;
        isOpen = true;

        if (wheelContainer != null)
            wheelContainer.gameObject.SetActive(true);

        wheelContainer.anchoredPosition = Vector2.zero;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseWheel()
    {
        isOpen = false;

        if (wheelContainer != null)
            wheelContainer.gameObject.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public bool IsOpen()
    {
        return isOpen;
    }

    public void ApplyMaterialToSlot(int slotIndex)
    {
        if (currentObject == null || selectedMaterial == null)
            return;

        Material[] mats = currentObject.materials;

        if (slotIndex < 0 || slotIndex >= mats.Length)
            return;

        mats[slotIndex] = selectedMaterial;
        currentObject.materials = mats;

        CloseWheel();
    }

    private void ApplyMaterialToSlotWithMaterial(Material mat)
    {
        selectedMaterial = mat;
        ApplyMaterialToSlot(currentSlot);
    }
}
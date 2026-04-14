using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MaterialWheelController : MonoBehaviour
{
    [Header("Object & Wheel")]
    public Renderer currentObject;
    public RectTransform wheelContainer;
    public RectTransform sliceContainer;
    public Button slicePrefab;

    [Header("Wheel Settings")]
    public int totalButtons = 8;
    public float radius = 120f;

    private bool isOpen = false;
    private int currentSlot = 0;
    private Material selectedBaseMaterial;

    private readonly Dictionary<Renderer, Material[]> originalMaterialsByRenderer = new Dictionary<Renderer, Material[]>();
    private readonly List<Material> materialVariants = new List<Material>();

    void Start()
    {
        if (wheelContainer != null)
            wheelContainer.gameObject.SetActive(false);
    }

    // =========================
    // SELECT OBJECT
    // =========================
    public void SelectObject(Renderer newRenderer)
    {
        if (newRenderer == null)
            return;

        currentObject = newRenderer;

        if (!originalMaterialsByRenderer.ContainsKey(currentObject))
        {
            Material[] source = currentObject.materials;
            Material[] copy = new Material[source.Length];

            for (int i = 0; i < source.Length; i++)
                copy[i] = new Material(source[i]);

            originalMaterialsByRenderer[currentObject] = copy;
        }

        currentSlot = GetBakedMaterialSlot(currentObject);

        Material[] originals = originalMaterialsByRenderer[currentObject];
        if (currentSlot >= 0 && currentSlot < originals.Length)
            selectedBaseMaterial = new Material(originals[currentSlot]);
    }

    void CacheOriginalMaterials(Renderer rend)
    {
        if (rend == null)
            return;

        if (originalMaterialsByRenderer.ContainsKey(rend))
            return;

        Material[] source = rend.materials;
        Material[] copy = new Material[source.Length];

        for (int i = 0; i < source.Length; i++)
            copy[i] = new Material(source[i]);

        originalMaterialsByRenderer[rend] = copy;
    }

    // =========================
    // OPEN WHEEL
    // =========================
    public void OpenWheel(int slotIndex = 0)
    {
        if (currentObject == null)
            return;

        if (!originalMaterialsByRenderer.ContainsKey(currentObject))
            SelectObject(currentObject);

        Material[] originals = originalMaterialsByRenderer[currentObject];
        if (originals == null || originals.Length == 0)
            return;

        // ALWAYS use the baked material slot
        currentSlot = GetBakedMaterialSlot(currentObject);

        if (currentSlot < 0 || currentSlot >= originals.Length)
            currentSlot = 0;

        selectedBaseMaterial = new Material(originals[currentSlot]);

        if (selectedBaseMaterial == null)
            return;

        BuildMaterialVariants();

        if (materialVariants.Count == 0)
            return;

        isOpen = true;

        if (wheelContainer != null)
            wheelContainer.gameObject.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        CreateWheel();
    }

    public void OpenWheelAuto()
    {
        if (currentObject == null)
            return;

        CacheOriginalMaterials(currentObject);

        if (!originalMaterialsByRenderer.TryGetValue(currentObject, out Material[] originals) || originals == null || originals.Length == 0)
            return;

        int bestIndex = FindBestMaterialSlot(originals);
        OpenWheel(bestIndex);
    }

    // =========================
    // CLOSE WHEEL
    // =========================
    public void CloseWheel()
    {
        isOpen = false;

        if (wheelContainer != null)
            wheelContainer.gameObject.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        ClearWheelUI();
    }

    public bool IsOpen()
    {
        return isOpen;
    }

    // =========================
    // BUILD VARIANTS
    // Based on the selected baked/base material
    // =========================
    void BuildMaterialVariants()
    {
        materialVariants.Clear();

        if (selectedBaseMaterial == null)
            return;

        Texture bakedTexture = GetMaterialTexture(selectedBaseMaterial);
        Material sourceMat = new Material(selectedBaseMaterial);

        float[] brightnessLevels = new float[]
        {
            0.55f,
            0.75f,
            1.15f,
            1.35f
        };

        for (int i = 0; i < brightnessLevels.Length; i++)
        {
            Material variant = new Material(sourceMat);

            if (bakedTexture != null)
                ForceKeepMainTexture(variant, bakedTexture);

            ApplyBrightnessToMaterial(variant, brightnessLevels[i]);
            materialVariants.Add(variant);
        }

        Color[] tintColors = new Color[]
        {
            new Color(1f,   0.4f, 0.4f, 1f),
            new Color(0.4f, 0.6f, 1f, 1f),
            new Color(0.4f, 1f,   0.4f, 1f),
            new Color(1f,   0.9f, 0.4f, 1f)
        };

        for (int i = 0; i < tintColors.Length; i++)
        {
            Material variant = new Material(sourceMat);

            if (bakedTexture != null)
                ForceKeepMainTexture(variant, bakedTexture);

            SetMaterialTint(variant, tintColors[i]);
            materialVariants.Add(variant);
        }

        if (materialVariants.Count > totalButtons)
            materialVariants.RemoveRange(totalButtons, materialVariants.Count - totalButtons);
    }

    void ForceKeepMainTexture(Material mat, Texture tex)
    {
        if (mat == null || tex == null)
            return;

        string prop = GetActiveTexturePropertyName(mat);
        if (!string.IsNullOrEmpty(prop))
            mat.SetTexture(prop, tex);
    }

    void ApplyBrightnessToMaterial(Material mat, float multiplier)
    {
        if (mat == null)
            return;

        if (mat.HasProperty("_BaseColor"))
        {
            Color c = mat.GetColor("_BaseColor");
            c.r = Mathf.Clamp01(c.r * multiplier);
            c.g = Mathf.Clamp01(c.g * multiplier);
            c.b = Mathf.Clamp01(c.b * multiplier);
            c.a = 1f;
            mat.SetColor("_BaseColor", c);
        }

        if (mat.HasProperty("_Color"))
        {
            Color c = mat.GetColor("_Color");
            c.r = Mathf.Clamp01(c.r * multiplier);
            c.g = Mathf.Clamp01(c.g * multiplier);
            c.b = Mathf.Clamp01(c.b * multiplier);
            c.a = 1f;
            mat.SetColor("_Color", c);
        }

        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", Color.black);
    }

    void SetMaterialTint(Material mat, Color tint)
    {
        if (mat == null)
            return;

        tint.a = 1f;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", tint);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", tint);

        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", Color.black);
    }

    // =========================
    // CREATE UI WHEEL
    // =========================
    void CreateWheel()
    {
        if (sliceContainer == null || slicePrefab == null || materialVariants.Count == 0)
        {
            Debug.LogWarning("MaterialWheelController: Wheel cannot be created.");
            return;
        }

        ClearWheelUI();

        int n = materialVariants.Count;

        float containerRadius = Mathf.Min(sliceContainer.rect.width, sliceContainer.rect.height) * 0.5f;
        float buttonSize = slicePrefab.GetComponent<RectTransform>().rect.width;
        float safeRadius = containerRadius - (buttonSize * 0.5f) - 10f;
        float finalRadius = Mathf.Min(radius, safeRadius);

        for (int i = 0; i < n; i++)
        {
            float angle = i * Mathf.PI * 2f / n;
            Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * finalRadius;

            Button slice = Instantiate(slicePrefab, sliceContainer);
            slice.GetComponent<RectTransform>().anchoredPosition = pos;

            Material variantMat = materialVariants[i];
            Texture previewTexture = GetMaterialTexture(variantMat);
            Color previewColor = GetButtonPreviewColor(i);

            RawImage preview = slice.transform.Find("PreviewImage")?.GetComponent<RawImage>();
            if (preview != null)
            {
                RectTransform rt = preview.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                preview.texture = previewTexture;
                preview.color = previewColor;
                preview.raycastTarget = false;
            }

            int capturedIndex = i;
            slice.onClick.RemoveAllListeners();
            slice.onClick.AddListener(() => ApplyVariant(capturedIndex));
        }
    }

    Color GetButtonPreviewColor(int index)
    {
        switch (index)
        {
            case 0: return new Color(0.85f, 0.85f, 0.85f, 1f);
            case 1: return new Color(0.85f, 0.85f, 0.85f, 1f);
            case 2: return new Color(0.85f, 0.85f, 0.85f, 1f);
            case 3: return new Color(0.85f, 0.85f, 0.85f, 1f);
            case 4: return new Color(1f,   0.4f, 0.4f, 1f);
            case 5: return new Color(0.4f, 0.6f, 1f, 1f);
            case 6: return new Color(0.4f, 1f,   0.4f, 1f);
            case 7: return new Color(1f,   0.9f, 0.4f, 1f);
        }

        return Color.white;
    }

    void ClearWheelUI()
    {
        foreach (Transform child in sliceContainer)
            Destroy(child.gameObject);
    }

    // =========================
    // MATERIAL SLOT DETECTION
    // Prefer baked material, fallback to first valid one
    // =========================
    int FindBestMaterialSlot(Material[] mats)
    {
        if (mats == null || mats.Length == 0)
            return 0;

        for (int i = 0; i < mats.Length; i++)
        {
            if (HasBakedColorTexture(mats[i]))
                return i;
        }

        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i] != null)
                return i;
        }

        return 0;
    }

    bool HasBakedColorTexture(Material mat)
    {
        if (mat == null)
            return false;

        string prop = GetActiveTexturePropertyName(mat);
        if (string.IsNullOrEmpty(prop))
            return false;

        Texture tex = mat.GetTexture(prop);
        if (tex == null)
            return false;

        return IsBakedColorTextureName(tex.name);
    }

    bool IsBakedColorTextureName(string textureName)
    {
        if (string.IsNullOrEmpty(textureName))
            return false;

        string n = textureName.ToLowerInvariant().Trim();

        return n.EndsWith("_c") || n.Contains("_c_");
    }

    // =========================
    // TEXTURE PROPERTY HELPERS
    // =========================
    string GetActiveTexturePropertyName(Material mat)
    {
        if (mat == null)
            return null;

        string[] possibleProps =
        {
            "_BaseMap",
            "_MainTex",
            "_BaseColorMap",
            "_BaseTexture"
        };

        foreach (string prop in possibleProps)
        {
            if (!mat.HasProperty(prop))
                continue;

            Texture tex = mat.GetTexture(prop);
            if (tex != null)
                return prop;
        }

        return null;
    }

    Texture GetMaterialTexture(Material mat)
    {
        if (mat == null)
            return null;

        string prop = GetActiveTexturePropertyName(mat);
        if (string.IsNullOrEmpty(prop))
            return null;

        return mat.GetTexture(prop);
    }

    // =========================
    // APPLY VARIANT
    // =========================
    void ApplyVariant(int variantIndex)
    {
        if (currentObject == null)
            return;

        if (variantIndex < 0 || variantIndex >= materialVariants.Count)
            return;

        Material[] mats = currentObject.materials;

        currentSlot = GetBakedMaterialSlot(currentObject);

        if (currentSlot < 0 || currentSlot >= mats.Length)
            return;

        mats[currentSlot] = new Material(materialVariants[variantIndex]);
        currentObject.materials = mats;

        CloseWheel();
    }
    
    
    
    int GetBakedMaterialSlot(Renderer rend)
    {
        if (rend == null)
            return 0;

        Material[] mats = rend.materials;
        if (mats == null || mats.Length == 0)
            return 0;

        for (int i = 0; i < mats.Length; i++)
        {
            Material mat = mats[i];
            if (mat == null)
                continue;

            string prop = GetActiveTexturePropertyName(mat);
            if (string.IsNullOrEmpty(prop))
                continue;

            Texture tex = mat.GetTexture(prop);
            if (tex == null)
                continue;

            string texName = tex.name.ToLowerInvariant();

            if (texName.EndsWith("_c") || texName.Contains("_c_"))
                return i;
        }

        return 0;
    }
}
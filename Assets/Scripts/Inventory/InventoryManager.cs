using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup inventoryCanvasGroup; // CanvasGroup on InventoryMenu
    public Transform slotParent;             // InventorySlots (Grid parent)
    public GameObject slotPrefab;            // ItemSlot prefab

    [Header("Description UI")]
    public CanvasGroup descriptionCanvasGroup;
    public Image descriptionImage;
    public TMP_Text descriptionText;

    [Header("Items (data)")]
    public List<InventoryItemData> items = new List<InventoryItemData>();

    private bool menuActive = false;
    private Coroutine fadeCoroutine;

    void Start()
    {
        // Inventory menu starts hidden & non-interactable
        SetMenuState(false);

        // Description starts invisible & empty
        if (descriptionCanvasGroup != null)
        {
            descriptionCanvasGroup.alpha = 0f;
            descriptionCanvasGroup.interactable = false;
           descriptionCanvasGroup.blocksRaycasts = false;
        }

        if (descriptionImage != null) descriptionImage.sprite = null;
        if (descriptionText != null) descriptionText.text = "";

        PopulateSlots();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            menuActive = !menuActive;
            SetMenuState(menuActive);

            if (!menuActive && descriptionCanvasGroup != null)
            {
                descriptionCanvasGroup.alpha = 0f;
                descriptionCanvasGroup.interactable = false;
                descriptionCanvasGroup.blocksRaycasts = false;
            }
        }
    }

    void SetMenuState(bool active)
    {
        if (inventoryCanvasGroup == null) return;

        inventoryCanvasGroup.alpha = active ? 1f : 0f;
        inventoryCanvasGroup.interactable = active;
        inventoryCanvasGroup.blocksRaycasts = active;
    }

    void PopulateSlots()
    {
        if (slotParent == null || slotPrefab == null) return;

        for (int i = slotParent.childCount - 1; i >= 0; i--)
            Destroy(slotParent.GetChild(i).gameObject);

        foreach (var item in items)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotParent);
            ItemSlotUI slotUI = slotObj.GetComponent<ItemSlotUI>();
            if (slotUI != null)
                slotUI.Setup(item, this);
        }
    }

    // Called by ItemSlotUI
    public void ShowItemDetails(InventoryItemData item)
    {
        if (item == null) return;

        descriptionImage.sprite = item.icon;
        descriptionText.text = $"<b>{item.itemName}</b>\n\n{item.description}";

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeCanvasGroup(descriptionCanvasGroup, 0f, 1f, 0.25f));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float t = 0f;
        cg.alpha = from;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        cg.alpha = to;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }
}

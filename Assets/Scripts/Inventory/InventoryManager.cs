using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup inventoryCanvasGroup;
    public Transform slotParent;
    public GameObject slotPrefab;

    [Header("Description UI")]
    public CanvasGroup descriptionCanvasGroup;
    public Image descriptionImage;
    public TMP_Text descriptionText;

    [Header("Items")]
    public List<InventoryItemData> items = new List<InventoryItemData>();

    private bool menuActive;
    private Coroutine fadeCoroutine;

    void Start()
    {
        SetMenuState(false);

        descriptionCanvasGroup.alpha = 0f;
        descriptionCanvasGroup.interactable = false;
        descriptionCanvasGroup.blocksRaycasts = false;

        descriptionImage.sprite = null;
        descriptionText.text = "";

        PopulateSlots();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            menuActive = !menuActive;
            SetMenuState(menuActive);

            if (!menuActive)
                HideDescription();
        }
    }

    void PopulateSlots()
    {
        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        foreach (var item in items)
        {
            GameObject slot = Instantiate(slotPrefab, slotParent);
            slot.GetComponent<ItemSlotUI>().Setup(item, this);
        }
    }

    public void ShowItemDetails(InventoryItemData item)
    {
        descriptionImage.sprite = item.icon;
        descriptionText.text = $"<b>{item.itemName}</b>\n\n{item.description}";

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeCanvasGroup(descriptionCanvasGroup, 0f, 1f, 0.2f));
    }

    public void HideDescription()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        descriptionCanvasGroup.alpha = 0f;
        descriptionCanvasGroup.interactable = false;
        descriptionCanvasGroup.blocksRaycasts = false;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float t = 0f;
        cg.alpha = from;

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

    void SetMenuState(bool active)
    {
        inventoryCanvasGroup.alpha = active ? 1f : 0f;
        inventoryCanvasGroup.interactable = active;
        inventoryCanvasGroup.blocksRaycasts = active;
    }

    public void HideMenu()
    {
        menuActive = false;
        SetMenuState(false);
        HideDescription();
    }

    public void ShowMenu()
    {
        menuActive = true;
        SetMenuState(true);
    }
}

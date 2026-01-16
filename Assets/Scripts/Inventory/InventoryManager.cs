using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory UI")]
    public CanvasGroup inventoryCanvasGroup;
    public float menuFadeDuration = 0.25f;

    [Header("Header UI")]
    public RectTransform headerTransform;
    public CanvasGroup headerCanvasGroup;
    public float headerDropDistance = 80f;
    public float headerDropDuration = 0.25f;

    [Header("Slots")]
    public Transform slotParent;
    public GameObject slotPrefab;

    [Header("Description UI")]
    public CanvasGroup descriptionCanvasGroup;
    public Image descriptionImage;
    public TMP_Text descriptionText;

    [Header("Items")]
    public List<InventoryItemData> items = new List<InventoryItemData>();

    private bool menuActive;
    private Coroutine menuFadeCoroutine;
    private Coroutine headerCoroutine;
    private Vector2 headerStartPos;

    void Start()
    {
        headerStartPos = headerTransform.anchoredPosition;

        inventoryCanvasGroup.alpha = 0f;
        inventoryCanvasGroup.interactable = false;
        inventoryCanvasGroup.blocksRaycasts = false;

        headerCanvasGroup.alpha = 0f;

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

            if (menuActive)
                OpenMenu();
            else
                CloseMenu();
        }
    }

    // ---------- MENU ----------

    void OpenMenu()
    {
        if (menuFadeCoroutine != null) StopCoroutine(menuFadeCoroutine);
        menuFadeCoroutine = StartCoroutine(FadeMenu(0f, 1f, true));

        if (headerCoroutine != null) StopCoroutine(headerCoroutine);
        headerCoroutine = StartCoroutine(DropHeader());
    }

    void CloseMenu()
    {
        if (menuFadeCoroutine != null) StopCoroutine(menuFadeCoroutine);
        menuFadeCoroutine = StartCoroutine(FadeMenu(1f, 0f, false));

        HideDescription();
        ResetHeader();
    }

    IEnumerator FadeMenu(float from, float to, bool interactable)
    {
        float t = 0f;
        inventoryCanvasGroup.alpha = from;

        while (t < menuFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            inventoryCanvasGroup.alpha = Mathf.Lerp(from, to, t / menuFadeDuration);
            yield return null;
        }

        inventoryCanvasGroup.alpha = to;
        inventoryCanvasGroup.interactable = interactable;
        inventoryCanvasGroup.blocksRaycasts = interactable;
    }

    // ---------- HEADER ----------

    IEnumerator DropHeader()
    {
        headerCanvasGroup.alpha = 1f;

        Vector2 start = headerStartPos + Vector2.up * headerDropDistance;
        Vector2 end = headerStartPos;

        headerTransform.anchoredPosition = start;

        float t = 0f;
        while (t < headerDropDuration)
        {
            t += Time.unscaledDeltaTime;
            headerTransform.anchoredPosition = Vector2.Lerp(start, end, t / headerDropDuration);
            yield return null;
        }

        headerTransform.anchoredPosition = end;
    }

    void ResetHeader()
    {
        headerCanvasGroup.alpha = 0f;
        headerTransform.anchoredPosition = headerStartPos + Vector2.up * headerDropDistance;
    }

    // ---------- ITEMS ----------

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

        descriptionCanvasGroup.alpha = 1f;
        descriptionCanvasGroup.interactable = true;
        descriptionCanvasGroup.blocksRaycasts = true;
    }

    public void HideDescription()
    {
        descriptionCanvasGroup.alpha = 0f;
        descriptionCanvasGroup.interactable = false;
        descriptionCanvasGroup.blocksRaycasts = false;
    }

    // ---------- EXTERNAL ----------

    public void HideMenu()
    {
        menuActive = false;
        CloseMenu();
    }

    public void ShowMenu()
    {
        menuActive = true;
        OpenMenu();
    }
}

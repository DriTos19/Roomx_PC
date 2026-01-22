using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
 
public class InventoryManager : MonoBehaviour
{
    [Header("Main UI")]
    public CanvasGroup inventoryCanvasGroup;
    public RectTransform headerPanel;
    public TMP_Text headerText;
 
    [Header("Slots")]
    public Transform slotParent;
    public GameObject slotPrefab;
 
    [Header("Description")]
    public CanvasGroup descriptionCanvasGroup;
    public Image descriptionImage;
    public TMP_Text descriptionText;
 
    [Header("Items")]
    public List<InventoryItemData> items = new List<InventoryItemData>();
 
    [Header("Animation")]
    public float menuFadeDuration = 0.25f;
    public float headerDropDistance = 80f;
 
    private bool menuActive;
    private Coroutine menuFadeCoroutine;
 
    void Start()
    {
        // Menu hidden at start
        inventoryCanvasGroup.alpha = 0f;
        inventoryCanvasGroup.interactable = false;
        inventoryCanvasGroup.blocksRaycasts = false;
 
        // Header hidden above screen
        headerText.gameObject.SetActive(false);
        headerPanel.anchoredPosition += Vector2.up * headerDropDistance;
 
        // Description hidden
        descriptionCanvasGroup.alpha = 0f;
        descriptionCanvasGroup.interactable = false;
        descriptionCanvasGroup.blocksRaycasts = false;
 
        PopulateSlots();
    }
 
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (menuActive)
                CloseMenu();
            else
                OpenMenu();
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
 
    // ---------------- MENU ----------------
 
    public void OpenMenu()
    {
        menuActive = true;
 
        if (menuFadeCoroutine != null)
            StopCoroutine(menuFadeCoroutine);
 
        menuFadeCoroutine = StartCoroutine(FadeMenu(0f, 1f));
        StartCoroutine(DropHeader(true));
    }
 
    public void CloseMenu()
    {
        menuActive = false;
 
        if (menuFadeCoroutine != null)
            StopCoroutine(menuFadeCoroutine);
 
        HideDescription();
        menuFadeCoroutine = StartCoroutine(FadeMenu(1f, 0f));
        StartCoroutine(DropHeader(false));
    }
 
    IEnumerator FadeMenu(float from, float to)
    {
        float t = 0f;
        inventoryCanvasGroup.alpha = from;
 
        inventoryCanvasGroup.interactable = true;
        inventoryCanvasGroup.blocksRaycasts = true;
 
        while (t < menuFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            inventoryCanvasGroup.alpha = Mathf.Lerp(from, to, t / menuFadeDuration);
            yield return null;
        }
 
        inventoryCanvasGroup.alpha = to;
 
        if (to == 0f)
        {
            inventoryCanvasGroup.interactable = false;
            inventoryCanvasGroup.blocksRaycasts = false;
        }
    }
 
    IEnumerator DropHeader(bool show)
    {
        Vector2 startPos = headerPanel.anchoredPosition;
        Vector2 targetPos = show
            ? startPos - Vector2.up * headerDropDistance
            : startPos + Vector2.up * headerDropDistance;
 
        if (show)
            headerText.gameObject.SetActive(true);
 
        float t = 0f;
        while (t < menuFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            headerPanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, t / menuFadeDuration);
            yield return null;
        }
 
        headerPanel.anchoredPosition = targetPos;
 
        if (!show)
            headerText.gameObject.SetActive(false);
    }
 
    // ---------------- DESCRIPTION ----------------
 
    public void ShowItemDetails(InventoryItemData item)
    {
        if (!menuActive) return;
 
        descriptionImage.sprite = item.icon;
        descriptionText.text = $"<b>{item.itemName}</b>\n\n{item.description}";
 
        descriptionCanvasGroup.alpha = 1f;
        descriptionCanvasGroup.interactable = true;
        descriptionCanvasGroup.blocksRaycasts = false;
    }
 
    public void HideDescription()
    {
        descriptionCanvasGroup.alpha = 0f;
        descriptionCanvasGroup.interactable = false;
        descriptionCanvasGroup.blocksRaycasts = false;
    }

    public void ShowMenu()
    {
        throw new System.NotImplementedException();
    }
}
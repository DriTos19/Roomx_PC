using UnityEngine;
using System.Collections;

public class ToggleMenu : MonoBehaviour
{
    [Header("Menu References")]
    public CanvasGroup menuGroup;
    public KeyCode toggleKey = KeyCode.I;
    public float fadeDuration = 0.3f;

    [Header("Cursor")]
    [Tooltip("Unlock the cursor and make it visible while this menu is open.")]
    public bool manageCursor = false;

    private bool isVisible = false;
    private Coroutine fadeRoutine;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ToggleMenuVisibility();
    }

    void ToggleMenuVisibility()
    {
        isVisible = !isVisible;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeMenu(isVisible));

        if (manageCursor)
        {
            Cursor.lockState = isVisible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isVisible;
        }
    }

    IEnumerator FadeMenu(bool show)
    {
        float startAlpha = menuGroup.alpha;
        float endAlpha = show ? 1f : 0f;
        float elapsed = 0f;

        if (show)
        {
            menuGroup.interactable = true;
            menuGroup.blocksRaycasts = true;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            menuGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        menuGroup.alpha = endAlpha;

        if (!show)
        {
            menuGroup.interactable = false;
            menuGroup.blocksRaycasts = false;
        }
    }
}
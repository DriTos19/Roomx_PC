using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels (CanvasGroups)")]
    public CanvasGroup mainMenuPanel;
    public CanvasGroup secondMenuPanel;   // NEW
    public CanvasGroup settingsPanel;
    public CanvasGroup helpPanel;
    public CanvasGroup houseSelectionPanel; // NEW (your loading/house panel)

    [Header("House Selection")]
    public Button[] houseButtons;     // 5 stacked house buttons
    public string[] houseSceneNames;  // Scene names for each house
    public Button nextButton;
    public Button previousButton;

    private int currentHouseIndex = 0;

    [Header("Audio")]
    public AudioSource backgroundMusic;
    public Image muteIcon;
    public Sprite muteSprite;
    public Sprite unmuteSprite;
    public Slider volumeSlider;

    [Header("Transition")]
    public float fadeDuration = 0.4f;

    private bool isMuted = false;
    private CanvasGroup currentPanel;

    void Start()
    {
        currentPanel = mainMenuPanel;

        SetActivePanel(mainMenuPanel, true);
        SetActivePanel(secondMenuPanel, false);
        SetActivePanel(settingsPanel, false);
        SetActivePanel(helpPanel, false);
        SetActivePanel(houseSelectionPanel, false);

        if (volumeSlider != null)
        {
            volumeSlider.value = backgroundMusic.volume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        SetupHouseSelection();
    }

    // =========================
    // PANEL SWITCHING
    // =========================
    

    public void OpenSecondMenu()
    {
        StartCoroutine(SwitchPanel(secondMenuPanel));
    }

    public void OpenHouseSelection()
    {
        StartCoroutine(SwitchPanel(houseSelectionPanel));
    }

    public void OpenSettings()
    {
        StartCoroutine(SwitchPanel(settingsPanel));
    }

    public void OpenHelp()
    {
        StartCoroutine(SwitchPanel(helpPanel));
    }

    public void BackToMainMenu()
    {
        StartCoroutine(SwitchPanel(mainMenuPanel));
    }

    private IEnumerator SwitchPanel(CanvasGroup newPanel)
    {
        if (currentPanel == newPanel)
            yield break;

        yield return StartCoroutine(FadeCanvasGroup(currentPanel, 1f, 0f));
        SetActivePanel(currentPanel, false);

        SetActivePanel(newPanel, true);
        yield return StartCoroutine(FadeCanvasGroup(newPanel, 0f, 1f));

        currentPanel = newPanel;
    }

    // =========================
    // HOUSE SELECTION SYSTEM
    // =========================

    void SetupHouseSelection()
    {
        UpdateHouseButtons();

        nextButton.onClick.AddListener(NextHouse);
        previousButton.onClick.AddListener(PreviousHouse);

        for (int i = 0; i < houseButtons.Length; i++)
        {
            int index = i;
            houseButtons[i].onClick.AddListener(() => LoadHouse(index));
        }
    }

     public void NextHouse()
    {
        currentHouseIndex++;
        if (currentHouseIndex >= houseButtons.Length)
            currentHouseIndex = 0;

        UpdateHouseButtons();
    }

    public void PreviousHouse()
    {
        currentHouseIndex--;
        if (currentHouseIndex < 0)
            currentHouseIndex = houseButtons.Length - 1;

        UpdateHouseButtons();
    }

    void UpdateHouseButtons()
    {
        for (int i = 0; i < houseButtons.Length; i++)
        {
            houseButtons[i].gameObject.SetActive(i == currentHouseIndex);
        }
    }

   public void LoadHouse(int index)
    {
        if (index >= 0 && index < houseSceneNames.Length)
        {
            SceneManager.LoadScene(houseSceneNames[index]);
        }
    }

    // =========================
    // AUDIO
    // =========================

    public void ToggleMute()
    {
        isMuted = !isMuted;
        backgroundMusic.mute = isMuted;

        if (muteIcon != null)
            muteIcon.sprite = isMuted ? muteSprite : unmuteSprite;
    }

    public void SetVolume(float value)
    {
        backgroundMusic.volume = value;
    }

    // =========================
    // FADE SYSTEM
    // =========================

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end)
    {
        float elapsed = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        while (elapsed < fadeDuration)
        {
            cg.alpha = Mathf.Lerp(start, end, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cg.alpha = end;
        cg.interactable = end > 0.9f;
        cg.blocksRaycasts = end > 0.9f;
    }

    private void SetActivePanel(CanvasGroup cg, bool active)
    {
        cg.alpha = active ? 1f : 0f;
        cg.interactable = active;
        cg.blocksRaycasts = active;
    }
}
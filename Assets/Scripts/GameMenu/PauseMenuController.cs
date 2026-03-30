using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private CanvasGroup pauseMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    private bool isPaused = false;

    private void Start()
    {
        // Set up button listeners!
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);

        // Hide menu initially
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.alpha = 0f;
            pauseMenuPanel.interactable = false;
            pauseMenuPanel.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f; // Pause the game
        ShowMenu();
    }

    private void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f; // Resume the game
        HideMenu();
    }

    private void ShowMenu()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.alpha = 1f;
            pauseMenuPanel.interactable = true;
            pauseMenuPanel.blocksRaycasts = true;
        }
    }

    private void HideMenu()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.alpha = 0f;
            pauseMenuPanel.interactable = false;
            pauseMenuPanel.blocksRaycasts = false;
        }
    }

    private void QuitGame()
    {
        Time.timeScale = 1f; // Resume time before quitting
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f; // Resume time before loading scene
        SceneManager.LoadScene("Menu"); // Make sure your main menu scene is named "Menu"
    }
}


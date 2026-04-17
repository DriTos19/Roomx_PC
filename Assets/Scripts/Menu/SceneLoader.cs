using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadMenu()
    {
        // Unfreeze time (important if inventory pauses the game)
        Time.timeScale = 1f;

        SceneManager.LoadScene("Menu");
    }
}
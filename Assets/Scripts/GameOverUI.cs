using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private GameObject gameOverPanel;

    [Header("Hide these on Game Over")]
    [SerializeField]
    private GameObject hudRoot; // drag HUDRoot here

    [Header("Scenes")]
    [SerializeField]
    private string mainMenuSceneName = "MainMenu";

    private bool shown;

    private void Awake()
    {
        if (gameOverPanel)
            gameOverPanel.SetActive(false);
    }

    public void Ping()
    {
        Debug.Log("PING");
    }

    public void ShowGameOver()
    {
        if (shown)
            return;
        shown = true;

        if (hudRoot)
            hudRoot.SetActive(false); // hides all other UI
        if (gameOverPanel)
            gameOverPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
    }

    public void Restart()
    {
        Debug.Log("Restart clicked");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        Debug.Log("MainMenu clicked");
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}

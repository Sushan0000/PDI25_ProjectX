using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private string playSceneName = "Map_v1";

    public void OnPlayButton()
    {
        Debug.Log("Play button clicked");

        // reset anything the menu might have changed
        Time.timeScale = 1f;

        // explicitly unload the menu scene
        SceneManager.LoadScene(playSceneName, LoadSceneMode.Single);
    }

    public void OnExitButton()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}

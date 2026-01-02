using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void ChooseScene(string SceneName)
    {
        SceneManager.LoadScene(SceneName);
    }

    public void QuitApp()
    {
        Application.Quit();
    }
}


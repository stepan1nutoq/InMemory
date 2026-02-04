using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance => _instance;
    
    public int SelectedHeroId { get; set; }
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    // Метод для загрузки сцены с деталями героя
    public void LoadHeroDetailsScene()
    {
        SceneManager.LoadScene("Info");
    }
    
    // Метод для загрузки сцены с уровнем героя
    public void LoadHeroLevelScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Имя сцены не указано!");
        }
    }
    
    // Вернуться в главное меню
    public void LoadMainMenu()
    {
        SceneManager.LoadScene("kozhuhovo");
    }
}
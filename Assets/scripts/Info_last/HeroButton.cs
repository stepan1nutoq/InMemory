using UnityEngine;
using UnityEngine.UI;

public class HeroButton : MonoBehaviour
{
    [SerializeField] private int heroId; // Установите в Inspector для каждой кнопки (1, 2, 3, 4, 5)
    
    private Button button;
    
    private void Start()
    {
        button = GetComponent<Button>();
        
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
        else
        {
            Debug.LogError("Компонент Button не найден на объекте: " + gameObject.name);
        }
    }
    
    private void OnButtonClicked()
    {
        Debug.Log($"Выбран герой с ID: {heroId}");
        
        // Сохраняем выбранный ID в GameManager
        GameManager.Instance.SelectedHeroId = heroId;
        
        // Загружаем сцену с деталями героя
        GameManager.Instance.LoadHeroDetailsScene();
    }
    
    // Метод для установки ID героя (можно вызывать из Inspector)
    public void SetHeroId(int id)
    {
        heroId = id;
    }
}
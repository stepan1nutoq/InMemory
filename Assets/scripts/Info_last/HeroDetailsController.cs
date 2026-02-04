using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class HeroDetailsController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image heroImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text yearsText;
    [SerializeField] private TMP_Text rewardsText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text buttonText;
    
    [Header("Настройки")]
    [SerializeField] private string imagesPath = "Heroes/"; // Папка в Resources
    
    private HeroData currentHero;
    
    private void Start()
    {
        // Получаем ID выбранного героя
        int heroId = GameManager.Instance.SelectedHeroId;
        
        if (heroId <= 0)
        {
            Debug.LogError("ID героя не установлен! Возвращаемся в главное меню.");
            GameManager.Instance.LoadMainMenu();
            return;
        }
        
        // Получаем данные героя из базы данных
        currentHero = DatabaseManager.Instance.GetHeroById(heroId);
        
        if (currentHero == null)
        {
            Debug.LogError($"Не удалось получить данные для героя с ID: {heroId}");
            
            // Показываем сообщение об ошибке
            if (titleText != null)
                titleText.text = "Ошибка загрузки данных";
            
            if (descriptionText != null)
                descriptionText.text = "Не удалось загрузить данные героя. Попробуйте позже.";
            
            // Делаем кнопку неактивной
            if (startButton != null)
                startButton.interactable = false;
                
            return;
        }
        
        // Обновляем UI
        UpdateUI();
        
        // Настраиваем кнопки
        SetupButtons();
    }
    
    private void UpdateUI()
    {
        // Устанавливаем тексты
        if (titleText != null)
            titleText.text = currentHero.title;
        
        if (descriptionText != null)
            descriptionText.text = currentHero.description;
        
        if (yearsText != null)
            yearsText.text = currentHero.years;
        
        if (rewardsText != null)
            rewardsText.text = currentHero.rewards;
        
        if (buttonText != null)
            buttonText.text = "Начать уровень";
        
        // Загружаем и устанавливаем картинку
        LoadAndSetImage();
    }
    
    private void LoadAndSetImage()
    {
        if (heroImage == null)
        {
            Debug.LogWarning("Компонент Image для героя не назначен");
            return;
        }
        
        string fullPath = imagesPath + currentHero.imageName;
        Sprite sprite = Resources.Load<Sprite>(fullPath);
        
        if (sprite != null)
        {
            heroImage.sprite = sprite;
            Debug.Log($"Изображение загружено: {fullPath}");
        }
        else
        {
            Debug.LogWarning($"Не удалось загрузить изображение: {fullPath}");
            
            // Можно установить изображение по умолчанию
            // heroImage.sprite = Resources.Load<Sprite>("Heroes/Default");
        }
    }
    
    private void SetupButtons()
    {
        // Кнопка "Начать уровень"
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartButtonClick);
            
            // Проверяем, разблокирован ли герой
            startButton.interactable = currentHero.is_unlocked;
            
            if (!currentHero.is_unlocked)
            {
                if (buttonText != null)
                    buttonText.text = "В МЕНЮ";
                if (backButton != null)
                {
                    backButton.onClick.RemoveAllListeners();
                    backButton.onClick.AddListener(OnBackButtonClick);
                }
            }
        }       
    }
    
    private void OnStartButtonClick()
    {
        if (currentHero != null && !string.IsNullOrEmpty(currentHero.nextScene))
        {
            Debug.Log($"Загружаем сцену: {currentHero.nextScene}");
            SceneManager.LoadScene(currentHero.nextScene);
        }
        else
        {
            Debug.LogError("Следующая сцена не указана для этого героя!");
        }
    }
    
    private void OnBackButtonClick()
    {
        Debug.Log("Возвращаемся в главное меню");
        GameManager.Instance.LoadMainMenu();
    }
}
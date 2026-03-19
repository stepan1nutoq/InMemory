using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class QuizManager : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        public string questionText;
        public string[] answers; // Массив из 4-х вариантов ответа
        public int correctAnswerIndex; // Индекс правильного ответа (0, 1, 2 или 3)
    }

    [System.Serializable]
    public class AnswerButton
    {
        public Button button; // Сама кнопка
        public TextMeshProUGUI buttonText; // Текст на кнопке
        public GameObject errorIndicator; // Объект, который меняет цвет на красный при ошибке
        public Color originalColor; // Исходный цвет объекта (будет сохранен при старте)
    }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI questionText; // Текст вопроса сверху
    [SerializeField] private AnswerButton[] answerButtons; // Массив из 4-х кнопок с их индикаторами ошибок
    [SerializeField] private TextMeshProUGUI scoreCounterText; // Текст счетчика (например, "0/4")

    [Header("Quiz Settings")]
    [SerializeField] private Color errorColor = Color.red; // Цвет для индикации ошибки
    [SerializeField] private float errorDisplayTime = 0.5f; // Сколько времени показывать красный цвет

    [Header("Completion Settings - Current Scene")]
    [SerializeField] private TextMeshProUGUI completionMessageText; // TextMeshPro для финального сообщения
    [SerializeField] private string completionMessage = "Продолжить";

    [Header("Completion Settings - Other Scene")]
    [SerializeField] private string targetSceneName = "GameScene"; // Имя сцены, где нужно изменить объекты
    [SerializeField] private string targetButtonName = "SpecialButton"; // Имя кнопки для удаления
    [SerializeField] private string targetImageName = "SpecialImage"; // Имя Image для изменения цвета
    [SerializeField] private Color targetImageColor = Color.green; // Новый цвет для Image на другой сцене

    // Приватные переменные
    private List<Question> questions;
    private int currentQuestionIndex = 0;
    private int correctAnswersCount = 0;
    private int totalQuestions = 4;
    private bool isAnswerSelected = false;

    // Для хранения перемешанных индексов ответов
    private int[] shuffledAnswerIndices;

    // Ключи для PlayerPrefs
    private const string QUIZ_COMPLETED_KEY = "QuizCompleted";
    private const string QUIZ_CORRECT_ANSWERS_KEY = "QuizCorrectAnswers";

    void Start()
    {
        // Сохраняем исходные цвета индикаторов
        SaveOriginalColors();

        // Инициализируем вопросы
        InitializeDefaultQuestions();

        totalQuestions = questions.Count;
        ShowQuestion(currentQuestionIndex);
        UpdateScoreCounter();
    }

    // Сохраняем исходные цвета индикаторов
    void SaveOriginalColors()
    {
        foreach (var answerButton in answerButtons)
        {
            if (answerButton.errorIndicator != null)
            {
                // Пытаемся получить компонент Image
                Image img = answerButton.errorIndicator.GetComponent<Image>();
                if (img != null)
                {
                    answerButton.originalColor = img.color;
                    continue;
                }

                // Пытаемся получить компонент TextMeshPro (если это текст)
                TextMeshProUGUI text = answerButton.errorIndicator.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    answerButton.originalColor = text.color;
                    continue;
                }

                // Если ни Image ни Text не найдены, используем белый цвет по умолчанию
                answerButton.originalColor = Color.white;
            }
        }
    }

    // Метод для инициализации вопросов по умолчанию
    void InitializeDefaultQuestions()
    {
        questions = new List<Question>();

        // Вопрос 1
        Question q1 = new Question();
        q1.questionText = "В какой знаменитый авиационный полк была зачислена Татьяна Макарова?";
        q1.answers = new string[4];
        q1.answers[0] = "586-й истребительный авиационный полк.";
        q1.answers[1] = "125-й гвардейский бомбардировочный авиационный полк.";
        q1.answers[2] = "588-й ночной бомбардировочный авиационный полк («ночные ведьмы»).";
        q1.answers[3] = "46-й штурмовой авиационный полк.";
        q1.correctAnswerIndex = 2;
        questions.Add(q1);

        // Вопрос 2
        Question q2 = new Question();
        q2.questionText = "На каком самолёте она совершила все свои боевые вылеты?";
        q2.answers = new string[4];
        q2.answers[0] = "Ил-2 («летающий танк»).";
        q2.answers[1] = "Пе-2 (пикирующий бомбардировщик).";
        q2.answers[2] = "По-2 (У-2) (лёгкий ночной бомбардировщик-биплан).";
        q2.answers[3] = "Як-1 (истребитель).";
        q2.correctAnswerIndex = 2;
        questions.Add(q2);

        // Вопрос 3
        Question q3 = new Question();
        q3.questionText = "Кто был постоянным штурманом в экипаже Татьяны Макаровой?";
        q3.answers = new string[4];
        q3.answers[0] = "Марина Раскова.";
        q3.answers[1] = "Полина Гельман.";
        q3.answers[2] = "Вера Белик.";
        q3.answers[3] = "Руфина Гашева.";
        q3.correctAnswerIndex = 2;
        questions.Add(q3);

        // Вопрос 4
        Question q4 = new Question();
        q4.questionText = "В каком городе похоронены Татьяна Макарова и её штурман Вера Белик?";
        q4.answers = new string[4];
        q4.answers[0] = "Варшава, Польша.";
        q4.answers[1] = "Смоленск, Россия.";
        q4.answers[2] = "Остроленка, Польша.";
        q4.answers[3] = "Волгоград, Россия.";
        q4.correctAnswerIndex = 2;
        questions.Add(q4);
    }

    // Метод для отображения вопроса по индексу
    void ShowQuestion(int index)
    {
        if (index < 0 || index >= questions.Count)
        {
            Debug.LogError("Индекс вопроса вне диапазона");
            return;
        }

        Question currentQuestion = questions[index];

        // Обновляем текст вопроса
        if (questionText != null)
            questionText.text = currentQuestion.questionText;

        // Перемешиваем порядок ответов
        ShuffleAnswers(currentQuestion);

        // Обновляем тексты на кнопках в новом порядке
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < currentQuestion.answers.Length)
            {
                if (answerButtons[i].buttonText != null)
                    answerButtons[i].buttonText.text = currentQuestion.answers[shuffledAnswerIndices[i]];

                // Сбрасываем цвет индикатора ошибки к исходному
                ResetErrorIndicator(i);
            }
        }

        // Разблокируем кнопки для нового вопроса
        isAnswerSelected = false;
        EnableAllButtons(true);
    }

    // Метод для перемешивания ответов
    void ShuffleAnswers(Question question)
    {
        // Создаем массив индексов [0, 1, 2, 3]
        shuffledAnswerIndices = new int[] { 0, 1, 2, 3 };

        // Перемешиваем массив индексов (алгоритм Фишера-Йетса)
        System.Random rng = new System.Random();
        int n = shuffledAnswerIndices.Length;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            int value = shuffledAnswerIndices[k];
            shuffledAnswerIndices[k] = shuffledAnswerIndices[n];
            shuffledAnswerIndices[n] = value;
        }
    }

    // Получить оригинальный индекс ответа до перемешивания
    int GetOriginalAnswerIndex(int shuffledIndex)
    {
        if (shuffledAnswerIndices != null && shuffledIndex >= 0 && shuffledIndex < shuffledAnswerIndices.Length)
        {
            return shuffledAnswerIndices[shuffledIndex];
        }
        return shuffledIndex;
    }

    // Сброс цвета индикатора ошибки к исходному
    void ResetErrorIndicator(int buttonIndex)
    {
        if (answerButtons[buttonIndex].errorIndicator != null)
        {
            SetIndicatorColor(buttonIndex, answerButtons[buttonIndex].originalColor);
        }
    }

    // Установка цвета индикатора
    void SetIndicatorColor(int buttonIndex, Color color)
    {
        if (answerButtons[buttonIndex].errorIndicator != null)
        {
            // Пытаемся получить компонент Image
            Image img = answerButtons[buttonIndex].errorIndicator.GetComponent<Image>();
            if (img != null)
            {
                img.color = color;
                return;
            }

            // Пытаемся получить компонент TextMeshPro (если это текст)
            TextMeshProUGUI text = answerButtons[buttonIndex].errorIndicator.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.color = color;
                return;
            }
        }
    }

    // Включить/выключить все кнопки
    void EnableAllButtons(bool enable)
    {
        foreach (var answerButton in answerButtons)
        {
            if (answerButton.button != null)
            {
                answerButton.button.interactable = enable;
            }
        }
    }

    // Этот метод вызывается при нажатии на кнопку с ответом
    public void OnAnswerButtonClicked(int buttonIndex)
    {
        // Если уже выбран ответ или викторина закончена - игнорируем нажатие
        if (isAnswerSelected || currentQuestionIndex >= questions.Count)
            return;

        Question currentQuestion = questions[currentQuestionIndex];

        // Получаем оригинальный индекс ответа (до перемешивания)
        int originalAnswerIndex = GetOriginalAnswerIndex(buttonIndex);

        // Проверяем правильность ответа
        if (originalAnswerIndex == currentQuestion.correctAnswerIndex)
        {
            // Правильный ответ
            correctAnswersCount++;
            Debug.Log("Правильно!");

            // Обновляем счетчик
            UpdateScoreCounter();

            // Блокируем кнопки, чтобы нельзя было нажать снова
            isAnswerSelected = true;
            EnableAllButtons(false);

            // Переходим к следующему вопросу
            currentQuestionIndex++;

            // Проверяем, есть ли еще вопросы
            if (currentQuestionIndex < questions.Count)
            {
                // Небольшая задержка перед показом следующего вопроса для лучшего UX
                Invoke(nameof(ShowNextQuestion), 0.5f);
            }
            else
            {
                // Викторина окончена
                Invoke(nameof(EndQuiz), 0.5f);
            }
        }
        else
        {
            // Показываем красный индикатор для этой кнопки
            SetIndicatorColor(buttonIndex, errorColor);

            // Возвращаем исходный цвет через указанное время
            Invoke(nameof(ResetAllErrorIndicators), errorDisplayTime);

            // НЕ переходим к следующему вопросу
            // НЕ увеличиваем счетчик
        }
    }

    // Сброс всех индикаторов ошибок к исходным цветам
    void ResetAllErrorIndicators()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            ResetErrorIndicator(i);
        }
    }

    // Показать следующий вопрос
    void ShowNextQuestion()
    {
        ShowQuestion(currentQuestionIndex);
    }

    // Обновление текста счетчика (формат n/4)
    void UpdateScoreCounter()
    {
        if (scoreCounterText != null)
        {
            scoreCounterText.text = $"{correctAnswersCount}/{totalQuestions}";
        }
    }

    // Действия при завершении викторины
    void EndQuiz()
    {

        // Сохраняем результат в PlayerPrefs
        PlayerPrefs.SetInt(QUIZ_CORRECT_ANSWERS_KEY, correctAnswersCount);

        // Проверяем, ответил ли игрок правильно на все вопросы
        if (correctAnswersCount == totalQuestions)
        {

            // Сохраняем флаг о завершении викторины
            PlayerPrefs.SetInt(QUIZ_COMPLETED_KEY, 1);
            PlayerPrefs.Save();

            if (completionMessageText != null)
            {
                completionMessageText.text = completionMessage;
            }
            // Показываем сообщение о разблокировке
            if (questionText != null)
            {
                questionText.text = $"Поздравляем! Вы ответили правильно на все вопросы! Обновляем карту...";
            }
        }
        else
        {
            // Если ответили не на все вопросы правильно
            if (questionText != null)
            {
                questionText.text = $"Викторина завершена! Правильных ответов: {correctAnswersCount} из {totalQuestions}\nПопробуйте еще раз!";
            }
        }

        // Блокируем кнопки
        EnableAllButtons(false);
    }

    // Метод для сброса прогресса викторины (можно привязать к кнопке)
    public void ResetQuizProgress()
    {
        PlayerPrefs.DeleteKey(QUIZ_COMPLETED_KEY);
        PlayerPrefs.DeleteKey(QUIZ_CORRECT_ANSWERS_KEY);
        PlayerPrefs.Save();

        Debug.Log("Прогресс викторины сброшен");

        // Перезапускаем текущую сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
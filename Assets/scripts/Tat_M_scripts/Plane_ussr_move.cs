using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RectTransform))]
public class ImageControllerWithInputActions : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 500f;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float deceleration = 8f;
    
    [Header("Screen Boundaries")]
    [SerializeField] private bool clampToScreen = true;
    [SerializeField] private float padding = 50f; // Отступ от краев
    
    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;
    
    // Ссылка на Input Action
    private InputAction moveAction;
    private RectTransform rectTransform;
    private Canvas canvas;
    private float currentVelocity = 0f;
    private float targetPositionX;

    [System.Obsolete]
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        
        if (playerInput == null)
        {
            playerInput = FindObjectOfType<PlayerInput>();
            if (playerInput == null)
            {
                Debug.LogError("PlayerInput component not found! Add it to a GameObject in the scene.");
                return;
            }
        }
    }
    
    void Start()
    {
        // Получаем Input Action
        moveAction = playerInput.actions["MoveHorizontal"];
        
        if (moveAction == null)
        {
            Debug.LogError("MoveHorizontal action not found! Check your Input Actions asset.");
            return;
        }
        
        // Включаем action
        moveAction.Enable();
        
        // Инициализация позиции
        targetPositionX = rectTransform.anchoredPosition.x;
    }
    
    void Update()
    {
        if (moveAction == null) return;
        
        HandleInput();
        SmoothMovement();
        if (clampToScreen) ClampToScreen();
    }
    
    void HandleInput()
    {
        // Читаем значение ввода (от -1 до 1)
        float inputValue = moveAction.ReadValue<float>();
        
        // Плавное изменение скорости
        if (inputValue != 0)
        {
            currentVelocity = Mathf.MoveTowards(currentVelocity, 
                inputValue * moveSpeed, 
                acceleration * moveSpeed * Time.deltaTime);
        }
        else
        {
            currentVelocity = Mathf.MoveTowards(currentVelocity, 0f, 
                deceleration * moveSpeed * Time.deltaTime);
        }
        
        // Обновление целевой позиции
        targetPositionX += currentVelocity * Time.deltaTime;
    }
    
    void SmoothMovement()
    {
        Vector2 currentPos = rectTransform.anchoredPosition;
        Vector2 targetPos = new Vector2(targetPositionX, currentPos.y);
        
        // Плавное движение
        rectTransform.anchoredPosition = Vector2.Lerp(currentPos, targetPos, 10f * Time.deltaTime);
    }
    
    void ClampToScreen()
    {
        if (canvas == null) return;
        
        // Рассчитываем границы экрана с учетом размера Image
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        float canvasWidth = canvasRect.rect.width;
        float imageWidth = rectTransform.rect.width;
        
        float minX = -canvasWidth / 2 + imageWidth / 2 + padding;
        float maxX = canvasWidth / 2 - imageWidth / 2 - padding;
        
        // Ограничиваем позицию
        targetPositionX = Mathf.Clamp(targetPositionX, minX, maxX);
        
        Vector2 currentPos = rectTransform.anchoredPosition;
        currentPos.x = Mathf.Clamp(currentPos.x, minX, maxX);
        rectTransform.anchoredPosition = currentPos;
    }
    
    void OnEnable()
    {
        if (moveAction != null) moveAction.Enable();
    }
    
    void OnDisable()
    {
        if (moveAction != null) moveAction.Disable();
    }
    
    // Метод для визуализации границ в редакторе
    void OnDrawGizmosSelected()
    {
        if (!clampToScreen || canvas == null) return;
        
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        float canvasWidth = canvasRect.rect.width;
        float imageWidth = GetComponent<RectTransform>().rect.width;
        
        float minX = -canvasWidth / 2 + imageWidth / 2 + padding;
        float maxX = canvasWidth / 2 - imageWidth / 2 - padding;
        
        Vector3 center = canvas.transform.position;
        Vector3 leftBound = center + Vector3.left * (canvasWidth / 2 - padding);
        Vector3 rightBound = center + Vector3.right * (canvasWidth / 2 - padding);
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(leftBound + Vector3.up * 100, leftBound + Vector3.down * 100);
        Gizmos.DrawLine(rightBound + Vector3.up * 100, rightBound + Vector3.down * 100);
    }
}
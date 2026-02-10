using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SpawnerScript : MonoBehaviour
{
    [Header("Префаб для спавна")]
    public GameObject prefabToSpawn;

    [Header("Настройки")]
    public float fallSpeed = 100f; // Увеличил для UI
    public float destroyYPosition = -500f; // Для Canvas
    public float spriteChangeDuration = 2f;

    [Header("Новый спрайт")]
    public Sprite temporarySprite;

    [Header("Счетчик")]
    public int destroyedObjectsCount = 0;
    public Text counterText;

    [Header("Input Actions")]
    public InputAction spawnAction = new InputAction("Spawn", binding: "<Keyboard>/space");

    private GameObject currentFallingObject;
    private bool isFalling = false;
    private Coroutine fallingCoroutine;

    void OnEnable()
    {
        spawnAction.Enable();
        spawnAction.performed += OnSpawnPerformed;
    }

    void OnDisable()
    {
        spawnAction.performed -= OnSpawnPerformed;
        spawnAction.Disable();
    }

    void Start()
    {
        UpdateCounterUI();
    }

    void OnSpawnPerformed(InputAction.CallbackContext context)
    {
        if (!isFalling)
        {
            SpawnAndStartFall();
        }
    }

    void SpawnAndStartFall()
    {
        // Получаем RectTransform текущего объекта
        RectTransform thisRect = GetComponent<RectTransform>();
        if (thisRect == null)
        {
            Debug.LogError("Объект должен иметь RectTransform для UI!");
            return;
        }

        // Позиция спавна - под текущим объектом
        Vector3 spawnPosition = thisRect.position;
        
        // Вычисляем отступ вниз (высота текущего объекта)
        float offsetY = thisRect.rect.height * thisRect.localScale.y;
        spawnPosition.y -= offsetY;

        // Спавним префаб
        currentFallingObject = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        
        // Устанавливаем родительский Canvas
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            currentFallingObject.transform.SetParent(parentCanvas.transform, false);
            
            // Важно: сохраняем позицию в локальных координатах Canvas
            RectTransform spawnedRect = currentFallingObject.GetComponent<RectTransform>();
            if (spawnedRect != null)
            {
                // Конвертируем мировую позицию в локальную позицию Canvas
                Vector2 localPosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.transform as RectTransform,
                    spawnPosition,
                    parentCanvas.worldCamera,
                    out localPosition);
                
                spawnedRect.localPosition = localPosition;
            }
        }
        else
        {
            Debug.LogWarning("Canvas не найден!");
        }

        // Начинаем падение в корутине
        isFalling = true;
        if (fallingCoroutine != null)
        {
            StopCoroutine(fallingCoroutine);
        }
        fallingCoroutine = StartCoroutine(FallingRoutine());
    }

    IEnumerator FallingRoutine()
    {
        RectTransform fallingRect = currentFallingObject.GetComponent<RectTransform>();
        if (fallingRect == null)
        {
            Debug.LogError("Падающий объект должен иметь RectTransform!");
            yield break;
        }

        while (currentFallingObject != null && 
               fallingRect.localPosition.y > destroyYPosition)
        {
            // Двигаем объект вниз (в локальных координатах)
            fallingRect.localPosition += Vector3.down * fallSpeed * Time.deltaTime;

            // Проверяем пересечение
            GameObject collidedObject = CheckCollision(fallingRect);
            if (collidedObject != null)
            {
                // Обрабатываем столкновение
                yield return StartCoroutine(HandleCollision(collidedObject));
                yield break; // Завершаем корутину
            }

            yield return null;
        }

        // Если достигли нижней границы
        if (currentFallingObject != null)
        {
            DestroyFallingObject();
        }
    }

    GameObject CheckCollision(RectTransform fallingRect)
    {
        if (currentFallingObject == null || fallingRect == null) return null;

        // Ищем Canvas
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) return null;

        // Получаем Rect падающего объекта
        Rect fallingRectWorld = GetWorldRect(fallingRect);

        foreach (Transform child in parentCanvas.transform)
        {
            // Исключаем сам падающий объект и объект-спавнер
            if (child.gameObject == currentFallingObject || 
                child.gameObject == this.gameObject)
                continue;

            RectTransform targetRect = child.GetComponent<RectTransform>();
            if (targetRect != null)
            {
                Rect targetRectWorld = GetWorldRect(targetRect);
                
                // Проверяем пересечение
                if (fallingRectWorld.Overlaps(targetRectWorld))
                {
                    return child.gameObject;
                }
            }
        }

        return null;
    }

    Rect GetWorldRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        
        Vector2 min = corners[0];
        Vector2 max = corners[2];
        
        return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
    }

    IEnumerator HandleCollision(GameObject targetObject)
    {
        // Сохраняем ссылку на уничтожаемый объект
        GameObject objectToDestroy = targetObject;
        
        // Уничтожаем падающий объект
        DestroyFallingObject();

        // Меняем спрайт у целевого объекта
        SpriteRenderer targetSpriteRenderer = objectToDestroy.GetComponent<SpriteRenderer>();
        Image targetImage = objectToDestroy.GetComponent<Image>();
        
        Sprite originalSprite = null;
        
        if (targetSpriteRenderer != null)
        {
            originalSprite = targetSpriteRenderer.sprite;
            targetSpriteRenderer.sprite = temporarySprite;
        }
        else if (targetImage != null)
        {
            originalSprite = targetImage.sprite;
            targetImage.sprite = temporarySprite;
        }

        // Ждем указанное время
        yield return new WaitForSeconds(spriteChangeDuration);

        // Восстанавливаем оригинальный спрайт
        if (targetSpriteRenderer != null && originalSprite != null)
        {
            targetSpriteRenderer.sprite = originalSprite;
        }
        else if (targetImage != null && originalSprite != null)
        {
            targetImage.sprite = originalSprite;
        }

        // Уничтожаем целевой объект
        if (objectToDestroy != null)
        {
            Destroy(objectToDestroy);
        }

        // Увеличиваем счетчик
        destroyedObjectsCount++;
        UpdateCounterUI();
    }

    void DestroyFallingObject()
    {
        if (currentFallingObject != null)
        {
            Destroy(currentFallingObject);
            currentFallingObject = null;
        }
        isFalling = false;
        
        if (fallingCoroutine != null)
        {
            StopCoroutine(fallingCoroutine);
            fallingCoroutine = null;
        }
    }

    void UpdateCounterUI()
    {
        if (counterText != null)
        {
            counterText.text = $"Уничтожено: {destroyedObjectsCount}";
        }
    }

    public void StartSpawning()
    {
        if (!isFalling)
        {
            SpawnAndStartFall();
        }
    }

    // Для отладки: рисуем Rect в редакторе
    void OnDrawGizmos()
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            Gizmos.color = Color.green;
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            
            // Рисуем прямоугольник
            Gizmos.DrawLine(corners[0], corners[1]);
            Gizmos.DrawLine(corners[1], corners[2]);
            Gizmos.DrawLine(corners[2], corners[3]);
            Gizmos.DrawLine(corners[3], corners[0]);
            
            // Показываем точку спавна
            Vector3 spawnPoint = corners[0];
            spawnPoint.y -= rect.rect.height * rect.localScale.y;
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(spawnPoint, 5f);
        }
    }
}
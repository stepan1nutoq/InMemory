using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SpawnerScript : MonoBehaviour
{
    [Header("Префаб для спавна")]
    public GameObject prefabToSpawn;

    [Header("Настройки")]
    public float fallSpeed = 5f;
    public float destroyYPosition = -10f;
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
        // Вычисляем позицию спавна (под текущим объектом)
        Vector3 spawnPosition = transform.position;
        
        // Для UI объектов используем RectTransform
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            spawnPosition.y -= rectTransform.rect.height * rectTransform.localScale.y;
        }
        else
        {
            // Для обычных GameObject используем масштаб и коллайдер
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                spawnPosition.y -= collider.bounds.extents.y;
            }
            else
            {
                spawnPosition.y -= 1f; // Значение по умолчанию
            }
        }

        // Спавним префаб
        currentFallingObject = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        
        // Устанавливаем родительский Canvas если есть
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            currentFallingObject.transform.SetParent(parentCanvas.transform, false);
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
        while (currentFallingObject != null && 
               currentFallingObject.transform.position.y > destroyYPosition)
        {
            // Двигаем объект вниз
            currentFallingObject.transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            // Проверяем пересечение
            GameObject collidedObject = CheckCollision();
            if (collidedObject != null)
            {
                // Обрабатываем столкновение
                yield return HandleCollision(collidedObject);
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

    GameObject CheckCollision()
    {
        if (currentFallingObject == null) return null;

        // Проверка для UI объектов
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            RectTransform fallingRect = currentFallingObject.GetComponent<RectTransform>();
            if (fallingRect == null) return null;

            foreach (Transform child in parentCanvas.transform)
            {
                // Исключаем сам падающий объект и объект-спавнер
                if (child.gameObject == currentFallingObject || 
                    child.gameObject == this.gameObject) // Убрали проверку тега
                    continue;

                RectTransform targetRect = child.GetComponent<RectTransform>();
                if (targetRect != null && RectTransformOverlap(fallingRect, targetRect))
                {
                    return child.gameObject;
                }
            }
        }
        else
        {
            // Проверка для обычных GameObject
            Collider2D fallingCollider = currentFallingObject.GetComponent<Collider2D>();
            if (fallingCollider == null) return null;

            // Ищем все объекты с определенным тегом или слоем
            Collider2D[] colliders = Physics2D.OverlapBoxAll(
                fallingCollider.bounds.center, 
                fallingCollider.bounds.size, 
                0f);

            foreach (Collider2D collider in colliders)
            {
                if (collider.gameObject == currentFallingObject || 
                    collider.gameObject == this.gameObject)
                    continue;

                return collider.gameObject;
            }
        }

        return null;
    }

    bool RectTransformOverlap(RectTransform rect1, RectTransform rect2)
    {
        Rect rect1World = GetWorldRect(rect1);
        Rect rect2World = GetWorldRect(rect2);
        
        return rect1World.Overlaps(rect2World);
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
}
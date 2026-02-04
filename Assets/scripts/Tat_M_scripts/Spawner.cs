using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RandomSpawnerUI : MonoBehaviour
{
    [System.Serializable]
    public class SpawnableObjectData
    {
        public GameObject prefab;
        public float targetYPosition = 0f;
        [Tooltip("Ширина объекта в пикселях. Рекомендуется задать вручную")]
        public float objectWidth = 200f;
    }

    [Header("UI Объекты для спавна")]
    [SerializeField] private SpawnableObjectData[] objectsToSpawn;

    [Header("Родительский Canvas")]
    [SerializeField] private RectTransform parentCanvas;

    [Header("Настройки позиции")]
    [SerializeField] private float spawnStartY = -200f;
    [SerializeField] private float minX = -500f;
    [SerializeField] private float maxX = 500f;

    [Header("Настройки спавна")]
    [SerializeField] private int maxObjectsOnScreen = 3;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private int spawnCountOnStart = 1;
    [SerializeField] private float autoSpawnInterval = 2f;

    [Header("Настройки анимации")]
    [SerializeField] private float riseDuration = 1f;
    [SerializeField] private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private List<RectTransform> spawnedObjects = new List<RectTransform>();
    private Coroutine autoSpawnCoroutine;

    private void Start()
    {
        if (spawnOnStart)
        {
            StartCoroutine(SpawnMultipleOnStart());
        }

        if (autoSpawnInterval > 0)
        {
            StartAutoSpawning();
        }
    }

    private void OnDestroy()
    {
        StopAutoSpawning();
    }

    private IEnumerator SpawnMultipleOnStart()
    {
        for (int i = 0; i < spawnCountOnStart; i++)
        {
            if (TrySpawnObject())
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private bool TrySpawnObject()
    {
        // 1. Проверка лимита объектов
        if (spawnedObjects.Count >= maxObjectsOnScreen)
        {
            Debug.Log($"Не спавним: достигнут лимит {maxObjectsOnScreen} объектов");
            return false;
        }

        // 2. Проверка наличия объектов
        if (objectsToSpawn == null || objectsToSpawn.Length == 0)
        {
            Debug.LogError("Нет объектов для спавна!");
            return false;
        }

        // 3. Выбираем случайный объект
        int randomIndex = Random.Range(0, objectsToSpawn.Length);
        SpawnableObjectData objectData = objectsToSpawn[randomIndex];

        if (objectData.prefab == null)
        {
            Debug.LogError($"Объект с индексом {randomIndex} не назначен!");
            return false;
        }

        // 4. Получаем ширину объекта
        float objectWidth = GetObjectWidth(objectData);
        
        // 5. Ищем свободное место с учетом ВСЕХ объектов
        if (!FindFreePositionForAllObjects(objectWidth, out float spawnX))
        {
            Debug.Log("Не спавним: нет свободного места с учетом всех объектов");
            return false;
        }

        // 6. Спавним объект
        StartCoroutine(SpawnObjectWithAnimation(objectData, spawnX, objectWidth));
        return true;
    }

    private float GetObjectWidth(SpawnableObjectData objectData)
    {
        // Используем заданную ширину или значение по умолчанию
        return objectData.objectWidth > 0 ? objectData.objectWidth : 200f;
    }

    /// <summary>
    /// Ищет свободную позицию с учетом ВСЕХ существующих объектов
    /// </summary>
    private bool FindFreePositionForAllObjects(float objectWidth, out float spawnX)
    {
        spawnX = 0f;

        // Если объектов нет, выбираем случайную позицию
        if (spawnedObjects.Count == 0)
        {
            spawnX = GetRandomPositionWithMargin(objectWidth);
            Debug.Log($"Первый объект: X={spawnX}, Ширина={objectWidth}");
            return true;
        }

        // Собираем информацию о всех существующих объектах
        List<ObjectPositionInfo> existingObjects = new List<ObjectPositionInfo>();
        
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            if (spawnedObjects[i] != null)
            {
                float posX = spawnedObjects[i].anchoredPosition.x;
                float width = GetWidthForSpawnedObject(i);
                existingObjects.Add(new ObjectPositionInfo(posX, width));
            }
        }

        Debug.Log($"Проверяем свободное место. Существует объектов: {existingObjects.Count}");

        // Пробуем найти свободную позицию (до 100 попыток)
        for (int attempt = 0; attempt < 100; attempt++)
        {
            float candidateX = GetRandomPositionWithMargin(objectWidth);
            bool isPositionValid = true;

            // Проверяем кандидата против ВСЕХ существующих объектов
            foreach (var existingObj in existingObjects)
            {
                // Рассчитываем расстояние между центрами
                float distanceBetweenCenters = Mathf.Abs(candidateX - existingObj.positionX);
                
                // Рассчитываем минимально необходимое расстояние
                // (половина ширины нового объекта + половина ширины существующего объекта)
                float minRequiredDistance = (objectWidth / 2) + (existingObj.width / 2);
                
                // Добавляем небольшой зазор (20% от средней ширины)
                float gap = (objectWidth + existingObj.width) * 0.1f;
                minRequiredDistance += gap;

                if (distanceBetweenCenters < minRequiredDistance)
                {
                    isPositionValid = false;
                    Debug.Log($"Позиция {candidateX} не подходит: " +
                             $"расстояние до объекта на {existingObj.positionX} = {distanceBetweenCenters}, " +
                             $"требуется минимум {minRequiredDistance}");
                    break;
                }
            }

            if (isPositionValid)
            {
                spawnX = candidateX;
                Debug.Log($"Найдена валидная позиция: {spawnX} (попытка {attempt + 1})");
                return true;
            }
        }

        Debug.Log($"Не удалось найти свободное место после 100 попыток");
        Debug.Log($"Существующие объекты:");
        foreach (var obj in existingObjects)
        {
            Debug.Log($"  Позиция: {obj.positionX}, Ширина: {obj.width}");
        }
        
        return false;
    }

    /// <summary>
    /// Получает ширину спавненного объекта по индексу
    /// </summary>
    private float GetWidthForSpawnedObject(int index)
    {
        // Ищем соответствующий префаб в массиве objectsToSpawn
        RectTransform objTransform = spawnedObjects[index];
        if (objTransform == null) return 200f;

        // Ищем по имени префаба
        string objName = objTransform.name;
        foreach (var objData in objectsToSpawn)
        {
            if (objData.prefab != null && objName.Contains(objData.prefab.name))
            {
                return GetObjectWidth(objData);
            }
        }

        return 200f; // Значение по умолчанию
    }

    /// <summary>
    /// Получает случайную позицию с учетом ширины объекта
    /// </summary>
    private float GetRandomPositionWithMargin(float objectWidth)
    {
        float halfWidth = objectWidth / 2;
        float margin = objectWidth * 0.2f; // 20% отступа от краев
        
        return Random.Range(minX + halfWidth + margin, maxX - halfWidth - margin);
    }

    private IEnumerator SpawnObjectWithAnimation(SpawnableObjectData objectData, float spawnX, float objectWidth)
    {
        float objectTargetY = objectData.targetYPosition;

        GameObject spawnedObject = Instantiate(objectData.prefab, parentCanvas);
        RectTransform rectTransform = spawnedObject.GetComponent<RectTransform>();

        if (rectTransform == null)
            rectTransform = spawnedObject.AddComponent<RectTransform>();

        // Добавляем компонент для отслеживания
        SpawnedObjectTracker tracker = spawnedObject.AddComponent<SpawnedObjectTracker>();
        tracker.Initialize(this, rectTransform);

        // Сохраняем ширину в пользовательских данных
        SpawnedObjectData customData = spawnedObject.AddComponent<SpawnedObjectData>();
        customData.objectWidth = objectWidth;

        spawnedObjects.Add(rectTransform);

        rectTransform.anchoredPosition = new Vector2(spawnX, spawnStartY);
        
        yield return StartCoroutine(AnimateRise(rectTransform, spawnX, objectTargetY));
        
        Debug.Log($"✅ Создан {objectData.prefab.name} на X: {spawnX}, " +
                 $"Ширина: {objectWidth}, Всего объектов: {spawnedObjects.Count}");
    }

    private IEnumerator AnimateRise(RectTransform rectTransform, float targetX, float targetY)
    {
        float startY = spawnStartY;
        float elapsedTime = 0f;

        while (elapsedTime < riseDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / riseDuration);
            float curvedT = riseCurve.Evaluate(t);

            float currentY = Mathf.Lerp(startY, targetY, curvedT);
            rectTransform.anchoredPosition = new Vector2(targetX, currentY);

            yield return null;
        }

        rectTransform.anchoredPosition = new Vector2(targetX, targetY);
    }

    public void RemoveObjectFromList(RectTransform rectTransform)
    {
        if (spawnedObjects.Contains(rectTransform))
        {
            spawnedObjects.Remove(rectTransform);
            Debug.Log($"Объект удален. Осталось: {spawnedObjects.Count}/{maxObjectsOnScreen}");
        }
    }

    public void StartAutoSpawning()
    {
        StopAutoSpawning();
        autoSpawnCoroutine = StartCoroutine(AutoSpawnRoutine());
    }

    public void StopAutoSpawning()
    {
        if (autoSpawnCoroutine != null)
        {
            StopCoroutine(autoSpawnCoroutine);
            autoSpawnCoroutine = null;
        }
    }

    private IEnumerator AutoSpawnRoutine()
    {
        while (true)
        {
            TrySpawnObject();
            yield return new WaitForSeconds(autoSpawnInterval);
        }
    }

    /// <summary>
    /// Возвращает детальную информацию о текущем состоянии
    /// </summary>
    public string GetDetailedDebugInfo()
    {
        string info = $"=== ДЕТАЛЬНАЯ ИНФОРМАЦИЯ ===\n";
        info += $"Всего объектов: {spawnedObjects.Count}/{maxObjectsOnScreen}\n";
        info += $"Диапазон X: {minX} до {maxX}\n\n";
        
        if (spawnedObjects.Count > 0)
        {
            info += "Текущие объекты:\n";
            
            // Группируем по расстояниям
            for (int i = 0; i < spawnedObjects.Count; i++)
            {
                if (spawnedObjects[i] != null)
                {
                    float posX = spawnedObjects[i].anchoredPosition.x;
                    float width = GetWidthForSpawnedObject(i);
                    
                    info += $"{i}: X={posX:F0}, Ширина={width:F0}\n";
                    
                    // Считаем расстояния до других объектов
                    for (int j = i + 1; j < spawnedObjects.Count; j++)
                    {
                        if (spawnedObjects[j] != null)
                        {
                            float otherPosX = spawnedObjects[j].anchoredPosition.x;
                            float otherWidth = GetWidthForSpawnedObject(j);
                            float distance = Mathf.Abs(posX - otherPosX);
                            float minRequired = (width/2) + (otherWidth/2);
                            
                            info += $"  → до объекта {j}: расстояние={distance:F0}, " +
                                   $"требуется минимум={minRequired:F0}, " +
                                   $"статус={(distance >= minRequired ? "✅ OK" : "❌ ПЕРЕСЕЧЕНИЕ!")}\n";
                        }
                    }
                }
            }
        }
        else
        {
            info += "Нет активных объектов\n";
        }
        
        return info;
    }

    /// <summary>
    /// Быстрая проверка на пересечения
    /// </summary>
    public bool CheckForOverlaps()
    {
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            if (spawnedObjects[i] == null) continue;
            
            float posX1 = spawnedObjects[i].anchoredPosition.x;
            float width1 = GetWidthForSpawnedObject(i);
            
            for (int j = i + 1; j < spawnedObjects.Count; j++)
            {
                if (spawnedObjects[j] == null) continue;
                
                float posX2 = spawnedObjects[j].anchoredPosition.x;
                float width2 = GetWidthForSpawnedObject(j);
                
                float distance = Mathf.Abs(posX1 - posX2);
                float minDistance = (width1/2) + (width2/2);
                
                if (distance < minDistance)
                {
                    Debug.LogError($"❌ НАЙДЕНО ПЕРЕСЕЧЕНИЕ! Объекты {i} и {j} пересекаются!");
                    Debug.LogError($"Объект {i}: X={posX1}, Ширина={width1}");
                    Debug.LogError($"Объект {j}: X={posX2}, Ширина={width2}");
                    Debug.LogError($"Расстояние: {distance}, Требуется: {minDistance}");
                    return true;
                }
            }
        }
        
        Debug.Log("✅ Пересечений не найдено");
        return false;
    }

    public void ClearAllObjects()
    {
        for (int i = parentCanvas.childCount - 1; i >= 0; i--)
        {
            Destroy(parentCanvas.GetChild(i).gameObject);
        }
        spawnedObjects.Clear();
        Debug.Log("Все объекты очищены");
    }
}

/// <summary>
/// Вспомогательный класс для хранения информации об объекте
/// </summary>
public class ObjectPositionInfo
{
    public float positionX;
    public float width;
    
    public ObjectPositionInfo(float posX, float w)
    {
        positionX = posX;
        width = w;
    }
}

/// <summary>
/// Компонент для отслеживания объектов
/// </summary>
public class SpawnedObjectTracker : MonoBehaviour
{
    private RandomSpawnerUI spawner;
    private RectTransform rectTransform;

    public void Initialize(RandomSpawnerUI spawner, RectTransform rectTransform)
    {
        this.spawner = spawner;
        this.rectTransform = rectTransform;
    }

    private void OnDestroy()
    {
        if (spawner != null && rectTransform != null)
        {
            spawner.RemoveObjectFromList(rectTransform);
        }
    }
}

/// <summary>
/// Компонент для хранения данных о спавненном объекте
/// </summary>
public class SpawnedObjectData : MonoBehaviour
{
    public float objectWidth = 200f;
}
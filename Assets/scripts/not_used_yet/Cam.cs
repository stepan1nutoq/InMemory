using UnityEngine;

public class CameraBoundsLimiter : MonoBehaviour
{
    public SpriteRenderer mapRenderer;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("Этот скрипт должен быть прикреплен к камере.");
            enabled = false;
            return;
        }

        if (mapRenderer == null)
        {
            Debug.LogError("Назначьте SpriteRenderer в инспекторе");
            enabled = false;
            return;
        }

        Bounds bounds = mapRenderer.bounds;
        Vector3 center = bounds.center;

        transform.position = new Vector3(center.x, center.y, transform.position.z);

        float spriteWidth = bounds.size.x;
        float spriteHeight = bounds.size.y;

        float screenRatio = (float)Screen.width / Screen.height;

        float targetSizeY = spriteHeight / 2f;

        float targetSizeX = spriteWidth / 2f / screenRatio;

        cam.orthographicSize = Mathf.Max(targetSizeY, targetSizeX);

        _CalculateAndClampCameraPosition(bounds);
    }

    void LateUpdate()
    {
        if (mapRenderer != null)
        {
            Bounds bounds = mapRenderer.bounds;
            _CalculateAndClampCameraPosition(bounds);
        }
    }

    private void _CalculateAndClampCameraPosition(Bounds bounds)
    {
        float vertExtent = cam.orthographicSize;
        float horzExtent = vertExtent * cam.aspect;

        float minX = bounds.min.x + horzExtent;
        float maxX = bounds.max.x - horzExtent;
        float minY = bounds.min.y + vertExtent;
        float maxY = bounds.max.y - vertExtent;

        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        float clampedY = Mathf.Clamp(transform.position.y, minY, maxY);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }
}
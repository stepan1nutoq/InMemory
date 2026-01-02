using UnityEngine;
using UnityEngine.InputSystem;

public class MouseController : MonoBehaviour
{
    public Camera mainCamera;

    [SerializeField]
    private Vector2 cursorOffset = new Vector2(16f, -23f);

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        Cursor.visible = false;
    }

    void Update()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 adjustedPosition = mousePosition + cursorOffset;

        Vector3 bottomLeft = mainCamera.ScreenToWorldPoint(new Vector3(10, -12, mainCamera.nearClipPlane));
        Vector3 topRight = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width + 10, Screen.height - 20, mainCamera.nearClipPlane));

        float minX = bottomLeft.x;
        float maxX = topRight.x;
        float minY = bottomLeft.y;
        float maxY = topRight.y;

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(adjustedPosition.x, adjustedPosition.y, mainCamera.nearClipPlane));

        worldPos.x = Mathf.Clamp(worldPos.x, minX, maxX);
        worldPos.y = Mathf.Clamp(worldPos.y, minY, maxY);
        worldPos.z = -1f;

        transform.position = worldPos;
    }
}
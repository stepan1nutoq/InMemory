using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteHoverColorChange : MonoBehaviour
{
    public Color hoverColor = new Color(0.5647f, 0f, 0f); 
    
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    private Color originalColor;

    public InputAction 
    pointerPosition; 

    private bool isHovering = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        originalColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        pointerPosition.Enable();

        pointerPosition.performed += OnPointerMove;
        pointerPosition.canceled += OnPointerMove;
    }

    private void OnDisable()
    {
        pointerPosition.performed -= OnPointerMove;
        pointerPosition.canceled -= OnPointerMove;
        pointerPosition.Disable();
    }

    private void Update()
    {   
        var collider = GetComponent<PolygonCollider2D>();if (collider == null){    Debug.LogWarning("PolygonCollider2D не найден");    return;}
        CheckHover();
    }

    private Vector2 currentPointerPosition;

    private void OnPointerMove(InputAction.CallbackContext context)
    {
        currentPointerPosition = context.ReadValue<Vector2>();
    }

    private void CheckHover()
    {
        if (spriteRenderer == null || mainCamera == null)
            return;

     Vector3 worldPoint = mainCamera.ScreenToWorldPoint(currentPointerPosition);
     Collider2D collider = Physics2D.OverlapPoint(worldPoint);
     bool currentlyHovering = collider != null && collider.gameObject == gameObject;

        if (currentlyHovering != isHovering)
        {
            isHovering = currentlyHovering;
            spriteRenderer.color = isHovering ? hoverColor : originalColor;
        }
    }
}
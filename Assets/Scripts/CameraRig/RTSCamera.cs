using UnityEngine;
using UnityEngine.InputSystem;

// Камера как в MOBA: стрелки или мышь у края экрана — двигать,
// колёсико — зум, пробел — к герою. WASD не используем: QWER пойдут под способности.
public class RTSCamera : MonoBehaviour
{
    public Transform followTarget;

    [Header("Движение")]
    public float panSpeed = 25f;
    public bool edgePan = true;
    public float edgeSize = 12f;
    public Vector2 boundsMin = new Vector2(-LaneData.Half, -LaneData.Half);
    public Vector2 boundsMax = new Vector2(LaneData.Half, LaneData.Half);

    [Header("Зум")]
    public float zoomStep = 3f;
    public float minDistance = 10f;
    public float maxDistance = 40f;

    Transform camTransform;
    Vector3 zoomDir;
    float distance;

    void Awake()
    {
        var cam = GetComponentInChildren<Camera>();
        camTransform = cam.transform;
        zoomDir = camTransform.localPosition.normalized;
        distance = camTransform.localPosition.magnitude;
    }

    // Перенести камеру в точку (клик по мини-карте).
    public void JumpTo(Vector3 world) => transform.position = new Vector3(world.x, 0f, world.z);

    // Потянуть камеру пальцем (смещение в пикселях экрана).
    public void PanScreen(Vector2 deltaPixels)
    {
        float k = distance * 2.2f / Mathf.Max(1, Screen.height);
        var pos = transform.position - new Vector3(deltaPixels.x, 0f, deltaPixels.y) * k;
        pos.x = Mathf.Clamp(pos.x, boundsMin.x, boundsMax.x);
        pos.z = Mathf.Clamp(pos.z, boundsMin.y, boundsMax.y);
        transform.position = pos;
    }

    // Зум (плюс — ближе).
    public void ZoomBy(float amount)
    {
        distance = Mathf.Clamp(distance - amount, minDistance, maxDistance);
        camTransform.localPosition = zoomDir * distance;
        camTransform.LookAt(transform);
    }

    void LateUpdate()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null || TouchInput.UsingTouch) return;

        Vector3 dir = Vector3.zero;
        if (kb.upArrowKey.isPressed) dir.z += 1;
        if (kb.downArrowKey.isPressed) dir.z -= 1;
        if (kb.rightArrowKey.isPressed) dir.x += 1;
        if (kb.leftArrowKey.isPressed) dir.x -= 1;

        if (edgePan && Application.isFocused)
        {
            Vector2 p = mouse.position.ReadValue();
            if (p.x >= 0 && p.x <= Screen.width && p.y >= 0 && p.y <= Screen.height && !HudArea.IsOver(p))
            {
                if (p.x < edgeSize) dir.x -= 1;
                if (p.x > Screen.width - edgeSize) dir.x += 1;
                if (p.y < edgeSize) dir.z -= 1;
                if (p.y > Screen.height - edgeSize) dir.z += 1;
            }
        }

        Vector3 pos = transform.position + dir.normalized * panSpeed * Time.unscaledDeltaTime;

        if (kb.spaceKey.isPressed && followTarget != null)
        {
            pos.x = followTarget.position.x;
            pos.z = followTarget.position.z;
        }

        pos.x = Mathf.Clamp(pos.x, boundsMin.x, boundsMax.x);
        pos.z = Mathf.Clamp(pos.z, boundsMin.y, boundsMax.y);
        transform.position = pos;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance = Mathf.Clamp(distance - Mathf.Sign(scroll) * zoomStep, minDistance, maxDistance);
            camTransform.localPosition = zoomDir * distance;
            camTransform.LookAt(transform);
        }
    }
}

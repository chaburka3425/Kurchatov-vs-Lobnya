using UnityEngine;

// Картинка всегда повёрнута к камере (лица на головах героя).
public class Billboard : MonoBehaviour
{
    public Transform anchor;           // к какой точке прилипает (голова)
    public float towardCamera = 0.45f; // насколько выдвинута к камере

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (anchor != null) transform.position = anchor.position - cam.transform.forward * towardCamera;
        transform.rotation = cam.transform.rotation;
    }
}
